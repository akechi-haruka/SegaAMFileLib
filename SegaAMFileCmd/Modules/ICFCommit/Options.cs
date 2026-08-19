using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFCommit {
    [Verb("icf-commit", HelpText = "Commit an .icf file that is having update(s) being written")]
    class Options : GlobalOptions {
        [Option('k', "key", Required = false, HelpText = "The path to keys.txt, used to decrypt the ICF file.", Default = "keys.txt")]
        [UsedImplicitly]
        public String KeyFile { get; set; }

        [Value(0, Required = true, HelpText = "The ICF to commit")]
        [UsedImplicitly]
        public string FileName { get; set; }
    }
}