using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFWrite {

    [Verb("icf-write", HelpText = "Create .icf files")]
    class Options : GlobalOptions {

        [Option('k', "key", Required = false, HelpText = "The ICF encryption key in hexadecimal format", Default = 1)]
        [UsedImplicitly]
        public String Key { get; }

        [Option('i', "iv", Required = false, HelpText = "The ICF encryption IV in hexadecimal format", Default = 1)]
        [UsedImplicitly]
        public String Iv { get; }

        [Option('f', "file-name", Required = false, HelpText = "The output file name.", Default = "ICF1")]
        [UsedImplicitly]
        public string FileName { get; }

        [Value(0, Required = true, HelpText = "The game ID to be stored. (4 characters)")]
        [UsedImplicitly]
        public string GameId { get; }

        [Value(1, Required = true, HelpText = "The platform ID (including platform generation) to be stored. (4 characters)")]
        [UsedImplicitly]
        public string PlatformId { get; }

        [Value(2, Required = true, HelpText = "The version to store (Format: X.YY.ZZ)")]
        [UsedImplicitly]
        public string Version { get; }

        [Value(3, Required = false, HelpText = "The timestamp to store", Default = "")]
        [UsedImplicitly]
        public string Timestamp { get; }
    }
}
