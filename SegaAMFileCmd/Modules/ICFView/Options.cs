using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFView {

    [Verb("icf-view", HelpText = "View versions and data from .icf files")]
    class Options : GlobalOptions {

        [Option('k', "key", Required = false, HelpText = "The ICF encryption key in hexadecimal format")]
        [UsedImplicitly]
        public String Key { get; set; }

        [Option('i', "iv", Required = false, HelpText = "The ICF encryption IV in hexadecimal format")]
        [UsedImplicitly]
        public String Iv { get; set; }

        [Value(0, Required = true, HelpText = "The path to the ICF file.")]
        [UsedImplicitly]
        public string FileName { get; set; }

    }
}
