using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFView {
    [Verb("icf-view", HelpText = "View versions and data from .icf files")]
    class Options : GlobalOptions {
        [Option('k', "key", Required = false, HelpText = "The path to keys.txt, used to decrypt the ICF file.", Default = "keys.txt")]
        [UsedImplicitly]
        public String KeyFile { get; set; }

        [Value(0, Required = true, HelpText = "The path to the ICF file.")]
        [UsedImplicitly]
        public string FileName { get; set; }
    }
}