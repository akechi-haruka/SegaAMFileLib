using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;
using Version = Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.Version;

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
        Assert.That(icf.GetRecordCount(), Is.LessThan(99), "Too many ICF records");
        ICFEntryRecord? sr = icf.GetSystemRecord();
        Assert.That(sr, Is.Not.Null, "No system record");
        Assert.That(sr.Value.requiredVersion, Is.EqualTo(sr.Value.version), "ICF system version unequal required version");
        Assert.That(icf.GetAppRecord(), Is.Not.Null, "No app record");
    }

    [Test]
    public void T02_Read_1() {
        byte[] rawFile = File.ReadAllBytes("TestFiles\\ICF1");
        byte[] key = File.ReadAllBytes("TestFiles\\icf_key.bin");
        byte[] iv = File.ReadAllBytes("TestFiles\\icf_iv.bin");
        InstallationConfigurationFile icf = new InstallationConfigurationFile(rawFile, key, iv);
        ValidateICF(icf);
    }

    [Test]
    public void T03_Read_2() {
        byte[] rawFile = File.ReadAllBytes("TestFiles\\ICF2");
        byte[] key = File.ReadAllBytes("TestFiles\\icf_key.bin");
        byte[] iv = File.ReadAllBytes("TestFiles\\icf_iv.bin");
        InstallationConfigurationFile icf = new InstallationConfigurationFile(rawFile, key, iv);
        ValidateICF(icf);
    }

    [Test]
    public void T04_WriteReadCheck() {

        const String gameId = "XXXX";
        const String platformId = "AAV1";
        Version ver = new Version() {
            major = 1,
            minor = 30,
            build = 14
        };
        
        byte[] key = File.ReadAllBytes("TestFiles\\icf_key.bin");
        byte[] iv = File.ReadAllBytes("TestFiles\\icf_iv.bin");

        InstallationConfigurationFile icf = new InstallationConfigurationFile();

        icf.Header.SetAppId(gameId);
        icf.Header.SetPlatformId(platformId.Substring(0, 3));
        icf.Header.platformGeneration = Convert.ToByte(platformId.Substring(3));
        
        Timestamp time = Timestamp.Now();

        ICFEntryRecord systemEntry = new ICFEntryRecord {
            typeFlags = ICFType.System,
            entryFlags = EntryFlags.Enabled1 | EntryFlags.Enabled2,
            timestamp = time,
            requiredVersion = ver,
            version = ver
        };
        icf.AddRecord(systemEntry);
        ICFEntryRecord appEntry = new ICFEntryRecord {
            typeFlags = ICFType.App,
            entryFlags = EntryFlags.Enabled1 | EntryFlags.Enabled2,
            timestamp = time,
            requiredVersion = ver,
            version = ver
        };
        icf.AddRecord(appEntry);
        
        Assert.That(icf.Header.entryCount, Is.EqualTo(2));
        Assert.That(icf.Header.dataSize, Is.EqualTo(3 * 0x40));

        byte[] data = icf.Save();
        
        data = SegaAes.Encrypt(data, key, iv);

        InstallationConfigurationFile icf2 = new InstallationConfigurationFile(data, key, iv);
        
        Assert.That(icf2.GetRecordCount(), Is.EqualTo(icf.GetRecordCount()));
        Assert.That(icf2.GetSystemRecord(), Is.EqualTo(icf.GetSystemRecord()));
        Assert.That(icf2.GetAppRecord(), Is.EqualTo(icf.GetAppRecord()));
    }
    
}