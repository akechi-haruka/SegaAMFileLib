using CommandLine;
using Haruka.Arcade.SegaAMFileCmd.Modules.ICFView;
using Haruka.Arcade.SegaAMFileCmd.Modules.ICFWrite;
using Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSet;
using Haruka.Arcade.SegaAMFileLib;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd {
    static class Program {
        public static ILogger Log;

        static int Main(string[] args) {
            Log = Logging.Factory.CreateLogger(nameof(Program));
            try {
                return Parser.Default.ParseArguments
                        <SysfileSetRunner, ICFViewRunner, ICFWriteRunner>(args)
                    .MapResult<Modules.SysfileSet.Options, Modules.ICFView.Options, Modules.ICFWrite.Options, int>(
                        SysfileSetRunner.Run,
                        ICFViewRunner.Run,
                        ICFWriteRunner.Run,
                        _ => 1);
            } catch(Exception ex) {
                Log.LogCritical(ex, "An error has occurred");
                return Int32.MinValue;
            } finally {
                Log.LogInformation("Exiting");
            }
        }

        internal static void SetGlobalOptions(GlobalOptions options) {
            Logging.Initialize(Configuration.Initialize(), options.Silent, options.LogFile);
        }

    }
}
