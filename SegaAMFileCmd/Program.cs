using CommandLine;
using Haruka.Arcade.SegaAMFileCmd.Modules.DLIWrite;
using Haruka.Arcade.SegaAMFileCmd.Modules.ICFView;
using Haruka.Arcade.SegaAMFileCmd.Modules.ICFWrite;
using Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSet;
using Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSync;
using Haruka.Arcade.SegaAMFileLib;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd {
    static class Program {
        public static ILogger Log;

        private static int Main(string[] args) {
            try {
                return Parser.Default.ParseArguments
                        <Modules.SysfileSet.Options, Modules.SysfileSync.Options, Modules.ICFView.Options, Modules.ICFWrite.Options, Modules.DLIWrite.Options>(args)
                    .MapResult<Modules.SysfileSet.Options, Modules.SysfileSync.Options, Modules.ICFView.Options, Modules.ICFWrite.Options, Modules.DLIWrite.Options, int>(
                        SysfileSetRunner.Run,
                        SysfileSyncRunner.Run,
                        ICFViewRunner.Run,
                        ICFWriteRunner.Run,
                        DLIWriteRunner.Run,
                        _ => 1);
            } catch(Exception ex) {
                if (Log != null) {
                    Log.LogCritical(ex, "An error has occurred");
                } else {
                    Console.WriteLine("An error has occurred");
                    Console.WriteLine(ex);
                }

                return Int32.MinValue;
            } finally {
                Log?.LogInformation("Exiting");
            }
        }

        internal static void SetGlobalOptions(GlobalOptions options) {
            Logging.Initialize(Configuration.Initialize(), options.Silent, options.LogFile);
            Log = Logging.Factory.CreateLogger(nameof(Program));
        }

    }
}
