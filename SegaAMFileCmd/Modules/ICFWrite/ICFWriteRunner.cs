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

            if (!File.Exists(opts.FileName)) {
                Program.Log.LogError("Specified file not found: {f}", opts.FileName);
                return 1;
            }

            if (opts.Key == null && !File.Exists(KEY_FILE_NAME)) {
                Program.Log.LogError("Neither an encryption key was specified, nor was {f} found in the program directory.", KEY_FILE_NAME);
                return 1;
            }

            if (opts.Iv == null && !File.Exists(IV_FILE_NAME)) {
                Program.Log.LogError("Neither an encryption IV was specified, nor was {f} found in the program directory.", IV_FILE_NAME);
                return 1;
            }

            byte[] key;
            byte[] iv;

            if (opts.Key != null) {
                try {
                    key = Convert.FromHexString(opts.Key);
                } catch {
                    Program.Log.LogError("Bad format for passed encryption key.");
                    return 1;
                }
            } else {
                key = File.ReadAllBytes(KEY_FILE_NAME);
            }

            if (opts.Iv != null) {
                try {
                    iv = Convert.FromHexString(opts.Iv);
                } catch {
                    Program.Log.LogError("Bad format for passed encryption IV.");
                    return 1;
                }
            } else {
                iv = File.ReadAllBytes(IV_FILE_NAME);
            }

            if (opts.GameId.Length != 4) {
                Program.Log.LogError("Bad length for game ID: {i}", opts.GameId);
                return 1;
            }

            if (opts.PlatformId.Length != 4) {
                Program.Log.LogError("Bad length for platform ID: {i}", opts.GameId);
                return 1;
            }

            if (opts.PlatformId[^1] < '0' || opts.PlatformId[^1] > '9') {
                Program.Log.LogError("Final character of platform ID must be a number: {i}", opts.GameId);
                return 1;
            }

            if (!System.Version.TryParse(opts.Version, out System.Version parsedVersion)) {
                Program.Log.LogError("Failed to read given version number: " + opts.Version);
                return 1;
            }

            DateTime timestamp = DateTime.Now;
            if (opts.Timestamp != null) {
                if (!DateTime.TryParse(opts.Timestamp, out timestamp)) {
                    Program.Log.LogError("Failed to parse given timestamp: " + opts.Timestamp);
                    return 1;
                }    
            }

            InstallationConfigurationFile icf = new InstallationConfigurationFile();

            icf.Header.appId = opts.GameId;
            icf.Header.platformId = opts.PlatformId.Substring(0, 3);
            icf.Header.platformGeneration = Convert.ToByte(opts.PlatformId.Substring(3));

            Version ver = new Version {
                major = (ushort)parsedVersion.Major,
                minor = (byte)parsedVersion.Minor,
                build = (byte)parsedVersion.Revision
            };
            Timestamp time = new Timestamp(timestamp);

            ICFEntryRecord systemEntry = new ICFEntryRecord {
                typeFlags = ICFType.System,
                entryFlags = EntryFlags.Enabled1 | EntryFlags.Enabled2,
                timestamp = time,
                requiredVersion = ver,
                version = ver
            };
            icf.AddRecord(systemEntry);
            ICFEntryRecord appEntry = new ICFEntryRecord {
                typeFlags = ICFType.App,
                entryFlags = EntryFlags.Enabled1 | EntryFlags.Enabled2,
                timestamp = time,
                requiredVersion = ver,
                version = ver
            };
            icf.AddRecord(appEntry);

            byte[] data = icf.Save();

            data = SegaAes.Encrypt(data, key, iv);

            File.WriteAllBytes(opts.FileName, data);
            
            Program.Log.LogInformation("ICF written to: {f}", opts.FileName);
            
            return 0;
        }

        private static void PrintRecordInformation(ICFEntryRecord? record) {
            if (record == null) {
                Program.Log.LogWarning("Record not found");
                return;
            }

            Program.Log.LogInformation("- Required Version: {v}", record.Value.requiredVersion);
            Program.Log.LogInformation("- Version: {v}", record.Value.version);
            Program.Log.LogInformation("- Date: {d}", record.Value.timestamp);
        }
    }
}