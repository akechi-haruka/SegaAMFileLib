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

            InstallationConfigurationFile icf = new InstallationConfigurationFile(data, EncryptionEnvironment.Icf, opts.IgnoreCrc);

            IcfHeaderRecord header = icf.Header;
            Program.CmdLog.LogInformation("App ID: {a}", header.GetAppId());
            Program.CmdLog.LogInformation("Platform ID: {i}{g}", header.GetPlatformId(false), header.platformGeneration);

            for (int i = 0; i < icf.GetRecordCount(); i++) {
                IcfEntryRecord record = icf.GetRecord(i);
                if (record.typeFlags != IcfType.Option || !opts.IgnoreOption) {
                    Program.CmdLog.LogInformation("Record " + i);
                    PrintRecordInformation(header, record);
                }
            }

            return 0;
        }

        private static void PrintRecordInformation(IcfHeaderRecord header, IcfEntryRecord? record) {
            if (record == null) {
                Program.CmdLog.LogWarning("Record not found");
                return;
            }

            IcfEntryRecord value = record.Value;


            String timeString;
            try {
                timeString = value.timestamp.ToString();
            } catch (Exception ex) {
                timeString = "<invalid>: " + ex.Message;
            }

            String patchTimeString;
            try {
                patchTimeString = value.patchTimestamp.ToString();
            } catch (Exception ex) {
                patchTimeString = "<invalid>: " + ex.Message;
            }

            Program.CmdLog.LogInformation("- Record Type: {v}", value.typeFlags);
            Program.CmdLog.LogInformation("- Flags: 0x{v}", value.entryFlags.ToString("X"));
            Program.CmdLog.LogInformation("- Required Version: {v}", value.requiredVersion);
            Program.CmdLog.LogInformation("- Version: {v}", value.version);
            Program.CmdLog.LogInformation("- Date: {d}", timeString);
            Program.CmdLog.LogInformation("- AMFS file name: {f}", value.GetFileName(header));

            if (value.IsPatch()) {
                Program.CmdLog.LogInformation("- Patch Version: {d}", value.patchVersion);
                Program.CmdLog.LogInformation("- Patch Date: {d}", patchTimeString);
                Program.CmdLog.LogInformation("- Patch Required Version: {d}", value.patchRequiredVersion);
            }
        }
    }
}