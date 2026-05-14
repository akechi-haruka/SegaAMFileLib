using System.Runtime.InteropServices;
using DiscUtils;
using DiscUtils.Ntfs;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Haruka.Common.Configuration;
using Microsoft.Extensions.Logging;

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

    private static void CheckSize(Type struc, int expected) {
        int calculated = Marshal.SizeOf(struc);
        Assert.That(calculated, Is.EqualTo(expected), "Size mismatch of struct " + struc);
    }

    [Test]
    public void T01_Structs() {
        CheckSize(typeof(BootId), 0x2800);
    }

    [Test]
    public void T02_TestParseKnownGoodNtfs() {
        byte[] data = File.ReadAllBytes(Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.ntfs"));
        NtfsFileSystem appFs = new NtfsFileSystem(new MemoryStream(data));
        DiscFileInfo innerVhd = appFs.Root.GetFiles().FirstOrDefault(f => f.Name == "internal_0.vhd");
        Assert.That(innerVhd, Is.Not.Null);
    }

    [Test]
    public void T03_TestBootId() {
        AppFile app = new AppFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app")));

        Assert.That(app.BootId.GetAppId(), Is.EqualTo("SDEM"));

        app.BootId.Verify();
    }

    [Test]
    public void T04_TestDecrypt() {
        byte[] goodData = File.ReadAllBytes(Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.ntfs"));
        AppFile app = new AppFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app")));
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
    public void T05_TestExtractApp() {
        AppFile app = new AppFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app")));
        Assert.That(app.BootId.GetAppId(), Is.EqualTo("SDEM"));

        app.ExtractTo(Path.Combine(TMP_FOLDER, "sdem101"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem101\\game.bat")), Is.True);
    }

    [Test]
    public void T06_TestExtractAppDifferential() {
        AppFile app0 = new AppFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_1.01.01_20190304110240_0.app")));
        AppFile app1 = new AppFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_1.02.00_20190617104949_1_1.01.01.app")), app0);

        app0.ExtractTo(Path.Combine(TMP_FOLDER, "sdem101"));
        app1.ExtractTo(Path.Combine(TMP_FOLDER, "sdem102"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem102\\game.bat")), Is.True);

        byte[] bat101 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem101\\game.bat"));
        byte[] bat102 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem102\\game.bat"));

        CollectionAssert.AreNotEqual(bat101, bat102);
    }

    [Test]
    public void T07_TestExtractOpt() {
        OptFile opt = new OptFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDDT_A002_20200930135740_0.opt")));
        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDDT"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(ContainerType.Option));

        opt.ExtractTo(Path.Combine(TMP_FOLDER, "sddt_opt"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sddt_opt\\DataConfig.xml")), Is.True);
    }

    [Test]
    public void T08_TestExtractAPMOpt() {
        OptFile opt = new OptFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_FH10_20200605065842_0.opt")));
        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDEM"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(ContainerType.Option));

        opt.ExtractTo(Path.Combine(TMP_FOLDER, "sdem_opt"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem_opt\\SDFH_FH10_20200605065842_0.opt")), Is.True);
    }

    [Test]
    public void T09_TestExtractAPMOptInner() {
        ApmOptFile opt = new ApmOptFile(File.OpenRead(Path.Combine(TEST_FOLDER, "tmp\\sdem_opt\\SDFH_FH10_20200605065842_0.opt")));
        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDFH"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(ContainerType.Option));

        opt.ExtractTo(Path.Combine(TMP_FOLDER, "sdem_opt_inner"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem_opt_inner\\game.bat")), Is.True);
    }

    [Test]
    public void T10_TestExtractAPMOptChainInner() {
        OptFile opt = new OptFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_FH11_20210208034234_0.opt")), new OptFile(File.OpenRead(Path.Combine(TEST_FOLDER, "SDEM_FH10_20200605065842_0.opt"))));
        Assert.That(opt.BootId.GetAppId(), Is.EqualTo("SDEM"));
        Assert.That(opt.BootId.containerType, Is.EqualTo(ContainerType.Option));

        opt.ExtractInnerApmTo(Path.Combine(TMP_FOLDER, "sdem_opt_chain"));

        Assert.That(File.Exists(Path.Combine(TMP_FOLDER, "sdem_opt_chain\\game.bat")), Is.True);

        byte[] file1 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem_opt_inner\\pen5.exe"));
        byte[] file2 = File.ReadAllBytes(Path.Combine(TMP_FOLDER, "sdem_opt_chain\\pen5.exe"));

        CollectionAssert.AreNotEqual(file1, file2);
    }
}