using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFView {
    class ICFViewRunner {
        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!File.Exists(opts.FileName)) {
                Program.CmdLog.LogError("Specified file not found: {f}", opts.FileName);
                return 1;
            }

            EncryptionEnvironment.Initialize(opts.KeyFile);

            byte[] data = File.ReadAllBytes(opts.FileName);

            InstallationConfigurationFile icf = new InstallationConfigurationFile(data, EncryptionEnvironment.Icf);

            ICFHeaderRecord header = icf.Header;
            Program.CmdLog.LogInformation("App ID: {a}", header.GetAppId());
            Program.CmdLog.LogInformation("Platform ID: {i}{g}", header.GetPlatformId(false), header.platformGeneration);

            for (int i = 0; i < icf.GetRecordCount(); i++) {
                ICFEntryRecord record = icf.GetRecord(i);
                Program.CmdLog.LogInformation("Record " + i);
                PrintRecordInformation(record);
            }

            return 0;
        }

        private static void PrintRecordInformation(ICFEntryRecord? record) {
            if (record == null) {
                Program.CmdLog.LogWarning("Record not found");
                return;
            }

            Program.CmdLog.LogInformation("- Record Type: {v}", record.Value.typeFlags);
            Program.CmdLog.LogInformation("- Required Version: {v}", record.Value.requiredVersion);
            Program.CmdLog.LogInformation("- Version: {v}", record.Value.version);
            Program.CmdLog.LogInformation("- Date: {d}", record.Value.timestamp);
        }
    }
}