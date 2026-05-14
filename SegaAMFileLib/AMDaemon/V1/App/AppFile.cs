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

public class AppFile : FscryptFile {
    private static readonly ILogger LOG = Log.GetOrCreate("App ");

    public AppFile Parent { get; }

    public AppFile(Stream data, AppFile parent = null) : base(data) {
        Parent = parent;
        GenerateEncryptionKeys();
    }

    protected virtual void GenerateEncryptionKeys() {
        byte[] initialBytes = new byte[16];
        SourceStream.ReadExactly(initialBytes);

        EncryptionParameters env = EncryptionEnvironment.GetGame(BootId.GetAppId());
        Key = env.Key;
        Iv = AppFsEncryption.CalculateFileIv(Key, NTFS_HEADER, initialBytes);
        LOG.LogInformation("Custom IV was derived to be " + Hex.To(Iv));

        SourceStream.Seek(-initialBytes.Length, SeekOrigin.Current);
    }

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

        PartitionTable partitionTable = virtualDisk.Partitions;
        if (partitionTable == null) {
            throw new IOException("Partition table in inner .vhd file not found");
        }

        if (partitionTable.Partitions.Count == 0) {
            throw new IOException("No partitions found in inner .vhd file");
        }

        return new NtfsFileSystem(partitionTable.Partitions[0].Open());
    }
}