using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace SegaAMFileTests;

public class SysDataTest {
    [SetUp]
    public void Setup() {
        Logging.Initialize(Configuration.Initialize());
    }

    [Test]
    public void T01_Structs() {
        Logging.Main.LogDebug(Marshal.SizeOf(typeof(ErrorBody)).ToString("X2"));
        foreach (BackupRecordDefinition record in SysData.RECORDS) {
            int calculated = Marshal.SizeOf(record.Structure);
            uint expected = record.Size;
            Assert.That(calculated, Is.EqualTo(expected), "Size mismatch of struct " + record.Structure);
        }
    }

    [Test]
    public void T02_Read() {
        new SysData(File.ReadAllBytes("TestFiles\\sysfile.dat"));
    }

    [Test]
    public void T03_ReadWriteCheck() {
        byte[] file = File.ReadAllBytes("TestFiles\\sysfile.dat");
        SysData data = new SysData(file);
        data.Backup.creditData.player[0].credit = 12;
        data.Emoney.availableBrandList = 5;
        file = SysData.UpdateRecord(file, data.Backup);
        file = SysData.UpdateRecord(file, data.Emoney);
        File.WriteAllBytes("TestFiles\\sysfile2.dat", file);
        data = new SysData(File.ReadAllBytes("TestFiles\\sysfile2.dat"));
        Assert.That(data.Backup.creditData.player[0].credit, Is.EqualTo(12));
        Assert.That(data.Emoney.availableBrandList, Is.EqualTo(5));
    }
    
}