using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using ICFReader;
using Microsoft.Extensions.Logging;

namespace SegaAMFileTests;

public class ICFTest {
    [SetUp]
    public void Setup() {
        Logging.Initialize(Configuration.Initialize());
    }

    private static void CheckSize(Type struc, int expected) {
        int calculated = Marshal.SizeOf(struc);
        Assert.That(calculated, Is.EqualTo(expected), "Size mismatch of struct " + struc);
    }

    [Test]
    public void T01_Structs() {
        CheckSize(typeof(ICFHeaderRecord), 0x40);
        CheckSize(typeof(ICFEntryRecord), 0x40);
    }

    private void ValidateICF(InstallationConfigurationFile icf) {
        Assert.That(icf.GetRecordCount(), Is.LessThan(9), "Too many ICF records");
        ICFEntryRecord? sr = icf.GetSystemRecord();
        Assert.That(sr, Is.Not.Null, "No system record");
        Assert.That(sr.Value.requiredVersion, Is.EqualTo(sr.Value.version), "ICF system version unequal required version");
        Assert.That(icf.GetAppRecord(), Is.Not.Null, "No app record");
    }

    [Test]
    public void T02_Read_1() {
        byte[] rawFile = File.ReadAllBytes("TestFiles\\ICF1");
        byte[] key = File.ReadAllBytes("TestFiles\\icf_key.bin");
        byte[] iv = File.ReadAllBytes("TestFiles\\icf_key.iv");
        InstallationConfigurationFile icf = new InstallationConfigurationFile(rawFile, key, iv);
        ValidateICF(icf);
    }

    [Test]
    public void T03_Read_2() {
        byte[] rawFile = File.ReadAllBytes("TestFiles\\ICF2");
        byte[] key = File.ReadAllBytes("TestFiles\\icf_key.bin");
        byte[] iv = File.ReadAllBytes("TestFiles\\icf_key.iv");
        InstallationConfigurationFile icf = new InstallationConfigurationFile(rawFile, key, iv);
        ValidateICF(icf);
    }

    [Test]
    public void T04_ReadWriteCheck() {
        // TODO
    }
    
}