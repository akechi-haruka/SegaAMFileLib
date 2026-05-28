using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.FSGenerate {
    class FSGenerateRunner {
        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!Directory.Exists(opts.InputDirectory)) {
                Program.CmdLog.LogError("Specified directory not found: {f}", opts.InputDirectory);
                return 1;
            }

            EncryptionEnvironment.Initialize(opts.KeyFile);

            if (opts.IcfPath != null) {
            }

            DateTime parsedTimestamp = DateTime.Now;
            if (opts.Timestamp != null) {
                if (!DateTime.TryParse(opts.Timestamp, out parsedTimestamp)) {
                    Program.CmdLog.LogError("Failed to parse given game timestamp: " + opts.Timestamp);
                    return 1;
                }
            }

            Version parsedBaseVersion = null;
            if (opts.BaseVersion != null) {
                if (!Version.TryParse(opts.BaseVersion, out parsedBaseVersion)) {
                    Program.CmdLog.LogError("Failed to parse given base version: " + opts.BaseVersion);
                    return 1;
                }
            }

            Version parsedSystemVersion = null;
            if (opts.SystemVersion != null) {
                if (!Version.TryParse(opts.SystemVersion, out parsedSystemVersion)) {
                    Program.CmdLog.LogError("Failed to parse given base version: " + opts.SystemVersion);
                    return 1;
                }
            }

            DateTime? parsedBaseTimestamp = null;
            if (opts.BaseTimestamp != null) {
                if (!DateTime.TryParse(opts.BaseTimestamp, out DateTime tmp)) {
                    Program.CmdLog.LogError("Failed to parse given base timestamp: " + opts.BaseTimestamp);
                    return 1;
                }

                parsedBaseTimestamp = tmp;
            }

            InstallFileName fileNameInfo = InstallFileName.Create(opts.Type, opts.Type == InstallFileName.FileType.Pack ? GameID.SYSTEM_APP_ID : opts.GameId, Version.Parse(opts.Version), opts.OptionName, parsedTimestamp, opts.Sequence, parsedBaseVersion);

            FscryptContainerGenerator.Create(opts.InputDirectory, opts.OutputDirectory, fileNameInfo, parsedSystemVersion, parsedBaseTimestamp, opts.PlatformId, (byte)opts.PlatformGeneration, (byte)opts.Unknown);

            return 0;
        }
    }
}