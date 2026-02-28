using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd {
    class GlobalOptions {
        [Option('s', "silent", Required = false, HelpText = "Disables log output to console.")]
        [UsedImplicitly]
        public bool Silent { get; }
    }
}