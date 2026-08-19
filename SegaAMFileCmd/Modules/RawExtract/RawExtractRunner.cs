using DiscUtils;
using DiscUtils.ExFat;
using DiscUtils.Ntfs;
using DiscUtils.Streams;
using DiscUtils.Vhd;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.RawExtract {
    class RawExtractRunner {
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

            DiscFileSystem fs;

            if (opts.ForceNtfs) {
                fs = new NtfsFileSystem(input);
            } else {
                fs = new ExFatFileSystem(input);
            }

            FsUtils.ExtractRecursive(Program.CmdLog, fs.Root, opts.OutputDirectory, null, opts.SkipExisting);

            return 0;
        }
    }
}