using DiscUtils;
using DiscUtils.ExFat;
using DiscUtils.ExFat.Internal;
using DiscUtils.ExFat.Internal.Filesystem;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DiscUtils.Streams;
using DiscUtils.Vhd;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

public static class FscryptContainerGenerator {
    private static readonly ILogger LOG = Log.GetOrCreate("FSCryptGen");

    public static void Create(String sourceFilesPath, String outputPath, InstallFile fileInfo, System.Version systemVersion = null, DateTime? requiredTimestamp = null, String platformId = "ACA", byte platformGeneration = 0, byte unknown = 1, EncryptionParameters overrideEncryption = null) {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilesPath);

        if (!Directory.Exists(sourceFilesPath)) {
            throw new DirectoryNotFoundException("Input path not found: " + sourceFilesPath);
        }

        if (!(new DirectoryInfo(outputPath).Parent?.Exists ?? false)) {
            throw new DirectoryNotFoundException("Output path not found: " + outputPath);
        }

        if (requiredTimestamp == null && fileInfo.Sequence > 0) {
            throw new ArgumentException("A patch file requires a required timestamp");
        }

        EncryptionParameters fsEncryption;
        if (overrideEncryption != null) {
            fsEncryption = overrideEncryption;
        } else if (fileInfo.Type == InstallFile.FileType.Option) {
            fsEncryption = fileInfo.IsApm() ? FscryptUtils.CalculateApmEncryptionParameters(fileInfo.GameId) : EncryptionEnvironment.Option;
        } else {
            fsEncryption = EncryptionEnvironment.GetGame(fileInfo.GameId);
        }

        LOG.LogInformation("Creating container of type " + fileInfo.Type + " for " + fileInfo.GameId + ", ver " + fileInfo.VersionNumber);
        LOG.LogDebug("Encryption key: " + Hex.To(fsEncryption.Key));
        LOG.LogDebug("Encryption IV: " + Hex.To(fsEncryption.Iv));

        LOG.LogDebug("Calculating expected size");

        long totalFileSize = GatherDirectorySizeRecursive(sourceFilesPath);

        LOG.LogInformation("Total file size to add into container: " + Util.BytesToString(totalFileSize));

        bool isBasicOpt = fileInfo.Type == InstallFile.FileType.Option && !fileInfo.IsApm();
        bool isNtfsPlusVhd = fileInfo.Type == InstallFile.FileType.App || fileInfo.Type == InstallFile.FileType.Pack || (fileInfo.Type == InstallFile.FileType.Option && fileInfo.IsApm());
        bool isExfat = fileInfo.Type == InstallFile.FileType.Option && !fileInfo.IsApm();

        LOG.LogTrace("isBasicOpt: " + isBasicOpt);
        LOG.LogTrace("isNtfsPlusVhd: " + isNtfsPlusVhd);
        LOG.LogTrace("isExfat: " + isExfat);
        LOG.LogTrace("isAPM: " + fileInfo.IsApm());

        const long minInnerFsSize = 80 * 1024 * 1024; // weird things happen if we try to create a micro file system, enforce 4MB minimum
        totalFileSize = Math.Max(minInnerFsSize, totalFileSize);
        long innerFsSize = (long)(totalFileSize * 1.1F); // no idea how to calculate overhead per file
        innerFsSize = MathUtilities.RoundUp(innerFsSize + 512 + 512, Sizes.Sector); // + MBR + NTFS header, then round up to full sector
        long innerFsMemory = innerFsSize + Sizes.Sector; // add sector for VHD footer
        const long outerFsExtraSpace = minInnerFsSize; // extra space for the outer NTFS container holding the .vhd
        long outerFsSize = MathUtilities.RoundUp(isBasicOpt ? innerFsSize : innerFsSize + outerFsExtraSpace, Sizes.Sector); // round up sector
        long outerFsMemory = outerFsSize;
        long payloadLength = outerFsMemory;

