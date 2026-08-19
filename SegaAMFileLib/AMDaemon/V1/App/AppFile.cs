using DiscUtils;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DiscUtils.Streams;
using DiscUtils.Vhd;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

/// <summary>
/// .app file container.
/// </summary>
public class AppFile : FscryptFile {
    private static readonly ILogger LOG = Log.GetOrCreate("App ");

    /// <summary>
    /// The parent app container.
    /// </summary>
    public AppFile Parent { get; }

    /// <summary>
    /// Loads an .app fscrypt container from a stream.
    /// </summary>
    /// <param name="data">The stream to read from</param>
    /// <param name="parent">The parent container in case this container is a patch of another container.</param>
    /// <param name="verify">Whether to verify the container or not</param>
    /// <exception cref="ArgumentNullException">if data is null</exception>
    /// <exception cref="ArgumentException">there given stream is invalid</exception>
    /// <exception cref="IOException">error reading BootId or header data</exception>
    public AppFile(Stream data, AppFile parent = null, bool verify = true) : base(data, verify) {
        Parent = parent;
        GenerateEncryptionKeys();
    }

    /// <summary>
    /// Generates the encryption keys for this container.
    /// </summary>
    protected virtual void GenerateEncryptionKeys() {
        byte[] initialBytes = new byte[16];
        SourceStream.ReadExactly(initialBytes);

        EncryptionParameters env = EncryptionEnvironment.GetGame(BootId.GetAppId());
        Key = env.Key;
        Iv = FscryptUtils.CalculateFileIv(Key, NTFS_HEADER, initialBytes);
        LOG.LogDebug("Custom IV was derived to be " + Hex.To(Iv));

        SourceStream.Seek(-initialBytes.Length, SeekOrigin.Current);
    }

    /// <inheritdoc/>
    public override DiscFileSystem OpenRealFilesystem() {
        LOG.LogDebug("Opening filesystem");

        DiscFileInfo innerVhd = OpenInnerVhd();
        List<DiskImageFile> vhdChain = new List<DiskImageFile>();
        AppFile current = this;
        do {
            vhdChain.Add(new DiskImageFile(current.OpenInnerVhd().OpenRead()));
            LOG.LogInformation("- File chain [" + vhdChain.Count + "]: " + current.BootId);
            current = current.Parent;
        } while (current != null);

        LOG.LogInformation("Opening inner vhd file (" + innerVhd.FullName + ", " + innerVhd.Length + " bytes)");
        Disk virtualDisk = new Disk(vhdChain, Ownership.None);
        if (virtualDisk == null) {
            throw new IOException("Could not determine disk format for inner .vhd file");
        }

        LOG.LogTrace("VHD disk geometry: " + virtualDisk.Geometry);

        PartitionTable partitionTable = virtualDisk.Partitions;
        if (partitionTable == null) {
            throw new IOException("Partition table in inner .vhd file not found");
        }

        if (partitionTable.Partitions.Count == 0) {
            throw new IOException("No partitions found in inner .vhd file");
        }

        return new NtfsFileSystem(partitionTable.Partitions[0].Open());
    }

    private DiscFileInfo OpenInnerVhd() {
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