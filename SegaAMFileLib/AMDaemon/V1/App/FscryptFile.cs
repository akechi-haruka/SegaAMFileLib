using System.Runtime.InteropServices;
using DiscUtils;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

/// <summary>
/// The base class of any fscrypt container.
/// </summary>
public abstract class FscryptFile {
    /// <summary>
    /// The length of the HMAC signature placeholder in a fscrypt container.
    /// </summary>
    public const int HMAC_LENGTH = 0x200;

    /// <summary>
    /// The first 16 bytes of a NTFS file system.
    /// </summary>
    protected static readonly byte[] NTFS_HEADER = Hex.From("eb52904e544653202020200010010000");

    /// <summary>
    /// The first 16 bytes of an EXFAT file system.
    /// </summary>
    protected static readonly byte[] EXFAT_HEADER = Hex.From("eb769045584641542020200000000000");

    private static readonly ILogger LOG = Log.GetOrCreate("FSCrypt");

    /// <summary>
    /// The BootId of the container.
    /// </summary>
    public BootId BootId { get; }

    /// <summary>
    /// The encryption key of the container.
    /// </summary>
    public byte[] Key { get; protected set; }

    /// <summary>
    /// The initialization vector of the container.
    /// </summary>
    public byte[] Iv { get; protected set; }

    /// <summary>
    /// The underlying stream where data is read from. 
    /// </summary>
    protected Stream SourceStream { get; }

    /// <summary>
    /// Loads a fscrypt container from a stream.
    /// </summary>
    /// <param name="data">The stream to read from</param>
    /// <param name="verify">Whether to verify the container or not</param>
    /// <exception cref="ArgumentNullException">if data is null</exception>
    /// <exception cref="ArgumentException">there given stream is invalid</exception>
    /// <exception cref="IOException">error reading BootId or header data</exception>
    protected FscryptFile(Stream data, bool verify = true) {
        ArgumentNullException.ThrowIfNull(data);

        SourceStream = data;

        int bootIdLen = Marshal.SizeOf<BootId>();

        if (data.Length < bootIdLen) {
            throw new ArgumentException("data given is " + data.Length + " bytes, but at least " + bootIdLen + " are expected");
        }

        byte[] bootIdBytes = new byte[bootIdLen];
        data.ReadExactly(bootIdBytes);

        BootId = BootId.FromEncryptedBytes(bootIdBytes);

        long filesystemOffset = BootId.GetOffsetOfFileSystem();
        LOG.LogDebug("BootId game data: " + BootId.GetAppId() + " / " + BootId.GetPlatformId() + ", version=" + BootId.gameVersion + ", time=" + BootId.gameTimestamp);
        LOG.LogDebug("BootId block data: header=" + BootId.headerBlockCount + ", size=" + BootId.blockSize + ", total=" + BootId.blockCount + ", fsSize=" + BootId.GetFileSystemSize() + ", totalSize=" + BootId.GetFullContainerSize());

        if (verify) {
            VerifySignature(data);
            VerifyCrcTable(bootIdBytes, data);
        } else {
            LOG.LogWarning("File verification is skipped");
        }

        LOG.LogDebug("File system starts at " + filesystemOffset);
        data.Seek(filesystemOffset, SeekOrigin.Begin);
    }

    // amsImageHmac*
    private void VerifySignature(Stream stream) {
        LOG.LogTrace("Verifying fscrypt container HMAC signature");

        stream.Seek(BootId.length, SeekOrigin.Begin);
        byte[] stored = new byte[20];
        stream.ReadExactly(stored);
        stream.Seek(HMAC_LENGTH - stored.Length, SeekOrigin.Current);

        // don't verify bootid and hmac itself
        byte[] verifiableBytes = new byte[BootId.headerBlockCount * BootId.blockSize - BootId.length - HMAC_LENGTH];
        stream.ReadExactly(verifiableBytes);

        byte[] expected = Signing.Hash(verifiableBytes, EncryptionEnvironment.BootIdHmac);

        if (!Enumerable.SequenceEqual(expected, stored)) {
            throw new IOException("HMAC signature verification failed\nExpected: " + Hex.To(expected) + "\nGot     : " + Hex.To(stored));
        }

        LOG.LogDebug(verifiableBytes.Length + " bytes verified successfully");
    }

