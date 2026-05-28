using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.ICFCommit {
    class ICFCommitRunner {
        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            EncryptionEnvironment.Initialize(opts.KeyFile);

            byte[] data = File.ReadAllBytes(opts.FileName);

            InstallationConfigurationFile icf = new InstallationConfigurationFile(data, EncryptionEnvironment.Icf, true);

            IcfEntryRecord[] records = icf.GetRecords();
            icf.ClearRecords();
            foreach (IcfEntryRecord record in records) {
                IcfEntryRecord newRecord = record;
                if ((record.entryFlags & EntryFlags.Uncommited) != EntryFlags.Invalid) {
                    newRecord.entryFlags = EntryFlags.Enabled1 | EntryFlags.Enabled2;
                    Program.CmdLog.LogInformation("Commited an entry for: " + newRecord.GetFileName(icf.Header));
                }

                icf.AddRecord(newRecord);
            }

            data = icf.Save();

            data = SegaAes.EncryptFromEnv(data, EncryptionEnvironment.Icf);

            File.WriteAllBytes(opts.FileName, data);

            Program.CmdLog.LogInformation("ICF written to: {f}", opts.FileName);

            return 0;
        }
    }
}