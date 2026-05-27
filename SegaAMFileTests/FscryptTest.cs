using System.IO.Compression;
using System.Runtime.InteropServices;
using DiscUtils;
using DiscUtils.ExFat;
using DiscUtils.ExFat.Internal;
using DiscUtils.ExFat.Internal.Filesystem;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DiscUtils.Streams;
using DiscUtils.Vhd;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Haruka.Common.Collections;
using Haruka.Common.Configuration;
using Microsoft.Extensions.Logging;
using Version = System.Version;

namespace SegaAMFileTests;

public class FscryptTest {
    private static readonly String TEST_FOLDER = "TestFiles";
    private static readonly String TMP_FOLDER = Path.Combine(TEST_FOLDER, "tmp");

    [OneTimeSetUp]
    public void Init() {
        if (Directory.Exists(TMP_FOLDER)) {
            Directory.Delete(TMP_FOLDER, true);
        }

        Directory.CreateDirectory(TMP_FOLDER);
    }

    [SetUp]
    public void Setup() {
        AppConfig.Initialize();
        Log.Initialize();
        Log.Main.LogDebug(Environment.CurrentDirectory);
        EncryptionEnvironment.Initialize("TestFiles\\keys.txt");
    }

    [TearDown]
    public void End() {
        Log.FlushAndDispose();
    }

    private void CheckPath(string path) {
        if (!File.Exists(path)) {
            Assert.Inconclusive("Test file does not exist: " + path);
        }
    }

    private static void CheckSize(Type struc, int expected) {
        int calculated = Marshal.SizeOf(struc);
        Assert.That(calculated, Is.EqualTo(expected), "Size mismatch of struct " + struc);
    }

    [Test]
    public void T01_Structs() {
        CheckSize(typeof(BootId), BootId.SIZE);
    }

    [Test]
    public void T02_ParseKnownGoodNtfs() {
        string path = Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.ntfs");
        CheckPath(path);

        byte[] data = File.ReadAllBytes(path);
        NtfsFileSystem appFs = new NtfsFileSystem(new MemoryStream(data));
        DiscFileInfo innerVhd = appFs.Root.GetFiles().FirstOrDefault(f => f.Name == "internal_0.vhd");
        Assert.That(innerVhd, Is.Not.Null);
    }

    [Test]
    public void T03_TestBootId() {
        string path = Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app");
        CheckPath(path);

        AppFile app = new AppFile(File.OpenRead(path));

        Assert.That(app.BootId.GetAppId(), Is.EqualTo("SDEM"));

        app.BootId.Verify();

        Log.Main.LogInformation(app.BootId.DumpStrings());
    }

    [Test]
    public void T04_Decrypt() {
        string goodPath = Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.ntfs");
        CheckPath(goodPath);
        string appPath = Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app");
        CheckPath(goodPath);

        byte[] goodData = File.ReadAllBytes(goodPath);
        AppFile app = new AppFile(File.OpenRead(appPath));
        Log.Main.LogInformation("Reading file...");
        byte[] checkData = app.ReadAndDecryptWholeFile();

        Log.Main.LogInformation("--- EXPECTED ---");
        Log.Main.LogInformation(FsUtils.DumpNtfsFileSystemProperties(goodData));
        Log.Main.LogInformation("--- GOT ---");
        Log.Main.LogInformation(FsUtils.DumpNtfsFileSystemProperties(checkData));

        Log.Main.LogInformation("Initial 64 bytes of original filesystem:\n" + Hex.Dump(goodData, 64));
        Log.Main.LogInformation("Initial 64 bytes of decrypted filesystem:\n" + Hex.Dump(checkData, 64));

        // limit to the first 64kb so this doesn't take forever, will have several parts included regardless
        byte[] goodDataPart = new byte[ushort.MaxValue];
        byte[] checkDataPart = new byte[ushort.MaxValue];
        Array.Copy(goodData, goodDataPart, goodDataPart.Length);
        Array.Copy(checkData, checkDataPart, checkDataPart.Length);

        CollectionAssert.AreEqual(goodDataPart, checkDataPart);
    }

    [Test]
    public void T05_ExtractApp() {
        string path = Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app");
        CheckPath(path);

        AppFile app = new AppFile(File.OpenRead(path));
        Assert.That(app.BootId.GetAppId(), Is.EqualTo("SDEM"));
        Assert.That(app.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(path).Length));

