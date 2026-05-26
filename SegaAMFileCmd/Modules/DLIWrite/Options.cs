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

        [Option('e', "exists", Required = false, HelpText = "The filenames of files that must exist for this download")]
        [UsedImplicitly]
        public IEnumerable<string> Exists { get; set; }

        [Option("base-icf", Required = false, HelpText = "Generates filenames that must exist for this download from an existing ICF file")]
        [UsedImplicitly]
        public string BaseIcfFileName { get; set; }

        [Option("icf-system-only", Required = false, HelpText = "Only use the system record to generate EXIST from an existing ICF file")]
        [UsedImplicitly]
        public bool IcfSystemOnly { get; set; }

        [Option('k', "key", Required = false, HelpText = "The path to keys.txt, used to decrypt ICF files.", Default = "keys.txt")]
        [UsedImplicitly]
        public String KeyFile { get; set; }

        [Option("ignore-crc", Required = false, Hidden = true)]
        public bool IgnoreCrc { get; set; }

        [Value(0, Required = true, HelpText = "The type of the DLI file (App,Opt)")]
        [UsedImplicitly]
        public DliType Type { get; set; }

        [Value(1, Required = true, HelpText = "The 4 letter game ID")]
        [UsedImplicitly]
        public string GameId { get; set; }

        [Value(2, Required = true, HelpText = "The name of the resulting DLI file")]
        [UsedImplicitly]
        public string OutputFile { get; set; }

        [Value(3, Required = true, HelpText = "The full URL(s) to the file(s) to download")]
        [UsedImplicitly]
        public IEnumerable<string> Urls { get; set; }
    }
}