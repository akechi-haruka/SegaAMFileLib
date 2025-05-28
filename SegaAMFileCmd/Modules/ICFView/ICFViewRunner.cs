using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFView {
    class ICFViewRunner {
        private const String KEY_FILE_NAME = "icf_key.bin";
        private const String IV_FILE_NAME = "iv_key.bin";

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

            byte[] data = File.ReadAllBytes(opts.FileName);

            InstallationConfigurationFile icf = new InstallationConfigurationFile(data, key, iv);

            ICFHeaderRecord header = icf.Header;
            Program.Log.LogInformation("App ID: {a}", header.appId);
            Program.Log.LogInformation("Platform ID: {i}{g}", header.platformId, header.platformGeneration);

            for (int i = 0; i < icf.GetRecordCount(); i++) {
                ICFEntryRecord record = icf.GetRecord(i);
                Program.Log.LogInformation("Record " + i);
                PrintRecordInformation(record);
            }

            return 0;
        }

        private static void PrintRecordInformation(ICFEntryRecord? record) {
            if (record == null) {
                Program.Log.LogWarning("Record not found");
                return;
            }
            
            Program.Log.LogInformation("- Record Type: {v}", record.Value.typeFlags);
            Program.Log.LogInformation("- Required Version: {v}", record.Value.requiredVersion);
            Program.Log.LogInformation("- Version: {v}", record.Value.version);
            Program.Log.LogInformation("- Date: {d}", record.Value.timestamp);
        }
    }
}