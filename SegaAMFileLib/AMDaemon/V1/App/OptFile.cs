using DiscUtils;
using DiscUtils.ExFat;
using DiscUtils.Ntfs;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

/// <summary>
/// .opt file container.
/// </summary>
public class OptFile : FscryptFile {
    private static readonly ILogger LOG = Log.GetOrCreate("Opt ");

    /// <summary>
    /// The parent .opt file for APMv3 opts (which contain another .opt file)
    /// </summary>
    /// <seealso cref="ApmOptFile"/>
    public OptFile ApmParent { get; }

    /// <summary>
    /// Loads an .opt fscrypt container from a stream.
    /// </summary>
    /// <param name="data">The stream to read from</param>
    /// <param name="apmParent">The parent container in case this container is a patch of another container and for APMv3. This is only used for APM* functions, and should be null otherwise.</param>
    /// <param name="verify">Whether to verify the container or not</param>
    /// <exception cref="ArgumentNullException">if data is null</exception>
    /// <exception cref="ArgumentException">there given stream is invalid</exception>
    /// <exception cref="IOException">error reading BootId or header data</exception>
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

    /// <inheritdoc/>
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
        }

        return new ExFatFileSystem(decryptedFilesystemStream);
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

    /// <summary>
    /// Opens the inner APMv3 .opt that is contained inside this .opt file as a filesystem.
    /// </summary>
    /// <returns>A <see cref="DiscFileSystem"/> that accesses the INNER .opt inside this .opt file</returns>
    /// <exception cref="InvalidOperationException">if this .opt is not for APMv3</exception>
    /// <seealso cref="ApmOptFile"/>
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

    /// <summary>
    /// Extracts all files inside the .opt file inside this container to the given directory.
    /// </summary>
    /// <param name="targetDirectory">The directory to extract to. Will be created if it doesn't exist.</param>
    /// <param name="callback">An optional callback function that receives extraction progress.</param>
    /// <param name="skipExisting">Skip files if they already exist, otherwise overwrite them</param>
    /// <exception cref="ArgumentException"><paramref name="targetDirectory"/> is invalid</exception>
    /// <exception cref="InvalidOperationException">if this container is not for APMv3</exception>
    /// <exception cref="IOException">extraction failed (cause as inner exception)</exception>
    public void ExtractInnerApmTo(string targetDirectory, FsUtils.ProgressCallback callback = null, bool skipExisting = false) {
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
            FsUtils.ExtractRecursive(LOG, optFs.Root, targetDirectory, callback, skipExisting);
        } catch (Exception ex) {
            LOG.LogError(ex, "Extraction to " + targetDirectory + " failed");
            throw new IOException("Extraction to " + targetDirectory + " failed", ex);
        }
    }
}