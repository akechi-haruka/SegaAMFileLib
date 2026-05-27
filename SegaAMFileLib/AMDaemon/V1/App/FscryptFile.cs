using System.Runtime.InteropServices;
using DiscUtils;
using DiscUtils.Ntfs;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

public abstract class FscryptFile {
    public const int HMAC_LENGTH = 0x200;
    protected static readonly byte[] NTFS_HEADER = Hex.From("eb52904e544653202020200010010000");
    protected static readonly byte[] EXFAT_HEADER = Hex.From("eb769045584641542020200000000000");

    private static readonly ILogger LOG = Log.GetOrCreate("FSCrypt");

    public BootId BootId { get; }
    public byte[] Key { get; protected set; }
    public byte[] Iv { get; protected set; }
    public Stream SourceStream { get; }

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

    public abstract DiscFileSystem OpenRealFilesystem();

    public byte[] ReadAndDecryptWholeFile() {
        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);

        LOG.LogDebug("Allocating " + BootId.GetFileSystemSize() + " to read whole filesystem to memory");
        byte[] buf = new byte[BootId.GetFileSystemSize()];

        FscryptStream decryptedFilesystemStream = new FscryptStream(SourceStream, BootId.GetFileSystemSize(), Key, Iv);

        LOG.LogInformation("Reading " + buf.Length + " bytes");
        decryptedFilesystemStream.ReadExactly(buf);

        return buf;
    }

    public void ExtractTo(string targetDirectory, FsUtils.ProgressCallback callback = null) {
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
            FsUtils.ExtractRecursive(LOG, optFs.Root, targetDirectory, callback);
        } catch (Exception ex) {
            LOG.LogError(ex, "Extraction to " + targetDirectory + " failed");
            throw new IOException("Extraction to " + targetDirectory + " failed", ex);
        }
    }

    public DiscFileInfo OpenInnerVhd() {
        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);
        FscryptStream decryptedFilesystemStream = new FscryptStream(SourceStream, BootId.GetFileSystemSize(), Key, Iv);

        if (LOG.IsEnabled(LogLevel.Trace)) {
            byte[] buf = new byte[256];
            decryptedFilesystemStream.ReadExactly(buf);
            LOG.LogTrace("Initial 256 bytes of decrypted filesystem:\n" + Hex.Dump(buf, 256));
            decryptedFilesystemStream.Seek(0, SeekOrigin.Begin);
            LOG.LogTrace(FsUtils.DumpNtfsFileSystemProperties(decryptedFilesystemStream));
            decryptedFilesystemStream.Seek(0, SeekOrigin.Begin);
        }

        string innerVhdFile = "internal_" + BootId.sequenceNumber + ".vhd";
        NtfsFileSystem appFs = new NtfsFileSystem(decryptedFilesystemStream);
        DiscFileInfo innerVhd = appFs.Root.GetFiles().FirstOrDefault(f => f.Name == innerVhdFile);
        if (innerVhd == null) {
            LOG.LogError("Could not find requested file inside NTFS file system: " + innerVhdFile);
            LOG.LogInformation("Files in root: " + String.Join(',', appFs.Root.GetFiles()));
            throw new IOException("Could not find file inside NTFS file system: " + innerVhdFile);
        }

        return innerVhd;
    }
}