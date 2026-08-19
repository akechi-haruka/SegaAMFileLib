using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.BootIdRead {
    class BootIdRunner {
        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!File.Exists(opts.FileName)) {
                Program.CmdLog.LogError("Specified file not found: {f}", opts.FileName);
                return 1;
            }

            EncryptionEnvironment.Initialize(opts.KeyFile);

            byte[] data = new byte[BootId.SIZE];
            using (FileStream fs = File.OpenRead(opts.FileName)) {
                fs.ReadExactly(data);
            }

            BootId bootId = BootId.FromEncryptedBytes(data);

            Program.CmdLog.LogInformation("BootId:");
            Program.CmdLog.LogInformation(" - CRC: 0x" + bootId.crc.ToString("X"));
            Program.CmdLog.LogInformation(" - Length: 0x" + bootId.length.ToString("X"));
            Program.CmdLog.LogInformation(" - Signature: " + bootId.GetSignature());
            Program.CmdLog.LogInformation(" - Unknown: " + bootId.unknown);
            Program.CmdLog.LogInformation(" - Container Type: " + bootId.containerType);
            Program.CmdLog.LogInformation(" - Sequence: " + bootId.sequenceNumber);
            Program.CmdLog.LogInformation(" - Custom IV?: " + bootId.useCustomIV);
            Program.CmdLog.LogInformation(" - Game ID: " + bootId.GetAppId());
            Program.CmdLog.LogInformation(" - Timestamp: " + bootId.gameTimestamp);
            Program.CmdLog.LogInformation(" - Version: " + bootId.gameVersion);
            Program.CmdLog.LogInformation(" - Block Count: " + bootId.blockCount);
            Program.CmdLog.LogInformation(" - Block Size: " + bootId.blockSize);
            Program.CmdLog.LogInformation(" - Header Block Count: " + bootId.headerBlockCount);
            Program.CmdLog.LogInformation(" - Unknown2: " + bootId.unknown2);
            Program.CmdLog.LogInformation(" - Platform ID: " + bootId.GetPlatformId());
            Program.CmdLog.LogInformation(" - Platform Generation: " + bootId.platformGeneration);
            Program.CmdLog.LogInformation(" - Source Timestamp: " + bootId.sourceTimestamp);
            Program.CmdLog.LogInformation(" - Source Version: " + bootId.sourceVersion);
            Program.CmdLog.LogInformation(" - Platform Version: " + bootId.platformVersion);
            if (opts.WithStrings) {
                Program.CmdLog.LogInformation(bootId.DumpStrings());
            }

            return 0;
        }
    }
}