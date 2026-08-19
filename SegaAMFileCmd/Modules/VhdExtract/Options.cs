using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.VhdExtract {
    [Verb("vhd-extract", HelpText = "Extract vhd containers")]
    class Options : GlobalOptions {
        [Option("skip", Required = false, HelpText = "Skip extracting files that already exist")]
        public bool SkipExisting { get; set; }

        [Option("ntfs", Required = false, HelpText = "Force NTFS")]
        public bool ForceNtfs { get; set; }

        [Value(0, Required = true, HelpText = "The file name (.vhd)")]
        [UsedImplicitly]
        public string FileName { get; set; }

        [Value(1, Required = false, HelpText = "The path where extracted files should be stored")]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }
    }
}