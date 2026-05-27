using DiscUtils;
using DiscUtils.ExFat;
using DiscUtils.Ntfs;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

public class OptFile : FscryptFile {
    private static readonly ILogger LOG = Log.GetOrCreate("Opt ");

    public OptFile ApmParent { get; }

    public OptFile(Stream data, OptFile apmParent = null, bool verify = true) : base(data, verify) {
        ApmParent = apmParent;

        byte[] initialBytes = new byte[16];
        data.ReadExactly(initialBytes);

        Key = EncryptionEnvironment.Option.Key;
        Iv = FscryptUtils.CalculateFileIv(Key, IsApmOption() ? NTFS_HEADER : EXFAT_HEADER, initialBytes);
        LOG.LogDebug("Custom IV was derived to be " + Hex.To(Iv));

        data.Seek(-initialBytes.Length, SeekOrigin.Current);
    }

    private bool IsApmOption() {
        return BootId.IsApmOption();
    }

    public override DiscFileSystem OpenRealFilesystem() {
        LOG.LogDebug("Opening filesystem");

        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);
        FscryptStream decryptedFilesystemStream = new FscryptStream(SourceStream, BootId.GetFileSystemSize(), Key, Iv);

        if (LOG.IsEnabled(LogLevel.Trace)) {
            byte[] buf = new byte[256];
            decryptedFilesystemStream.ReadExactly(buf);
            LOG.LogTrace("Initial 256 bytes of decrypted filesystem:\n" + Hex.Dump(buf, 256));
            decryptedFilesystemStream.Seek(0, SeekOrigin.Begin);
        }

        if (IsApmOption()) {
            return new NtfsFileSystem(decryptedFilesystemStream);
        } else {
            return new ExFatFileSystem(decryptedFilesystemStream);
        }
    }

    private Stream OpenInnerApmOptStream() {
        if (!IsApmOption()) {
            throw new InvalidOperationException("This .opt file is not for APM");
        }

        DiscFileSystem outerFs = OpenRealFilesystem();
        DiscFileInfo innerOptFile = outerFs.Root.GetFiles().FirstOrDefault(f => f.Name.EndsWith(".opt"));
        if (innerOptFile == null) {
            throw new IOException("No inner .opt file found");
        }

        return innerOptFile.OpenRead();
    }

    public DiscFileSystem OpenInnerApmOptFilesystem() {
        LOG.LogInformation("Opening inner .opt filesystem (for APM)");
        if (!IsApmOption()) {
            throw new InvalidOperationException("This .opt file is not for APM");
        }

        LOG.LogInformation("- Base file: " + BootId);
        ApmOptFile innerOpt = new ApmOptFile(OpenInnerApmOptStream(), GetApmParentRecursive(ApmParent));
        return innerOpt.OpenRealFilesystem();
    }

    private static ApmOptFile GetApmParentRecursive(OptFile parent) {
        if (parent == null) {
            return null;
        }

        LOG.LogInformation("- Inner file chain: " + parent.BootId);
        parent.SourceStream.Seek(0, SeekOrigin.Begin);
        return new ApmOptFile(parent.OpenInnerApmOptStream(), GetApmParentRecursive(parent.ApmParent));
    }

    public void ExtractInnerApmTo(string targetDirectory, FsUtils.ProgressCallback callback = null) {
        if (!IsApmOption()) {
            throw new InvalidOperationException("This .opt file is not for APM");
        }

        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);
        try {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
            LOG.LogInformation("Extracting fscrypt file to: " + targetDirectory);
            if (!Directory.Exists(targetDirectory)) {
                LOG.LogDebug("Creating directory: " + targetDirectory);
                Directory.CreateDirectory(targetDirectory);
            }

            LOG.LogDebug("Opening file system");
            DiscFileSystem optFs = OpenInnerApmOptFilesystem();
            FsUtils.ExtractRecursive(LOG, optFs.Root, targetDirectory, callback);
        } catch (Exception ex) {
            LOG.LogError(ex, "Extraction to " + targetDirectory + " failed");
            throw new IOException("Extraction to " + targetDirectory + " failed", ex);
        }
    }
}