using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.FSExtract {
    [Verb("fs-extract", HelpText = "Extract or check fscrypt containers")]
    class Options : GlobalOptions {
        [Option('k', "key", Required = false, HelpText = "The path to keys.txt, used to decrypt files", Default = "keys.txt")]
        [UsedImplicitly]
        public String KeyFile { get; set; }

        [Option("no-verify", Required = false, HelpText = "Do not check CRC32 and HMAC values")]
        public bool NoVerify { get; set; }

        [Option("no-extract", Required = false, HelpText = "Do not check extract files, just check the container")]
        public bool NoExtract { get; set; }

        [Option("no-extract-inner", Required = false, HelpText = "Do not check extract fscrypt containers within the container")]
        public bool NoExtractInner { get; set; }

        [Option('p', "parent", Required = false, HelpText = "Parent containers in ascending order.")]
        public IEnumerable<string> Parents { get; set; }

        [Option('i', "parent-inner", Required = false, HelpText = "Parent containers for inner containers in ascending order.")]
        public IEnumerable<string> ParentsInner { get; set; }

        [Option("skip", Required = false, HelpText = "Skip extracting files that already exist")]
        public bool SkipExisting { get; set; }

        [Value(0, Required = true, HelpText = "The file name (.app, .opt, .pack)")]
        [UsedImplicitly]
        public string FileName { get; set; }

        [Value(1, Required = false, HelpText = "The path where extracted files should be stored")]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }
    }
}