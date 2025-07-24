using CommandLine;
using JetBrains.Annotations;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSet {
    [Verb("sysfile-set", HelpText = "Edit sysfile.dat")]
    class Options : GlobalOptions {
        [Value(0, Required = true, HelpText = "The location to sysfile.dat")]
        [UsedImplicitly]
        public string FileName { get; set; }

        [Value(1, Required = true, HelpText = "The operation to perform (InsertCredits,RemoveCredits,SetCredits,SetFreePlay)")]
        [UsedImplicitly]
        public SetAction Action { get; set; }

        [Value(2, Required = true, HelpText = "The value to use")]
        [UsedImplicitly]
        public int Value { get; set; }
    }

    enum SetAction {
        InsertCredits,
        RemoveCredits,
        SetCredits,
        SetFreePlay
    }
}