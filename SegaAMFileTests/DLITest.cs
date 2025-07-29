using Haruka.Arcade.SegaAMFileLib;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.DLI;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace SegaAMFileTests;

public class DLITest {
    [SetUp]
    public void Setup() {
        Logging.Initialize(Configuration.Initialize());
        Logging.Main.LogDebug(Environment.CurrentDirectory);
    }

    [Test]
    public void T01_TestReadApp() {
        DownloadInstructionFile dli = new DownloadInstructionFile("TestFiles/DLIApp.txt", DliType.App);
    }

    [Test]
    public void T02_TestReadOpt() {
        DownloadInstructionFile dli = new DownloadInstructionFile("TestFiles/DLIOpt.txt", DliType.Opt);
    }

    [Test]
    public void T03_TestWriteReadApp() {
        DownloadInstructionFile dli = new DownloadInstructionFile();
        dli.Common.DlFormat = 5.00F;
        dli.Common.GameId = "SXXX";
        dli.Common.ReleaseTime = DateTime.Now;
        dli.Common.OrderTime = DateTime.Now;
        dli.Common.PartSize = new uint[] { 1024, 2048, 4096 };
        dli.Common.IsdnDownloadInterval = new int[] { 1000, -1 };
        dli.Common.AdslDownloadInterval = new int[] { 1000, -1 };
        dli.Common.BroadbandDownloadInterval = new int[] { 1000, -1 };
        dli.Common.CloudDownloadAllowed = new DownloadInstructionFile.CloudDownload[48];
        Array.Fill(dli.Common.CloudDownloadAllowed, DownloadInstructionFile.CloudDownload.Allowed);
        dli.Common.ReportUrl = "http://dummy";
        dli.Common.ReportInterval = 3600;
        dli.Common.GameDescription = "DUMMY";
        //dli.Common.ReleaseType = 
        dli.Common.InstallUrls = new string[] { "http://dummy/SXXX_9.99.99_20200101010101_1_1.00.00.app" };
        dli.Common.ExistUrls = new string[] {
            "http://dummy/AAV_0001.00.00_20200101010101_0.pack",
            "http://dummy/SXXX_1.00.00_20200101010101_0_1.00.00.app"
        };
        dli.Common.PrivateInstallUrls = new string[] { };
        dli.Common.DownloadId = DownloadIdCalculator.GetDownloadId(dli);
        string output = dli.Write(DliType.App);
        DownloadInstructionFile dli2 = new DownloadInstructionFile(output.Split("\r\n"), DliType.App);

        Assert.That(dli.Common.DlFormat, Is.EqualTo(dli2.Common.DlFormat));
        Assert.That(dli.Common.DownloadId, Is.EqualTo(dli2.Common.DownloadId));
    }


    [Test]
    public void T04_TestWriteReadOpt() {
        DownloadInstructionFile dli = new DownloadInstructionFile();
        dli.Common.DlFormat = 5.00F;
        dli.Common.GameId = "SXXX";
        dli.Common.ReleaseTime = DateTime.Now;
        dli.Common.OrderTime = DateTime.Now;
        dli.Common.PartSize = new uint[] { 1024, 2048, 4096 };
        dli.Common.IsdnDownloadInterval = new int[] { 1000, -1 };
        dli.Common.AdslDownloadInterval = new int[] { 1000, -1 };
        dli.Common.BroadbandDownloadInterval = new int[] { 1000, -1 };
        dli.Common.CloudDownloadAllowed = new DownloadInstructionFile.CloudDownload[48];
        Array.Fill(dli.Common.CloudDownloadAllowed, DownloadInstructionFile.CloudDownload.Allowed);
        dli.Common.ReportUrl = "http://dummy";
        dli.Common.ReportInterval = 3600;
        dli.Common.GameDescription = "DUMMY";
        dli.Common.ReleaseType = OptReleaseType.Sequential;
        dli.Common.InstallUrls = new string[] { "http://dummy/SXXX_O001_20200101010101_0.opt" };
        dli.Common.ExistUrls = new string[] {
            "http://dummy/AAV_0001.00.00_20200101010101_0.pack",
            "http://dummy/SXXX_1.00.00_20200101010101_0_1.00.00.app"
        };
        dli.Common.PrivateInstallUrls = new string[] { };
        dli.Option = new DownloadInstructionFile.Optional {
            InstallUrls = dli.Common.InstallUrls,
            PrivateInstallUrls = dli.Common.PrivateInstallUrls
        };
        dli.Common.DownloadId = DownloadIdCalculator.GetDownloadId(dli);

        string output = dli.Write(DliType.Opt);
        DownloadInstructionFile dli2 = new DownloadInstructionFile(output.Split("\r\n"), DliType.Opt);

        Assert.That(dli.Common.DlFormat, Is.EqualTo(dli2.Common.DlFormat));
        Assert.That(dli.Common.DownloadId, Is.EqualTo(dli2.Common.DownloadId));
    }
}