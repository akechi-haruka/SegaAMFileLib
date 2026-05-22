using System.Runtime.InteropServices;
using DiscUtils;
using DiscUtils.Ntfs;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

public abstract class FscryptFile {
    protected static readonly byte[] NTFS_HEADER = Hex.From("eb52904e544653202020200010010000");
    protected static readonly byte[] EXFAT_HEADER = Hex.From("eb769045584641542020200000000000");

    private static readonly ILogger LOG = Log.GetOrCreate("FSCrypt");

    public BootId BootId { get; }
    public byte[] Key { get; protected set; }
    public byte[] Iv { get; protected set; }
    public Stream SourceStream { get; }

    protected FscryptFile(Stream data) {
        ArgumentNullException.ThrowIfNull(data);

        SourceStream = data;

        int bootIdLen = Marshal.SizeOf<BootId>();

        if (data.Length < bootIdLen) {
            throw new ArgumentException("data given is " + data.Length + " bytes, but at least " + bootIdLen + " are expected");
        }

        byte[] bootIdBytes = new byte[bootIdLen];
        data.ReadExactly(bootIdBytes);
        bootIdBytes = Aes128Cbc.DecryptFromEnv(bootIdBytes, EncryptionEnvironment.BootId);

        BootId = StructUtils.FromBytes<BootId>(bootIdBytes);

        uint crcExpected = BootId.crc;
        uint crcCalculated = SegaCrc32.CalcCrc32(bootIdBytes, 4);
        if (crcExpected != crcCalculated) {
            throw new IOException("CRC failure for BootID: Expected " + crcExpected + ", got " + crcCalculated);
        }

        BootId.Verify();

        long filesystemOffset = BootId.GetOffsetOfFileSystem();
        LOG.LogDebug("BootId block data: header=" + BootId.headerBlockCount + ", size=" + BootId.blockSize + ", total=" + BootId.blockCount + ", fsSize=" + BootId.GetFileSystemSize() + ", totalSize=" + BootId.GetFullContainerSize());
        LOG.LogDebug("File system starts at " + filesystemOffset);
        data.Seek(filesystemOffset, SeekOrigin.Begin);
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