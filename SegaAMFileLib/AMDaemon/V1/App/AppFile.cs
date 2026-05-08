using System.Runtime.InteropServices;
using DiscUtils;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DiscUtils.Vhd;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

public class AppFile {
    private static readonly byte[] OPTION_HEADER = Hex.From("eb769045584641542020200000000000");
    private static readonly byte[] APP_HEADER = Hex.From("eb52904e544653202020200010010000");

    private static readonly ILogger LOG = Log.GetOrCreate("App ");

    static AppFile() {
        VirtualDiskManager.RegisterVirtualDiskTypes(typeof(Disk).Assembly);
    }

    public BootId BootId { get; }
    public AppFile Parent { get; }
    public byte[] Key { get; }
    public byte[] Iv { get; }
    public Stream SourceStream { get; }

    public AppFile(Stream data, AppFile parent = null) {
        ArgumentNullException.ThrowIfNull(data);

        SourceStream = data;
        Parent = parent;

        int bootIdLen = Marshal.SizeOf<BootId>();

        if (data.Length < bootIdLen) {
            throw new ArgumentException("data given is " + data.Length + " bytes, but at least " + bootIdLen + " are expected");
        }

        byte[] bootIdBytes = new byte[bootIdLen];
        data.ReadExactly(bootIdBytes);
        bootIdBytes = Aes128Cbc.DecryptFromEnv(bootIdBytes, EncryptionEnvironment.BootId);
        BootId = StructUtils.FromBytes<BootId>(bootIdBytes);

        BootId.Verify();

        if (BootId.containerType == ContainerType.Option) {
            Key = EncryptionEnvironment.Option.Key;
            Iv = EncryptionEnvironment.Option.Iv;
        } else {
            EncryptionParameters env = EncryptionEnvironment.GetGame(BootId.GetAppId());
            Key = env.Key;
            Iv = env.Iv;
        }

        long filesystemOffset = BootId.GetOffsetOfFileSystem();
        LOG.LogDebug("File system starts at " + filesystemOffset);
        data.Seek(filesystemOffset, SeekOrigin.Begin);

        byte[] initialBytes = new byte[16];
        data.ReadExactly(initialBytes);
        Iv = AppFsEncryption.CalculateFileIv(Key, GetFileSystemHeader(), initialBytes);
        LOG.LogInformation("Custom IV was derived to be " + Hex.To(Iv));

        data.Seek(-initialBytes.Length, SeekOrigin.Current);
    }

    private byte[] GetFileSystemHeader() {
        return BootId.containerType == ContainerType.Option ? OPTION_HEADER : APP_HEADER;
    }

    public void ExtractTo(string targetDirectory, FsUtils.ProgressCallback callback = null) {
        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);
        try {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
            LOG.LogInformation("Extracting app file to: " + targetDirectory);
            if (!Directory.Exists(targetDirectory)) {
                LOG.LogDebug("Creating directory: " + targetDirectory);
                Directory.CreateDirectory(targetDirectory);
            }

            if (BootId.containerType == ContainerType.Option) {
                ExtractOptionTo(targetDirectory, callback);
            } else {
                ExtractAppTo(targetDirectory, callback);
            }
        } catch (Exception ex) {
            LOG.LogError(ex, "Extraction to " + targetDirectory + " failed");
            throw new IOException("Extraction to " + targetDirectory + " failed", ex);
        }
    }

    private void ExtractOptionTo(string targetDirectory, FsUtils.ProgressCallback callback) {
        throw new NotImplementedException();
    }

    private void ExtractAppTo(string targetDirectory, FsUtils.ProgressCallback callback) {
        LOG.LogDebug("Opening app file as NTFS");
        DiscFileSystem vhdFs = OpenRealFilesystem();
        FsUtils.ExtractRecursive(LOG, vhdFs.Root, targetDirectory, callback);
    }

    public DiscFileInfo OpenInnerVhd() {
        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);
        AppFsStream decryptedFilesystemStream = new AppFsStream(SourceStream, BootId.GetFileSystemSize(), Key, Iv);

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

    public DiscFileSystem OpenRealFilesystem() {
        LOG.LogDebug("Opening filesystem");

        DiscFileInfo innerVhd = OpenInnerVhd();
        DiskImageFile parent = null;

        if (Parent != null) {
            parent = new DiskImageFile(Parent.OpenInnerVhd().OpenRead());
        }

        LOG.LogInformation("Opening inner vhd file (" + innerVhd.FullName + ", " + innerVhd.Length + " bytes)");
        Disk virtualDisk = new Disk(innerVhd.FileSystem, innerVhd.FullName, FileAccess.Read, parent);
        if (virtualDisk == null) {
            throw new IOException("Could not determine disk format for inner .vhd file");
        }

        PartitionTable partitionTable = virtualDisk.Partitions;
        if (partitionTable == null) {
            throw new IOException("Partition table in inner .vhd file not found");
        }

        if (partitionTable.Partitions.Count == 0) {
            throw new IOException("No partitions found in inner .vhd file");
        }

        return new NtfsFileSystem(partitionTable.Partitions[0].Open());
    }

    public byte[] ReadAndDecryptWholeFile() {
        SourceStream.Seek(BootId.GetOffsetOfFileSystem(), SeekOrigin.Begin);

        LOG.LogDebug("Allocating " + BootId.GetFileSystemSize() + " to read whole FS to memory");
        byte[] buf = new byte[BootId.GetFileSystemSize()];

        AppFsStream decryptedFilesystemStream = new AppFsStream(SourceStream, BootId.GetFileSystemSize(), Key, Iv);

        LOG.LogInformation("Reading " + buf.Length + " bytes");
        decryptedFilesystemStream.ReadExactly(buf);

        return buf;
    }
}