using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSet {
    class SysfileSetRunner {
        

        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!File.Exists(opts.FileName)) {
                Program.Log.LogError("Specified file not found: {f}", opts.FileName);
                return 1;
            }

            byte[] data = File.ReadAllBytes(opts.FileName);
            SysData sysfile = new SysData(data);

            switch (opts.Action) {
                case SetAction.SetCredits:
                    sysfile.Backup.creditData.player[0].credit = (byte)opts.Value;
                    break;
            }
            
            Program.Log.LogInformation("Success: {a}", opts.Action);

            data = SysData.UpdateRecord(data, sysfile.Backup);
            File.WriteAllBytes(opts.FileName, data);
            
            Program.Log.LogInformation("File saved to: {f}", opts.FileName);
            
            return 0;
        }
    }
}
