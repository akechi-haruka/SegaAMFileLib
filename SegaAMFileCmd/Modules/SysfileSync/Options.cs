using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSync {
    [Verb("sysfile-sync", HelpText = "Synchronize the data between multiple sysfile.dat")]
    class Options : GlobalOptions {
        
        [Option('f', "file", Required = false, HelpText = "The sysfile (1 or 2) which should be updated. Otherwise both are updated.")]
        [UsedImplicitly]
        public int TargetFile { get; set; }

        [Option('d', HelpText = "The differential sysfile.dat")]
        [UsedImplicitly]
        public string Differential { get; set; }
        
        [Value(0, Required = true, HelpText = "The first sysfile.dat")]
        [UsedImplicitly]
        public string FileName1 { get; set; }

        [Value(1, Required = true, HelpText = "The second sysfile.dat")]
        [UsedImplicitly]
        public string FileName2 { get; set; }

        [Value(2, Required = true, HelpText = "How to synchronize credits (NoChange, File1, File2, Higher, Lower)", Default = SyncType.Higher)]
        [UsedImplicitly]
        public SyncType SyncCredits { get; set; }

        [Value(3, Required = true, HelpText = "How to synchronize bookkeeping stats (NoChange, File1, File2, Higher, Lower, Combine, DifferentialToFile1, DifferentialToFile2)", Default = SyncType.Higher)]
        [UsedImplicitly]
        public SyncType SyncBookkeeping { get; set; }
    }

    enum SyncType {
        NoChange,
        File1,
        File2,
        Higher,
        Lower,
        Combine,
        DifferentialToFile1,
        DifferentialToFile2
    }
}