        LOG.LogDebug("Allocating " + innerFsMemory + " bytes (" + Util.BytesToString(innerFsMemory) + ") for new inner file system");
        byte[] innerFsBytes = new byte[innerFsMemory]; // round up one sector + VHD footer
        using (Stream innerFsStream = new MemoryStream(innerFsBytes, true)) {
            DiscFileSystem innerFs;
            VirtualDisk innerDisk = null;
            if (isNtfsPlusVhd) {
                innerFs = CreateInnerFsApp(innerFsStream, innerFsSize, fileInfo, out innerDisk);
            } else if (isExfat) {
                innerFs = CreateInnerFsOpt(innerFsStream, innerFsSize, fileInfo);
            } else {
                throw new NotSupportedException("Cannot yet create container of type " + fileInfo.Type + "(APM=" + fileInfo.IsApm() + ")");
            }

            AddFilesToFileSystemRecursive(innerFs, sourceFilesPath);

            innerFs.Dispose();
            innerDisk?.Dispose();
        }

        innerFsBytes = FixBpbForIv(innerFsBytes, fileInfo);

        LOG.LogInformation(FsUtils.DumpNtfsFileSystemProperties(innerFsBytes));
        LOG.LogTrace("Initial 256 bytes of created inner filesystem:\n" + Hex.Dump(innerFsBytes, 256));

        LOG.LogDebug("Allocating " + outerFsMemory + " bytes (" + Util.BytesToString(outerFsMemory) + ") for container payload (including outer file system)");
        byte[] outerFsBytes = new byte[outerFsMemory];
        DiscFileSystem outerFs;
        using (Stream outerFsStream = new MemoryStream(outerFsBytes, true)) {
            if (isNtfsPlusVhd) {
                outerFs = CreateOuterFsApp(outerFsStream, outerFsSize, fileInfo);

                using (SparseStream internalFile = outerFs.OpenFile("internal_" + fileInfo.Sequence + ".vhd", FileMode.CreateNew)) {
                    internalFile.Write(innerFsBytes);
                }
            } else { // isExfat is implicitely true at this point
                // non-APM .opts have no outer filesystem
                outerFs = null;
                outerFsBytes = innerFsBytes;
            }
        }

        outerFs?.Dispose();

        outerFsBytes = FixBpbForIv(outerFsBytes, fileInfo);

        LOG.LogInformation(FsUtils.DumpNtfsFileSystemProperties(outerFsBytes));
        LOG.LogTrace("Initial 256 bytes of created outer filesystem:\n" + Hex.Dump(outerFsBytes, 256));

        BootId bootId = new BootId() {
            length = BootId.SIZE,
            containerType = fileInfo.Type,
            sequenceNumber = fileInfo.Sequence,
            gameTimestamp = new Timestamp(fileInfo.Date),
            gameVersion = Version.FromSystemVersion(fileInfo.VersionNumber),
            blockSize = BootId.NORMAL_BLOCK_SIZE,
            headerBlockCount = 8,
            platformGeneration = platformGeneration,
            sourceTimestamp = requiredTimestamp != null ? new Timestamp(requiredTimestamp.Value) : new Timestamp(),
            sourceVersion = fileInfo.Sequence > 0 ? Version.FromSystemVersion(fileInfo.RequiredAppVersion) : Version.Empty,
            unknown = unknown,
            platformVersion = Version.FromSystemVersion(systemVersion ?? new System.Version(0, 0, 0))
        };

        if (bootId.platformVersion.Equals(Version.Empty)) {
            LOG.LogWarning("Platform version is unset! This makes this container invalid against amdaemon!");
        }

        bootId.SetAppId(fileInfo.GameId);
        bootId.SetPlatform(platformId);
        bootId.SetSignature();
        bootId.blockCount = bootId.headerBlockCount + (ulong)payloadLength / bootId.blockSize + 1;

