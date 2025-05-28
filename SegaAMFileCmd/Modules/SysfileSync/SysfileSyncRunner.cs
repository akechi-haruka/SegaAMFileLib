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

            UpdateCredits(sysfile1, sysfile2, opts.SyncCredits);
            UpdateBookkeeping(sysfile1, sysfile2, opts.SyncBookkeeping);
            
            data1 = SysData.UpdateRecord(data1, sysfile1.Backup);
            data2 = SysData.UpdateRecord(data1, sysfile2.Backup);
            
            File.WriteAllBytes(opts.FileName1, data1);
            Program.Log.LogInformation("File 1 saved to: {f}", opts.FileName1);
            File.WriteAllBytes(opts.FileName2, data2);
            Program.Log.LogInformation("File 2 saved to: {f}", opts.FileName2);
            
            return 0;
        }

        private static unsafe void UpdateBookkeeping(SysData sysfile1, SysData sysfile2, SyncType option) {
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
        }

        private static void UpdateCredits(SysData sysfile1, SysData sysfile2, SyncType option) {
            int v = Sync(sysfile1.Backup.creditData.player[0].credit, sysfile2.Backup.creditData.player[0].credit, option);
            int r = Sync(sysfile1.Backup.creditData.player[0].remain, sysfile2.Backup.creditData.player[0].remain, option);
            
            Program.Log.LogInformation("Credits synchronized to {v}", v);
            Program.Log.LogInformation("Remain synchronized to {v}", r);

            sysfile1.Backup.creditData.player[0].credit = (byte)v;
            sysfile2.Backup.creditData.player[0].credit = (byte)r;
            sysfile1.Backup.creditData.player[0].remain = (byte)v;
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
                default:
                    throw new ArgumentException("unknown option: " + option);
            }
        }
    }
}
