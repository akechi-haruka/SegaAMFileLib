using CommandLine;
using Haruka.Arcade.SegaAMFileCmd.Modules.DLIWrite;
using Haruka.Arcade.SegaAMFileCmd.Modules.ICFView;
using Haruka.Arcade.SegaAMFileCmd.Modules.ICFWrite;
using Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSet;
using Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSync;
using Haruka.Common;
using Haruka.Common.Configuration;
using Microsoft.Extensions.Logging;
using Options = Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSet.Options;

namespace Haruka.Arcade.SegaAMFileCmd {
    static class Program {
        public static ILogger CmdLog;

        private static int Main(string[] args) {
            try {
                return Parser.Default.ParseArguments
                        <Options, Modules.SysfileSync.Options, Modules.ICFView.Options, Modules.ICFWrite.Options, Modules.DLIWrite.Options>(args)
                    .MapResult<Options, Modules.SysfileSync.Options, Modules.ICFView.Options, Modules.ICFWrite.Options, Modules.DLIWrite.Options, int>(
                        SysfileSetRunner.Run,
                        SysfileSyncRunner.Run,
                        ICFViewRunner.Run,
                        ICFWriteRunner.Run,
                        DLIWriteRunner.Run,
                        _ => 1);
            } catch (Exception ex) {
                if (CmdLog != null) {
                    CmdLog.LogCritical(ex, "An error has occurred");
                } else {
                    Console.WriteLine("An error has occurred");
                    Console.WriteLine(ex);
                }

                return Int32.MinValue;
            } finally {
                CmdLog?.LogInformation("Exiting");
            }
        }

        internal static void SetGlobalOptions(GlobalOptions options) {
            AppConfig.Initialize();
            Log.Initialize(options.Silent);
            CmdLog = Log.GetOrCreate("Cmd ");
        }
    }
}