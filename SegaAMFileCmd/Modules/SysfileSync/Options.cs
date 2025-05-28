using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSync {

    [Verb("sysfile-sync", HelpText = "Synchronize the data between multiple sysfile.dat")]
    class Options : GlobalOptions {
        
        [Value(0, Required = true, HelpText = "The first sysfile.dat")]
        [UsedImplicitly]
        public string FileName1 { get; }
        
        [Value(1, Required = true, HelpText = "The second sysfile.dat")]
        [UsedImplicitly]
        public string FileName2 { get; }

        [Value(2, Required = true, HelpText = "How to synchronize credits (File1, File2, Higher, Lower)", Default = SyncType.Higher)]
        [UsedImplicitly]
        public SyncType SyncCredits { get; }

        [Value(3, Required = true, HelpText = "How to synchronize bookkeeping stats (File1, File2, Higher, Lower)", Default = SyncType.Higher)]
        [UsedImplicitly]
        public SyncType SyncBookkeeping { get; }

    }

    enum SyncType {
        File1,
        File2,
        Higher,
        Lower
    }
}
