using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Microsoft.Extensions.Logging;
using Version = Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.Version;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFWrite {
    class ICFWriteRunner {
        private const String KEY_FILE_NAME = "icf_key.bin";
        private const String IV_FILE_NAME = "icf_iv.bin";

        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            EncryptionEnvironment.Initialize(opts.KeyFile);

            if (opts.GameId.Length != 4) {
                Program.CmdLog.LogError("Bad length for game ID: {i}", opts.GameId);
                return 1;
            }

            if (opts.PlatformId.Length != 4) {
                Program.CmdLog.LogError("Bad length for platform ID: {i}", opts.GameId);
                return 1;
            }

            if (opts.PlatformId[^1] < '0' || opts.PlatformId[^1] > '9') {
                Program.CmdLog.LogError("Final character of platform ID must be a number: {i}", opts.GameId);
                return 1;
            }

            if (!System.Version.TryParse(opts.SystemVersion, out System.Version parsedSystemVersion)) {
                Program.CmdLog.LogError("Failed to read given system version number: " + opts.GameVersion);
                return 1;
            }

            if (!System.Version.TryParse(opts.GameVersion, out System.Version parsedGameVersion)) {
                Program.CmdLog.LogError("Failed to read given game version number: " + opts.GameVersion);
                return 1;
            }

            if (!DateTime.TryParse(opts.SystemTimestamp, out DateTime parsedSystemTimestamp)) {
                Program.CmdLog.LogError("Failed to parse given game timestamp: " + opts.GameTimestamp);
                return 1;
            }

            DateTime parsedGameTimestamp = DateTime.Now;
            if (!String.IsNullOrWhiteSpace(opts.GameTimestamp)) {
                if (!DateTime.TryParse(opts.GameTimestamp, out parsedGameTimestamp)) {
                    Program.CmdLog.LogError("Failed to parse given game timestamp: " + opts.GameTimestamp);
                    return 1;
                }
            }

            InstallationConfigurationFile icf = new InstallationConfigurationFile();

            icf.Header.SetAppId(opts.GameId);
            icf.Header.SetPlatformId(opts.PlatformId.Substring(0, 3));
            icf.Header.platformGeneration = Convert.ToByte(opts.PlatformId.Substring(3));

            Version systemVersion = new Version {
                major = (ushort)parsedSystemVersion.Major,
                minor = (byte)parsedSystemVersion.Minor,
                build = (byte)parsedSystemVersion.Build
            };
            Timestamp systemTimestamp = new Timestamp(parsedSystemTimestamp);
            Version gameVersion = new Version {
                major = (ushort)parsedGameVersion.Major,
                minor = (byte)parsedGameVersion.Minor,
                build = (byte)parsedGameVersion.Build
            };
            Timestamp gameTimestamp = new Timestamp(parsedGameTimestamp);

            IcfEntryRecord systemEntry = new IcfEntryRecord {
                typeFlags = IcfType.System,
                entryFlags = EntryFlags.Enabled1 | EntryFlags.Enabled2,
                timestamp = systemTimestamp,
                requiredVersion = systemVersion,
                version = systemVersion
            };
            icf.AddRecord(systemEntry);
            IcfEntryRecord appEntry = new IcfEntryRecord {
                typeFlags = IcfType.App,
                entryFlags = EntryFlags.Enabled1 | EntryFlags.Enabled2,
                timestamp = gameTimestamp,
                requiredVersion = systemVersion,
                version = gameVersion
            };
            icf.AddRecord(appEntry);

            byte[] data = icf.Save();

            data = SegaAes.EncryptFromEnv(data, EncryptionEnvironment.Icf);

            File.WriteAllBytes(opts.FileName, data);

            Program.CmdLog.LogInformation("ICF written to: {f}", opts.FileName);

            return 0;
        }
    }
}