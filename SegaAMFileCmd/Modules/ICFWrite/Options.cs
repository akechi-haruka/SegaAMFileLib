using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFWrite {
    [Verb("icf-write", HelpText = "Create .icf files")]
    class Options : GlobalOptions {
        [Option('k', "key", Required = false, HelpText = "The ICF encryption key in hexadecimal format")]
        [UsedImplicitly]
        public String Key { get; set; }

        [Option('i', "iv", Required = false, HelpText = "The ICF encryption IV in hexadecimal format")]
        [UsedImplicitly]
        public String Iv { get; set; }

        [Option('f', "file-name", Required = false, HelpText = "The output file name.", Default = "ICF1")]
        [UsedImplicitly]
        public string FileName { get; set; }

        [Value(0, Required = true, HelpText = "The game ID to be stored. (4 characters)")]
        [UsedImplicitly]
        public string GameId { get; set; }

        [Value(1, Required = true, HelpText = "The platform ID (including platform generation) to be stored. (4 characters)")]
        [UsedImplicitly]
        public string PlatformId { get; set; }

        [Value(2, Required = true, HelpText = "The version to store (Format: X.YY.ZZ)")]
        [UsedImplicitly]
        public string Version { get; set; }

        [Value(3, Required = false, HelpText = "The timestamp to store", Default = "")]
        [UsedImplicitly]
        public string Timestamp { get; set; }
    }
}