using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.BootIdRead {
    [Verb("bootid", HelpText = "Read bootId from fscrypt containers")]
    class Options : GlobalOptions {
        [Option('k', "key", Required = false, HelpText = "The path to keys.txt, used to decrypt bootids", Default = "keys.txt")]
        [UsedImplicitly]
        public String KeyFile { get; set; }

        [Option("with-strings", Required = false, HelpText = "Dump the string table")]
        public bool WithStrings { get; set; }

        [Value(0, Required = true, HelpText = "The file name (.app, .opt, .pack)")]
        [UsedImplicitly]
        public string FileName { get; set; }
    }
}