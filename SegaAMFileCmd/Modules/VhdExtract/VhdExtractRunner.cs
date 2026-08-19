using DiscUtils;
using DiscUtils.ExFat;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DiscUtils.Streams;
using DiscUtils.Vhd;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.VhdExtract {
    class VhdExtractRunner {
        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!File.Exists(opts.FileName)) {
                Program.CmdLog.LogError("Specified file not found: {f}", opts.FileName);
                return 1;
            }

            Program.CmdLog.LogInformation("Reading " + opts.FileName + "...");
            Stream input = File.OpenRead(opts.FileName);

            Disk virtualDisk = new Disk(input, Ownership.Dispose);
            Program.CmdLog.LogTrace("VHD disk geometry: " + virtualDisk.Geometry);

            PartitionTable partitionTable = virtualDisk.Partitions;
            if (partitionTable == null) {
                throw new IOException("Partition table in inner .vhd file not found");
            }

            if (partitionTable.Partitions.Count == 0) {
                throw new IOException("No partitions found in inner .vhd file");
            }

            SparseStream partitionStream = partitionTable.Partitions[0].Open();

            DiscFileSystem fs;

            if (opts.ForceNtfs) {
                fs = new NtfsFileSystem(partitionStream);
            } else {
                fs = new ExFatFileSystem(partitionStream);
            }

            FsUtils.ExtractRecursive(Program.CmdLog, fs.Root, opts.OutputDirectory, null, opts.SkipExisting);

            return 0;
        }

        private static FscryptFile BuildParentChain(IEnumerable<string> parents, bool isApm, bool noVerify) {
            FscryptFile parentContainer = null;
            Program.CmdLog.LogDebug(parents.Count() + " parent containers specified");
            foreach (string parent in parents) {
                if (!File.Exists(parent)) {
                    throw new IOException("Parent file not found: " + parent);
                }

                Program.CmdLog.LogInformation("Reading parent container: " + parent);

                InstallFileName fileNameParent = InstallFileName.Parse(parent, isApm);
                Stream inputInner = File.OpenRead(parent);
                if (fileNameParent.Type == InstallFileName.FileType.Option) {
                    parentContainer = new ApmOptFile(inputInner, (ApmOptFile)parentContainer);
                } else {
                    parentContainer = new AppFile(inputInner, (AppFile)parentContainer, !noVerify);
                }
            }

            return parentContainer;
        }

        private static FscryptFile DetectContainerType(Stream input, bool noVerify, InstallFileName fileName, bool isApm, FscryptFile parentChain, bool forceNtfs) {
            FscryptFile container;

            if (fileName.Type == InstallFileName.FileType.App || fileName.Type == InstallFileName.FileType.Pack || forceNtfs) {
                container = new AppFile(input, (AppFile)parentChain, !noVerify);
            } else if (fileName.Type == InstallFileName.FileType.Option) {
                if (isApm) {
                    Program.CmdLog.LogInformation("Detected APM .opt file");
                    container = new ApmOptFile(input, (ApmOptFile)parentChain);
                } else {
                    Program.CmdLog.LogInformation("Detected regular .opt file");
                    container = new OptFile(input, (OptFile)parentChain, !noVerify);
                }
            } else {
                throw new IOException("Unknown container: " + fileName.Type);
            }

            return container;
        }
    }
}