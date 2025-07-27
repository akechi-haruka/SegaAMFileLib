using Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSet;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.SysfileSync {
    class SysfileSyncRunner {
        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!File.Exists(opts.FileName1)) {
                Program.Log.LogError("Specified file not found: {f}", opts.FileName1);
                return 1;
            }

            if (!File.Exists(opts.FileName2)) {
                Program.Log.LogWarning("{f} does not exist, copying {f2}", opts.FileName2, opts.FileName1);
                File.Copy(opts.FileName1, opts.FileName2);
            }

            byte[] data1 = File.ReadAllBytes(opts.FileName1);
            byte[] data2 = File.ReadAllBytes(opts.FileName2);
            SysData sysfile1 = new SysData(data1);
            SysData sysfile2 = new SysData(data2);

            SysData sysfile3 = null;
            if (opts.Differential != null) {
                if (!File.Exists(opts.Differential)) {
                    Program.Log.LogError("Specified file not found: {f}", opts.Differential);
                    return 1;
                }

                sysfile3 = new SysData(File.ReadAllBytes(opts.Differential));
            }

            UpdateCredits(sysfile1, sysfile2, opts.SyncCredits);
            if (!UpdateBookkeeping(sysfile1, sysfile2, sysfile3, opts.SyncBookkeeping)) {
                return 1;
            }

            if (opts.TargetFile is 0 or 1) {
                data1 = SysData.UpdateRecord(data1, sysfile1.Backup);
                File.WriteAllBytes(opts.FileName1, data1);
                Program.Log.LogInformation("File 1 saved to: {f}", opts.FileName1);
            }

            if (opts.TargetFile is 0 or 2) {
                data2 = SysData.UpdateRecord(data2, sysfile2.Backup);
                File.WriteAllBytes(opts.FileName2, data2);
                Program.Log.LogInformation("File 2 saved to: {f}", opts.FileName2);
            }

            return 0;
        }

        private static unsafe bool UpdateBookkeeping(SysData sysfile1, SysData sysfile2, SysData unmodified, SyncType option) {
            if (option == SyncType.NoChange) {
                Program.Log.LogInformation("Bookkeeping will not be updated");
                return true;
            } else if (option == SyncType.DifferentialToFile1 || option == SyncType.DifferentialToFile2) {
                Program.Log.LogInformation("Performing differential bookkeeping sync");

                if (unmodified == null) {
                    Program.Log.LogError("No differential file was provided.");
                    return false;
                }

                SysData modified = option == SyncType.DifferentialToFile1 ? sysfile1 : sysfile2;

                Bookkeeping modifiedb = modified.Backup.bookkeeping;
                Bookkeeping unmodifiedb = unmodified.Backup.bookkeeping;
                
                uint c;
                for (int i = 0; i < 8; i++) {
                    c = modifiedb.coinChute[i] - unmodifiedb.coinChute[i];

                    sysfile1.Backup.bookkeeping.coinChute[i] += c;
                    sysfile2.Backup.bookkeeping.coinChute[i] += c;

                    Program.Log.LogInformation("Chute {i} added {v}", i, c);
                }

                c = modifiedb.coinCredit - unmodifiedb.coinCredit;
                sysfile1.Backup.bookkeeping.coinCredit += c;
                sysfile2.Backup.bookkeeping.coinCredit += c;
                Program.Log.LogInformation("Coin-Credit added {v}", c);

                c = modifiedb.emoneyCoin - unmodifiedb.emoneyCoin;
                sysfile1.Backup.bookkeeping.emoneyCoin += c;
                sysfile2.Backup.bookkeeping.emoneyCoin += c;
                Program.Log.LogInformation("E-Money-Coin added {v}", c);

                c = modifiedb.emoneyCredit - unmodifiedb.emoneyCredit;
                sysfile1.Backup.bookkeeping.emoneyCredit += c;
                sysfile2.Backup.bookkeeping.emoneyCredit += c;
                Program.Log.LogInformation("E-Money-Credit added {v}", c);

                c = modifiedb.serviceCredit - unmodifiedb.serviceCredit;
                sysfile1.Backup.bookkeeping.serviceCredit += c;
                sysfile2.Backup.bookkeeping.serviceCredit += c;
                Program.Log.LogInformation("Service Credit added {v}", c);

                c = modifiedb.totalCoin - unmodifiedb.totalCoin;
                sysfile1.Backup.bookkeeping.totalCoin += c;
                sysfile2.Backup.bookkeeping.totalCoin += c;
                Program.Log.LogInformation("Total Coin added {v}", c);

                c = modifiedb.totalCredit - unmodifiedb.totalCredit;
                sysfile1.Backup.bookkeeping.totalCredit += c;
                sysfile2.Backup.bookkeeping.totalCredit += c;
                Program.Log.LogInformation("Total Credit added {v}", c);

                return true;
            }

            for (int i = 0; i < 8; i++) {
                int c = Sync((int)sysfile1.Backup.bookkeeping.coinChute[i], (int)sysfile2.Backup.bookkeeping.coinChute[i], option);

                sysfile1.Backup.bookkeeping.coinChute[i] = (uint)c;
                sysfile2.Backup.bookkeeping.coinChute[i] = (uint)c;

                Program.Log.LogInformation("Chute {i} synchronized to {v}", i, c);
            }

            int v = Sync((int)sysfile1.Backup.bookkeeping.coinCredit, (int)sysfile2.Backup.bookkeeping.coinCredit, option);
            sysfile1.Backup.bookkeeping.coinCredit = (uint)v;
            sysfile2.Backup.bookkeeping.coinCredit = (uint)v;
            Program.Log.LogInformation("Coin-Credit synchronized to {v}", v);

            v = Sync((int)sysfile1.Backup.bookkeeping.emoneyCoin, (int)sysfile2.Backup.bookkeeping.emoneyCoin, option);
            sysfile1.Backup.bookkeeping.emoneyCoin = (uint)v;
            sysfile2.Backup.bookkeeping.emoneyCoin = (uint)v;
            Program.Log.LogInformation("E-Money-Coin synchronized to {v}", v);

            v = Sync((int)sysfile1.Backup.bookkeeping.emoneyCredit, (int)sysfile2.Backup.bookkeeping.emoneyCredit, option);
            sysfile1.Backup.bookkeeping.emoneyCredit = (uint)v;
            sysfile2.Backup.bookkeeping.emoneyCredit = (uint)v;
            Program.Log.LogInformation("E-Money-Credit synchronized to {v}", v);

            v = Sync((int)sysfile1.Backup.bookkeeping.serviceCredit, (int)sysfile2.Backup.bookkeeping.serviceCredit, option);
            sysfile1.Backup.bookkeeping.serviceCredit = (uint)v;
            sysfile2.Backup.bookkeeping.serviceCredit = (uint)v;
            Program.Log.LogInformation("Service Credit synchronized to {v}", v);

            v = Sync((int)sysfile1.Backup.bookkeeping.totalCoin, (int)sysfile2.Backup.bookkeeping.totalCoin, option);
            sysfile1.Backup.bookkeeping.totalCoin = (uint)v;
            sysfile2.Backup.bookkeeping.totalCoin = (uint)v;
            Program.Log.LogInformation("Total Coin synchronized to {v}", v);

            v = Sync((int)sysfile1.Backup.bookkeeping.totalCredit, (int)sysfile2.Backup.bookkeeping.totalCredit, option);
            sysfile1.Backup.bookkeeping.totalCredit = (uint)v;
            sysfile2.Backup.bookkeeping.totalCredit = (uint)v;
            Program.Log.LogInformation("Total Credit synchronized to {v}", v);

            return true;
        }

        private static void UpdateCredits(SysData sysfile1, SysData sysfile2, SyncType option) {
            if (option == SyncType.NoChange) {
                Program.Log.LogInformation("Credits will not be updated");
                return;
            }

            int v = Sync(sysfile1.Backup.creditData.player[0].credit, sysfile2.Backup.creditData.player[0].credit, option);
            int r = Sync(sysfile1.Backup.creditData.player[0].remain, sysfile2.Backup.creditData.player[0].remain, option);

            Program.Log.LogInformation("Credits synchronized to {v}", v);
            Program.Log.LogInformation("Remain synchronized to {v}", r);

            sysfile1.Backup.creditData.player[0].credit = (byte)v;
            sysfile2.Backup.creditData.player[0].credit = (byte)v;
            sysfile1.Backup.creditData.player[0].remain = (byte)r;
            sysfile2.Backup.creditData.player[0].remain = (byte)r;
        }

        private static int Sync(int v1, int v2, SyncType option) {
            switch (option) {
                case SyncType.File1:
                    return v1;
                case SyncType.File2:
                    return v2;
                case SyncType.Lower:
                    return Math.Min(v1, v2);
                case SyncType.Higher:
                    return Math.Max(v1, v2);
                case SyncType.Combine:
                    return v1 + v2;
                default:
                    throw new ArgumentException("unknown option: " + option);
            }
        }
    }
}