    private void VerifyCrcTable(byte[] bootId, Stream stream) {
        LOG.LogTrace("Verifying fscrypt container CRC table");
        stream.Seek(BootId.length + HMAC_LENGTH, SeekOrigin.Begin);
        List<uint> givenTable = new List<uint>();
        for (uint i = 0; i < BootId.blockCount; i++) {
            byte[] crc = new byte[4];
            stream.ReadExactly(crc);
            givenTable.Add(BitConverter.ToUInt32(crc));
        }

        // first block skips hmac and it's own crc
        stream.Seek(BootId.length + HMAC_LENGTH + 0x4, SeekOrigin.Begin);

        List<uint> expectedTable = new List<uint>();
        for (uint i = 0; i < BootId.blockCount; i++) {
            if (i == 0) {
                uint crc = SegaCrc32.CalcCrc32(bootId);
                byte[] firstBlockData = new byte[BootId.blockSize - BootId.length - HMAC_LENGTH - 0x4];
                stream.ReadExactly(firstBlockData);
                expectedTable.Add(SegaCrc32.CalcCrc32(firstBlockData, null, null, crc));
            } else {
                byte[] block = new byte[BootId.blockSize];
                stream.ReadExactly(block);
                expectedTable.Add(SegaCrc32.CalcCrc32(block));
            }
        }

        // check
        if (givenTable.Count != expectedTable.Count) {
            throw new IOException("Expected " + expectedTable.Count + " entries in the fscrypt containers' CRC table, got " + givenTable.Count);
        }

        for (int i = 0; i < givenTable.Count; i++) {
            if (expectedTable[i] != givenTable[i]) {
                throw new IOException("Failed to verify block " + i + ", CRC failure: Expected " + expectedTable[i].ToString("X") + ", got " + givenTable[i].ToString("X"));
            }
        }

        LOG.LogDebug(givenTable.Count + " CRCs verified successfully");
    }

    /// <summary>
    /// Opens the file system that is inside this container, including any intermediary file systems or containers.
    /// </summary>
    /// <returns>A <see cref="DiscFileSystem"/> of the given FscryptFile that contains the actual game files (ex. you will find game.bat in an .app file)</returns>
    public abstract DiscFileSystem OpenRealFilesystem();

    /// <summary>
    /// Decrypts the WHOLE file in-memory. Do not use on large containers.
    /// </summary>
    /// <returns>a byte array of the raw (outer) file system that is inside the given container.</returns>
    public byte[] ReadAndDecryptWholeFile() {
        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);

        LOG.LogDebug("Allocating " + BootId.GetFileSystemSize() + " to read whole filesystem to memory");
        byte[] buf = new byte[BootId.GetFileSystemSize()];

        FscryptStream decryptedFilesystemStream = new FscryptStream(SourceStream, BootId.GetFileSystemSize(), Key, Iv);

        LOG.LogInformation("Reading " + buf.Length + " bytes");
        decryptedFilesystemStream.ReadExactly(buf);

        return buf;
    }

    /// <summary>
    /// Extracts all files inside the innermost file system of this container to the given directory.
    /// </summary>
    /// <param name="targetDirectory">The directory to extract to. Will be created if it doesn't exist.</param>
    /// <param name="callback">An optional callback function that receives extraction progress.</param>
    /// <param name="skipExisting">Skip files if they already exist, otherwise overwrite them</param>
    /// <exception cref="ArgumentException"><paramref name="targetDirectory"/> is invalid</exception>
    /// <exception cref="IOException">extraction failed (cause as inner exception)</exception>
    public void ExtractTo(string targetDirectory, FsUtils.ProgressCallback callback = null, bool skipExisting = false) {
        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);
        try {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
            LOG.LogInformation("Extracting fscrypt file to: " + targetDirectory);
            if (!Directory.Exists(targetDirectory)) {
                LOG.LogDebug("Creating directory: " + targetDirectory);
                Directory.CreateDirectory(targetDirectory);
            }

            LOG.LogDebug("Opening file system");
            DiscFileSystem optFs = OpenRealFilesystem();
            FsUtils.ExtractRecursive(LOG, optFs.Root, targetDirectory, callback, skipExisting);
        } catch (Exception ex) {
            LOG.LogError(ex, "Extraction to " + targetDirectory + " failed");
            throw new IOException("Extraction to " + targetDirectory + " failed", ex);
        }
    }
}