using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileCmd.Modules.FSExtract {
    class FSExtractRunner {
        private static List<string> innerContainers = new List<string>();

        internal static int Run(Options opts) {
            Program.SetGlobalOptions(opts);

            if (!File.Exists(opts.FileName)) {
                Program.CmdLog.LogError("Specified file not found: {f}", opts.FileName);
                return 1;
            }

            EncryptionEnvironment.Initialize(opts.KeyFile);

            Program.CmdLog.LogInformation("Parsing filename: " + opts.FileName);
            InstallFileName fileName = InstallFileName.Parse(opts.FileName);
            bool isApm = fileName.IsApm();

            Program.CmdLog.LogInformation("Reading " + opts.FileName + "...");
            Stream input = File.OpenRead(opts.FileName);
            FscryptFile parent = BuildParentChain(opts.Parents, false, opts.NoVerify);
            FscryptFile container = DetectContainerType(input, opts.NoVerify, fileName, false, parent);

            if (!opts.NoExtract) {
                if (opts.OutputDirectory == null) {
                    Program.CmdLog.LogError("An output directory must be specified.");
                    return 1;
                }

                if (!Directory.Exists(opts.OutputDirectory)) {
                    Directory.CreateDirectory(opts.OutputDirectory);
                }

                container.ExtractTo(opts.OutputDirectory, Callback, opts.SkipExisting);

                if (innerContainers.Count > 0) {
                    Program.CmdLog.LogInformation("Detected " + innerContainers.Count + " inner containers");

                    if (!opts.NoExtractInner) {
                        foreach (String file in innerContainers) {
                            string extractedInnerFile = Path.Combine(opts.OutputDirectory, file);
                            Program.CmdLog.LogInformation("Found inner container: " + extractedInnerFile);

                            InstallFileName fileNameInner = InstallFileName.Parse(extractedInnerFile, isApm);

                            Program.CmdLog.LogInformation("Reading " + extractedInnerFile + "...");
                            Stream inputInner = File.OpenRead(extractedInnerFile);
                            FscryptFile parentInner = BuildParentChain(opts.ParentsInner, isApm, opts.NoVerify);
                            FscryptFile containerInner = DetectContainerType(inputInner, opts.NoVerify, fileNameInner, isApm, parentInner);

                            String outputInner = extractedInnerFile.Substring(0, extractedInnerFile.Length - 4);

                            if (!Directory.Exists(outputInner)) {
                                Directory.CreateDirectory(outputInner);
                            }

                            containerInner.ExtractTo(outputInner, null, opts.SkipExisting);
                        }
                    }
                } else {
                    Program.CmdLog.LogInformation("--no-extract-inner was specified, doing nothing");
                }
            } else {
                Program.CmdLog.LogInformation("--no-extract was specified, doing nothing");
            }

            return 0;
        }

        private static FscryptFile BuildParentChain(IEnumerable<string> parents, bool isApm, bool noVerify) {
            FscryptFile parentContainer = null;
            Program.CmdLog.LogDebug(parents.Count() + " parent containers specified");
            foreach (string parent in parents) {
                if (!File.Exists(parent)) {
                    throw new IOException("Parent file not found: " + parent);
                }

                Program.CmdLog.LogInformation("Reading parent container: " + parent);

                InstallFileName fileNameParent = InstallFileName.Parse(parent, isApm);
                Stream inputInner = File.OpenRead(parent);
                if (fileNameParent.Type == InstallFileName.FileType.Option) {
                    parentContainer = new ApmOptFile(inputInner, (ApmOptFile)parentContainer);
                } else {
                    parentContainer = new AppFile(inputInner, (AppFile)parentContainer, !noVerify);
                }
            }

            return parentContainer;
        }

        private static FscryptFile DetectContainerType(Stream input, bool noVerify, InstallFileName fileName, bool isApm, FscryptFile parentChain) {
            FscryptFile container;

            if (fileName.Type == InstallFileName.FileType.App || fileName.Type == InstallFileName.FileType.Pack) {
                container = new AppFile(input, (AppFile)parentChain, !noVerify);
            } else if (fileName.Type == InstallFileName.FileType.Option) {
                if (isApm) {
                    Program.CmdLog.LogInformation("Detected APM .opt file");
                    container = new ApmOptFile(input, (ApmOptFile)parentChain);
                } else {
                    Program.CmdLog.LogInformation("Detected regular .opt file");
                    container = new OptFile(input, (OptFile)parentChain, !noVerify);
                }
            } else {
                throw new IOException("Unknown container: " + fileName.Type);
            }

            return container;
        }

        private static void Callback(string file, int num, int total, long currentSize, long processedSize, long totalSize) {
            if (file.EndsWith(".opt") || file.EndsWith(".app")) {
                innerContainers.Add(file);
            }
        }
    }
}