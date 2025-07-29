using CommandLine;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.DLI;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.DLIWrite {
    [Verb("dli-write", HelpText = "Create DLI .ini files")]
    class Options : GlobalOptions {
        [Option("order-time", Required = false, HelpText = "Date/Time when the files should be downloaded")]
        [UsedImplicitly]
        public DateTime? OrderTime { get; set; }
        
        [Option("release-time", Required = false, HelpText = "Date/Time when the files should be installed")]
        [UsedImplicitly]
        public DateTime? ReleaseTime { get; set; }
        
        [Option("report-url", Required = false, HelpText = "Download progress reporting URL")]
        [UsedImplicitly]
        public string ReportUrl { get; set; }

        [Value(0, Required = true, HelpText = "The type of the DLI file (App,Opt)")]
        [UsedImplicitly]
        public DliType Type { get; set; }

        [Value(1, Required = true, HelpText = "The 4 letter game ID")]
        [UsedImplicitly]
        public string GameId { get; set; }

        [Value(2, Required = true, HelpText = "The name of the resulting DLI file")]
        [UsedImplicitly]
        public string OutputFile { get; set; }

        [Value(3, Required = true, HelpText = "The file(s) to download")]
        [UsedImplicitly]
        public string[] Urls { get; set; }
    }
}