using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.FSExtract {
    class FSExtractRunner {
        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!File.Exists(opts.FileName)) {
                Program.CmdLog.LogError("Specified file not found: {f}", opts.FileName);
                return 1;
            }

            EncryptionEnvironment.Initialize(opts.KeyFile);

            Program.CmdLog.LogInformation("Parsing filename: " + opts.FileName);
            InstallFileName fileName = InstallFileName.Parse(opts.FileName);

            Program.CmdLog.LogInformation("Reading " + opts.FileName + "...");
            Stream input = File.OpenRead(opts.FileName);
            FscryptFile container;
            if (fileName.Type == InstallFileName.FileType.App || fileName.Type == InstallFileName.FileType.Pack) {
                container = new AppFile(input, null, !opts.NoVerify); // TODO: parents
            } else if (fileName.Type == InstallFileName.FileType.Option) {
                if (fileName.IsApm()) {
                    container = new ApmOptFile(input);
                } else {
                    container = new OptFile(input, null, !opts.NoVerify);
                }
            } else {
                throw new IOException("Unknown container: " + fileName.Type);
            }

            if (!opts.NoExtract) {
                if (opts.OutputDirectory == null) {
                    Program.CmdLog.LogError("An output directory must be specified.");
                    return 1;
                }

                if (!Directory.Exists(opts.OutputDirectory)) {
                    Directory.CreateDirectory(opts.OutputDirectory);
                }

                container.ExtractTo(opts.OutputDirectory);
            } else {
                Program.CmdLog.LogInformation("--no-extract was specified, doing nothing");
            }

            return 0;
        }
    }
}