        LOG.LogDebug("BootId block data: header=" + bootId.headerBlockCount + ", size=" + bootId.blockSize + ", total=" + bootId.blockCount + ", fsSize=" + bootId.GetFileSystemSize() + ", totalSize=" + bootId.GetFullContainerSize());

        LOG.LogDebug("Encrypting BootId");
        byte[] bootIdBytes = StructUtils.GetBytes(bootId);
        bootIdBytes = SegaCrc32.WriteCrcIntoFirst4Bytes(bootIdBytes);
        bootIdBytes = Aes128Cbc.Encrypt(bootIdBytes, EncryptionEnvironment.BootId.Key, EncryptionEnvironment.BootId.Iv);

        LOG.LogInformation("Creating output file: " + fileInfo.GetFileName());

        LOG.LogDebug("Encrypting file system");

        using (FileStream outputStream = new FileStream(Path.Combine(outputPath, fileInfo.GetFileName()), FileMode.Create)) {
            outputStream.Write(bootIdBytes, 0, (int)bootId.length);
            outputStream.Write(new byte[bootId.GetOffsetOfFileSystem() - bootIdBytes.Length]); // hmac and crc placeholder

            List<uint> blockCrcs = new List<uint>();
            using (FscryptStream encryptedOutputStream = new FscryptStream(outputStream, bootId.GetFileSystemSize(), fsEncryption.Key, fsEncryption.Iv)) {
                encryptedOutputStream.Write(outerFsBytes);

                ulong paddingLength = bootId.GetFullContainerSize() - (ulong)outputStream.Position;
                encryptedOutputStream.Write(new byte[paddingLength]); // padding to block size
                LOG.LogTrace(paddingLength + " padding bytes");

                encryptedOutputStream.Flush();

                outputStream.Seek(0, SeekOrigin.Begin);
                for (ulong block = 0; block < bootId.blockCount; block++) {
                    uint crc = SegaCrc32.CalcCrc32(outputStream.ReadExactly((int)bootId.blockSize));
                    blockCrcs.Add(crc);
                }

                // do this inside FscryptStream using, otherwise the FileStream will be disposed

                // build the CRC table
                if (blockCrcs.Count != (int)bootId.blockCount) {
                    throw new IOException("Expected to get " + bootId.blockCount + " CRCs from encryption operation, got " + blockCrcs.Count + "?");
                }

                outputStream.Seek(bootId.length + FscryptFile.HMAC_LENGTH, SeekOrigin.Begin); // rewind back to where the CRCs get written
                foreach (uint crc in blockCrcs) {
                    outputStream.Write(BitConverter.GetBytes(crc));
                }

                outputStream.Seek(bootId.length + FscryptFile.HMAC_LENGTH + 0x4, SeekOrigin.Begin); // rewind back again for the header CRC, ignore the CRC itself
                uint bootidCrc = SegaCrc32.CalcCrc32(bootIdBytes);

                byte[] headerBlockWithoutCrc = new byte[bootId.blockSize - bootId.length - FscryptFile.HMAC_LENGTH - 0x4]; // rest of the block
                outputStream.ReadExactly(headerBlockWithoutCrc);

                uint finalHeaderCrc = SegaCrc32.CalcCrc32(headerBlockWithoutCrc, null, null, bootidCrc);

                outputStream.Seek(bootId.length + FscryptFile.HMAC_LENGTH, SeekOrigin.Begin);
                outputStream.Write(BitConverter.GetBytes(finalHeaderCrc));

                // create signature
                outputStream.Seek(bootId.length + FscryptFile.HMAC_LENGTH, SeekOrigin.Begin);
                byte[] headerBlocks = new byte[bootId.headerBlockCount * bootId.blockSize - bootId.length - FscryptFile.HMAC_LENGTH]; // rest of the block
                outputStream.ReadExactly(headerBlocks);
                byte[] hash = Signing.Hash(headerBlocks, EncryptionEnvironment.BootIdHmac);

                LOG.LogTrace("HMAC signature: " + Hex.To(hash));

                outputStream.Seek(bootId.length, SeekOrigin.Begin);
                outputStream.Write(hash);

                LOG.LogInformation("Successfully written " + outputStream.Length + " bytes to " + fileInfo);
            }
        }
    }

    private static DiscFileSystem CreateInnerFsOpt(Stream stream, long size, InstallFile fileInfo) {
        LOG.LogTrace("CreateInnerFsOpt");
        ExFatPathFilesystem.Format(stream, new ExFatFormatOptions(), fileInfo.GetFileSystemLabel() + "_inner").Dispose();
        return new ExFatFileSystem(stream);
    }

    private static DiscFileSystem CreateOuterFsApp(Stream stream, long size, InstallFile fileInfo) {
        LOG.LogTrace("CreateOuterFsApp");
        Geometry geometry = new Geometry(size, 1, 17, 4096); // This is a quirk based on how the IV derivation works, so we must use 4K sector size
        return NtfsFileSystem.Format(stream, fileInfo.GameId + "_" + fileInfo.VersionNumber + "_" + fileInfo.Sequence + "_outer", geometry, 0, size / geometry.BytesPerSector, new NtfsFormatOptions());
    }

    private static DiscFileSystem CreateInnerFsApp(Stream stream, long size, InstallFile fileInfo, out VirtualDisk innerDisk) {
        LOG.LogTrace("CreateInnerFsApp");
        innerDisk = Disk.InitializeFixed(stream, Ownership.None, size);
        BiosPartitionTable.Initialize(innerDisk, WellKnownPartitionType.WindowsNtfs);
        VolumeManager vm = new VolumeManager(innerDisk);

        return NtfsFileSystem.Format(vm.GetLogicalVolumes()[0], fileInfo.GetFileSystemLabel() + "_inner", new NtfsFormatOptions());
    }

    private static byte[] FixBpbForIv(byte[] bytes, InstallFile fileInfo) {
        bool isNtfsPlusVhd = fileInfo.Type == InstallFile.FileType.App || fileInfo.Type == InstallFile.FileType.Pack || (fileInfo.Type == InstallFile.FileType.Option && fileInfo.IsApm());
        bool isExfat = fileInfo.Type == InstallFile.FileType.Option && !fileInfo.IsApm();

        if (isNtfsPlusVhd) {
            LOG.LogTrace("FixBpbForIv(Ntfs)");
            bytes[0] = 0xEB;
            bytes[1] = 0x52;
            bytes[2] = 0x90;
        } else if (isExfat) {
            LOG.LogTrace("FixBpbForIv(Exfat)");
            bytes[0] = 0xEB;
            bytes[1] = 0x76;
            bytes[2] = 0x90;
        }

        return bytes;
    }

    private static void AddFilesToFileSystemRecursive(DiscFileSystem fs, string root, string path = null) {
        if (path == null) {
            path = root;
        }

        foreach (string dir in Directory.EnumerateDirectories(path)) {
            String dirRelPath = Path.GetRelativePath(root, dir);
            LOG.LogDebug("Adding directory " + dirRelPath + " to file system");
            fs.CreateDirectory(dirRelPath);
            AddFilesToFileSystemRecursive(fs, root, dir);
        }

        foreach (string file in Directory.EnumerateFiles(path)) {
            string fileRelPath = Path.GetRelativePath(root, file);
            FileInfo fi = new FileInfo(file);
            LOG.LogInformation("Writing " + fileRelPath + " (" + Util.BytesToString(fi.Length) + ") to file system");
            using (Stream writer = fs.OpenFile(fileRelPath, FileMode.Create)) {
                using (Stream reader = fi.OpenRead()) {
                    reader.CopyTo(writer);
                }
            }
        }
    }

    private static long GatherDirectorySizeRecursive(string path) {
        return Directory.EnumerateDirectories(path).Sum(GatherDirectorySizeRecursive) + Directory.EnumerateFiles(path).Select(f => new FileInfo(f).Length).Sum();
    }
}