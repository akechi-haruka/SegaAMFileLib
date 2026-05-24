using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFWrite {
    [Verb("icf-write", HelpText = "Create .icf files")]
    class Options : GlobalOptions {
        [Option('k', "key", Required = false, HelpText = "The path to keys.txt, used to decrypt the ICF file.", Default = "keys.txt")]
        [UsedImplicitly]
        public String KeyFile { get; set; }

        [Option('f', "file-name", Required = false, HelpText = "The output file name.", Default = "ICF1")]
        [UsedImplicitly]
        public string FileName { get; set; }

        [Value(0, Required = true, HelpText = "The platform ID (including platform generation) to be stored. (4 characters)")]
        [UsedImplicitly]
        public string PlatformId { get; set; }

        [Value(1, Required = true, HelpText = "The system version to store (Format: X.YY.ZZ)")]
        [UsedImplicitly]
        public string SystemVersion { get; set; }

        [Value(2, Required = true, HelpText = "The system timestamp to store")]
        [UsedImplicitly]
        public string SystemTimestamp { get; set; }

        [Value(3, Required = true, HelpText = "The game ID to be stored. (4 characters)")]
        [UsedImplicitly]
        public string GameId { get; set; }

        [Value(4, Required = true, HelpText = "The game version to store (Format: X.YY.ZZ)")]
        [UsedImplicitly]
        public string GameVersion { get; set; }

        [Value(5, Required = false, HelpText = "The game timestamp to store", Default = "")]
        [UsedImplicitly]
        public string GameTimestamp { get; set; }
    }
}