        app.ExtractTo(Path.Combine(TMP_FOLDER, "sdem101"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem101\\game.bat")), Is.True);
    }

    [Test]
    public void T06_ExtractAppDifferential() {
        string path0 = Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app");
        CheckPath(path0);
        string path1 = Path.Combine(TEST_FOLDER, "SDEM_1.02.00_20190617104949_1_1.01.01.app");
        CheckPath(path1);

        AppFile app0 = new AppFile(File.OpenRead(path0));
        AppFile app1 = new AppFile(File.OpenRead(path1), app0);

        Log.Main.LogInformation("App0: " + app0.BootId);
        Log.Main.LogInformation("App0 signature: " + app0.BootId.GetSignature());
        Log.Main.LogInformation("App0 source: " + app0.BootId.sourceVersion + " - " + app0.BootId.sourceTimestamp);
        Log.Main.LogInformation("App0 sizes: count=" + app0.BootId.blockCount + ", headerCount=" + app0.BootId.headerBlockCount + ", blockSize=" + app0.BootId.blockSize);
        Log.Main.LogInformation("App1: " + app1.BootId);
        Log.Main.LogInformation("App1 signature: " + app1.BootId.GetSignature());
        Log.Main.LogInformation("App1 source: " + app1.BootId.sourceVersion + " - " + app1.BootId.sourceTimestamp);
        Log.Main.LogInformation("App1 sizes: count=" + app1.BootId.blockCount + ", headerCount=" + app1.BootId.headerBlockCount + ", blockSize=" + app1.BootId.blockSize);

        app0.ExtractTo(Path.Combine(TMP_FOLDER, "sdem101"));
        app1.ExtractTo(Path.Combine(TMP_FOLDER, "sdem102"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem102\\game.bat")), Is.True);

        byte[] bat101 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem101\\game.bat"));
        byte[] bat102 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem102\\game.bat"));

        CollectionAssert.AreNotEqual(bat101, bat102);
    }

    [Test]
    public void T07_ExtractOpt() {
        string path = Path.Combine(TEST_FOLDER, "SDDT_A002_20200930135740_0.opt");
        CheckPath(path);

        OptFile opt = new OptFile(File.OpenRead(path));
        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDDT"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(InstallFile.FileType.Option));
        Assert.That(opt.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(path).Length));

        opt.ExtractTo(Path.Combine(TMP_FOLDER, "sddt_opt"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sddt_opt\\DataConfig.xml")), Is.True);
    }

    [Test]
    public void T08_ExtractAPMOpt() {
        string path = Path.Combine(TEST_FOLDER, "SDEM_FH10_20200605065842_0.opt");
        CheckPath(path);

        OptFile opt = new OptFile(File.OpenRead(path));
        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDEM"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(InstallFile.FileType.Option));
        Assert.That(opt.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(path).Length));

        opt.ExtractTo(Path.Combine(TMP_FOLDER, "sdem_opt"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem_opt\\SDFH_FH10_20200605065842_0.opt")), Is.True);
    }

    [Test]
    public void T09_ExtractAPMOptInner() {
        string path = Path.Combine(TMP_FOLDER, "sdem_opt\\SDFH_FH10_20200605065842_0.opt");
        CheckPath(path);

        ApmOptFile opt = new ApmOptFile(File.OpenRead(path));
        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDFH"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(InstallFile.FileType.Option));
        Assert.That(opt.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(path).Length));

        opt.ExtractTo(Path.Combine(TMP_FOLDER, "sdem_opt_inner"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem_opt_inner\\game.bat")), Is.True);
    }

    [Test]
    public void T10_ExtractAPMOptChainInner() {
        string path1 = Path.Combine(TEST_FOLDER, "SDEM_FH11_20210208034234_0.opt");
        CheckPath(path1);
        string path0 = Path.Combine(TEST_FOLDER, "SDEM_FH10_20200605065842_0.opt");
        CheckPath(path0);

        OptFile opt = new OptFile(File.OpenRead(path1), new OptFile(File.OpenRead(path0)));

        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDEM"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(InstallFile.FileType.Option));
        Assert.That(opt.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(path1).Length));

        opt.ExtractInnerApmTo(Path.Combine(TMP_FOLDER, "sdem_opt_chain"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem_opt_chain\\game.bat")), Is.True);

        byte[] file1 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem_opt_inner\\pen5.exe"));
        byte[] file2 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem_opt_chain\\pen5.exe"));

        CollectionAssert.AreNotEqual(file1, file2);
    }

    [Test]
    public void T11_Encrypt() {
        byte[] test = new byte[256 * 256];
        for (int i = 0; i < test.Length; i++) {
            test[i] = (byte)(i % 256);
        }

        Log.Main.LogInformation("Initial page 0, first 256 bytes:\n" + Hex.Dump(test, 256));
        Log.Main.LogInformation("Initial page 1, first 256 bytes:\n" + Hex.Dump(test, 256, 1 * FscryptStream.PAGE_SIZE));
        Log.Main.LogInformation("Initial page 2, first 256 bytes:\n" + Hex.Dump(test, 256, 2 * FscryptStream.PAGE_SIZE));

        byte[] key = Util.RandomBytes(16);
        byte[] iv = Util.RandomBytes(16);

        Log.Main.LogInformation("Random key is " + Hex.To(key));
        Log.Main.LogInformation("Random IV is " + Hex.To(iv));

        byte[] encrypted = new byte[test.Length];
        MemoryStream resultBuffer = new MemoryStream(encrypted, true);
        FscryptStream encrypt = new FscryptStream(resultBuffer, encrypted.Length, key, iv);
        encrypt.Write(test);
        encrypt.Flush();
        encrypt.Close();

        Log.Main.LogInformation("Encrypted page 0, first 256 bytes:\n" + Hex.Dump(encrypted, 256));
        Log.Main.LogInformation("Encrypted page 1, first 256 bytes:\n" + Hex.Dump(encrypted, 256, 1 * FscryptStream.PAGE_SIZE));
        Log.Main.LogInformation("Encrypted page 2, first 512 bytes:\n" + Hex.Dump(encrypted, 256, 2 * FscryptStream.PAGE_SIZE));

        byte[] decrypted = new byte[test.Length];
        resultBuffer = new MemoryStream(encrypted, true);
        FscryptStream decrypt = new FscryptStream(resultBuffer, decrypted.Length, key, iv);
        decrypt.ReadExactly(decrypted);

        Log.Main.LogInformation("Decrypted page 0, first 256 bytes:\n" + Hex.Dump(decrypted, 256));
        Log.Main.LogInformation("Decrypted page 1, first 256 bytes:\n" + Hex.Dump(decrypted, 256, FscryptStream.PAGE_SIZE));
        Log.Main.LogInformation("Decrypted page 2, first 256 bytes:\n" + Hex.Dump(decrypted, 256, 2 * FscryptStream.PAGE_SIZE));

        CollectionAssert.AreEqual(test, decrypted);
    }

    [Test]
    public void T12_CreateNtfsPartition() {
        const long outerFsSize = 8650752;
        const long payloadLength = outerFsSize + 512 + 512; // MBR + NTFS header
        const long dummyFileSize = 4404019;

        const int bytesPerSector = 4096;

        byte[] outerFsBytes = new byte[payloadLength];

        using (Stream outerFsStream = new MemoryStream(outerFsBytes, true)) {
            Geometry geometry = new Geometry(outerFsSize, 1, 17, bytesPerSector);
            using (DiscFileSystem outerFs = NtfsFileSystem.Format(outerFsStream, "test_outer", geometry, 0, outerFsSize / geometry.BytesPerSector, new NtfsFormatOptions())) {
                using (SparseStream internalFile = outerFs.OpenFile("test.bin", FileMode.CreateNew)) {
                    internalFile.Write(new byte[dummyFileSize]);
                }
            }
        }

        using (Stream outerFsStream = new MemoryStream(outerFsBytes, true)) {
            Log.Main.LogInformation(FsUtils.DumpNtfsFileSystemProperties(outerFsStream));
            outerFsStream.Seek(0, SeekOrigin.Begin);

            using (NtfsFileSystem outerFs = new NtfsFileSystem(outerFsStream)) {
                Assert.That(outerFs.SectorSize, Is.EqualTo(bytesPerSector));
                Assert.That(outerFs.GetFileInfo("test.bin").Length, Is.EqualTo(dummyFileSize));
            }
        }
    }


    [Test]
    public void T13_CreateVhd() {
        const int innerFsSize = 4 * 1024 * 1024;
        const int dummyFileSize = 1 * 1024 * 1024;

        byte[] innerFsBytes = new byte[innerFsSize + 512];

        using (MemoryStream innerFsStream = new MemoryStream(innerFsBytes, true)) {
            using (Disk innerDisk = Disk.InitializeFixed(innerFsStream, Ownership.None, innerFsSize)) {
                BiosPartitionTable.Initialize(innerDisk, WellKnownPartitionType.WindowsNtfs);
                VolumeManager vm = new VolumeManager(innerDisk);

                using (NtfsFileSystem innerFs = NtfsFileSystem.Format(vm.GetLogicalVolumes()[0], "test", new NtfsFormatOptions())) {
                    using (SparseStream internalFile = innerFs.OpenFile("test.bin", FileMode.CreateNew)) {
                        byte[] dummyData = new byte[dummyFileSize];
                        dummyData.Fill<byte>(0xAB);
                        internalFile.Write(dummyData);
                    }
                }
            }
        }

        using (MemoryStream innerFsStream = new MemoryStream(innerFsBytes, true)) {
            DiskImageFile vhd = new DiskImageFile(innerFsStream);
            Disk virtualDisk = new Disk(new List<DiskImageFile>() { vhd }, Ownership.None);
            SparseStream fsStream = virtualDisk.Partitions.Partitions[0].Open();
            NtfsFileSystem ntfs = new NtfsFileSystem(fsStream);

            Assert.That(ntfs.GetFileInfo("test.bin").Length, Is.EqualTo(dummyFileSize));
        }
    }

    [Test]
    public void T14_CreateApp() {
        const String appID = "TEST";
        EncryptionEnvironment.SetEncryptionParametersForGame(appID, new byte[16], new byte[16]);
        string inputDir = CreateTestFileStructure();
        InstallFile fileInfo = InstallFile.CreateApp(appID, new Version(1, 0, 0), 0);

        FscryptContainerGenerator.Create(inputDir, TMP_FOLDER, fileInfo);

        string targetFile = Path.Combine(TMP_FOLDER, fileInfo.GetFileName());
        FileAssert.Exists(targetFile);

        AppFile bootlegFile = new AppFile(File.OpenRead(targetFile));
        Assert.That(bootlegFile.BootId.GetAppId(), Is.EqualTo(appID));
        Assert.That(bootlegFile.BootId.containerType, Is.EqualTo(InstallFile.FileType.App));
        Assert.That(bootlegFile.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(targetFile).Length));

        string checkDir = Path.Combine(TMP_FOLDER, "fscrypt_test_structure_check_app");
        bootlegFile.ExtractTo(checkDir);
        DirectoryAssert.Exists(checkDir);

        Util.AssertTwoDirectoriesContentEqual(inputDir, checkDir);
    }

    [Test]
    public void T15_CreateExfatPartition() {
        const int innerFsSize = 4 * 1024 * 1024;
        const int dummyFileSize = 1 * 1024 * 1024;

        byte[] innerFsBytes = new byte[innerFsSize];

        using (MemoryStream innerFsStream = new MemoryStream(innerFsBytes, true)) {
            ExFatPathFilesystem.Format(innerFsStream, new ExFatFormatOptions(), "test").Dispose();
            using (ExFatFileSystem exfat = new ExFatFileSystem(innerFsStream)) {
                using (SparseStream internalFile = exfat.OpenFile("test.bin", FileMode.CreateNew)) {
                    byte[] dummyData = new byte[dummyFileSize];
                    dummyData.Fill<byte>(0xAB);
                    internalFile.Write(dummyData);
                }
            }
        }

        using (MemoryStream innerFsStream = new MemoryStream(innerFsBytes, true)) {
            Log.Main.LogInformation(FsUtils.DumpNtfsFileSystemProperties(innerFsStream));
            innerFsStream.Seek(0, SeekOrigin.Begin);
            using (ExFatFileSystem exfat = new ExFatFileSystem(innerFsStream)) {
                Assert.That(exfat.GetFileInfo("test.bin").Length, Is.EqualTo(dummyFileSize));
            }
        }
    }

    [Test]
    public void T16_CreateOpt() {
        const String appID = "TOPT";
        EncryptionEnvironment.SetEncryptionParametersForGame(appID, new byte[16], new byte[16]);
        string inputDir = CreateTestFileStructure();
        InstallFile fileInfo = InstallFile.CreateOption(appID, "T001", new Version(1, 0, 0), 0);

        FscryptContainerGenerator.Create(inputDir, TMP_FOLDER, fileInfo);

        string targetFile = Path.Combine(TMP_FOLDER, fileInfo.GetFileName());
        FileAssert.Exists(targetFile);

        OptFile bootlegFile = new OptFile(File.OpenRead(targetFile));
        Assert.That(bootlegFile.BootId.GetAppId(), Is.EqualTo(appID));
        Assert.That(bootlegFile.BootId.containerType, Is.EqualTo(InstallFile.FileType.Option));
        Assert.That(bootlegFile.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(targetFile).Length));

        string checkDir = Path.Combine(TMP_FOLDER, "fscrypt_test_structure_check_opt");
        bootlegFile.ExtractTo(checkDir);
        DirectoryAssert.Exists(checkDir);

        Util.AssertTwoDirectoriesContentEqual(inputDir, checkDir);
    }

    [Test]
    public void T17_CreateApmOpt() {
        const String appID = GameID.APM_APP_ID;
        EncryptionEnvironment.SetEncryptionParametersForGame(appID, new byte[16], new byte[16]);
        string inputDir = CreateTestFileStructure();
        InstallFile fileInfo = InstallFile.CreateOption(appID, "TE10", new Version(1, 0, 0), 0);

        FscryptContainerGenerator.Create(inputDir, TMP_FOLDER, fileInfo);

        string targetFile = Path.Combine(TMP_FOLDER, fileInfo.GetFileName());
        FileAssert.Exists(targetFile);

        ApmOptFile bootlegFile = new ApmOptFile(File.OpenRead(targetFile));
        Assert.That(bootlegFile.BootId.GetAppId(), Is.EqualTo(appID));
        Assert.That(bootlegFile.BootId.containerType, Is.EqualTo(InstallFile.FileType.Option));
        Assert.That(bootlegFile.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(targetFile).Length));

        string checkDir = Path.Combine(TMP_FOLDER, "fscrypt_test_structure_check_apmopt");
        bootlegFile.ExtractTo(checkDir);
        DirectoryAssert.Exists(checkDir);

        Util.AssertTwoDirectoriesContentEqual(inputDir, checkDir);
    }

    [Test]
    public void T18_CreateLargeApp() {
        const String appID = "TESL";
        EncryptionEnvironment.SetEncryptionParametersForGame(appID, new byte[16], new byte[16]);

        string inputDir = Path.Combine(TEST_FOLDER, "large_tmp");
        if (!Directory.Exists(inputDir)) {
            Directory.CreateDirectory(inputDir);
        }

        string largeFile = Path.Combine(inputDir, "large.bin");
        if (!File.Exists(largeFile)) {
            byte[] oneMegabyte = new byte[1024 * 1024];
            oneMegabyte.Fill<byte>(0x77);
            using (FileStream fs = File.OpenWrite(largeFile)) {
                for (int i = 0; i < 1024; i++) {
                    fs.Write(oneMegabyte);
                }
            }
        }

        InstallFile fileInfo = InstallFile.CreateApp(appID, new Version(1, 0, 0), 0);

        FscryptContainerGenerator.Create(inputDir, TMP_FOLDER, fileInfo);

        string targetFile = Path.Combine(TMP_FOLDER, fileInfo.GetFileName());
        FileAssert.Exists(targetFile);

        AppFile bootlegFile = new AppFile(File.OpenRead(targetFile));
        Assert.That(bootlegFile.BootId.GetAppId(), Is.EqualTo(appID));
        Assert.That(bootlegFile.BootId.containerType, Is.EqualTo(InstallFile.FileType.App));
        Assert.That(bootlegFile.BootId.GetFullContainerSize(), Is.EqualTo(new FileInfo(targetFile).Length));

        string checkDir = Path.Combine(TMP_FOLDER, "fscrypt_test_structure_check_app_large");
        bootlegFile.ExtractTo(checkDir);
        DirectoryAssert.Exists(checkDir);

        Util.AssertTwoDirectoriesContentEqual(inputDir, checkDir);
    }

    private string CreateTestFileStructure() {
        string inputDir = Path.Combine(TEST_FOLDER, "fscrypt_test_structure");
        ZipFile.ExtractToDirectory(inputDir + ".zip", Directory.GetParent(inputDir).FullName, null, true);
        DirectoryAssert.Exists(inputDir);

        File.WriteAllBytes(Path.Combine(inputDir, "random.bin"), Util.RandomBytes(1 * 1024 * 1024));

        return inputDir;
    }
}