using CommandLine;
using Haruka.Arcade.SegaAMFileLib.Misc;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.FSGenerate {
    [Verb("fs-generate", HelpText = "Create fscrypt containers")]
    class Options : GlobalOptions {
        [Option('k', "key", Required = false, HelpText = "The path to keys.txt, used to encrypt files", Default = "keys.txt")]
        [UsedImplicitly]
        public String KeyFile { get; set; }

        [Option("timestamp", Required = false, HelpText = "The generation timestamp to store in the file")]
        [UsedImplicitly]
        public string Timestamp { get; set; }

        [Option("base-version", Required = false, HelpText = "The base game version this container builds upon (Format: X.YY.ZZ)")]
        [UsedImplicitly]
        public string BaseVersion { get; set; }

        [Option("base-timestamp", Required = false, HelpText = "The base game timestamp this container builds upon")]
        [UsedImplicitly]
        public string BaseTimestamp { get; set; }

        [Option("option-name", Required = false, HelpText = "The option name. Only used for .opt files")]
        [UsedImplicitly]
        public string OptionName { get; set; }

        [Option("system-version", Required = false, HelpText = "The OS version that is expected to be installed (Format: X.YY.ZZ)")]
        [UsedImplicitly]
        public string SystemVersion { get; set; }

        [Option("platform-id", Required = false, HelpText = "The 3-letter platform ID", Default = "ACA")]
        [UsedImplicitly]
        public string PlatformId { get; set; }

        [Option("platform-gen", Required = false, HelpText = "The platform generation", Default = 0)]
        [UsedImplicitly]
        public int PlatformGeneration { get; set; }

        [Option("unknown", Required = false, HelpText = "Unknown value", Default = 1)]
        [UsedImplicitly]
        public int Unknown { get; set; }

        [Option("icf", Required = false, HelpText = "Path to an ICF file to use as a base for all parameters required to make a patch for an .app or .opt file. This overrides all other flags.")]
        [UsedImplicitly]
        public string IcfPath { get; set; }

        [Value(0, Required = true, HelpText = "The container type (App, Opt, Pack)")]
        [UsedImplicitly]
        public InstallFileName.FileType Type { get; set; }

        [Value(1, Required = true, HelpText = "The 4-letter game ID. This is ignored on .pack files")]
        [UsedImplicitly]
        public string GameId { get; set; }

        [Value(2, Required = true, HelpText = "The game version for this container (Format: X.YY.ZZ)")]
        [UsedImplicitly]
        public string Version { get; set; }

        [Value(3, Required = true, HelpText = "The sequence number for this container (base = 0, patch = 1+)")]
        [UsedImplicitly]
        public byte Sequence { get; set; }

        [Value(4, Required = true, HelpText = "The input directory")]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Value(5, Required = false, HelpText = "The output directory")]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }
    }
}