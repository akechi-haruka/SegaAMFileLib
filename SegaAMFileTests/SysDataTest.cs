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
        foreach (BackupRecordDefinition record in SysData.Records) {
            int calculated = Marshal.SizeOf(record.Structure);
            uint expected = record.Size;
            Assert.That(calculated, Is.EqualTo(expected), "Size mismatch of struct " + record.Structure);
        }
    }

    [Test]
    public void T02_Read() {
        SysData data = new SysData(File.ReadAllBytes("G:\\SDVX\\fatego10\\amfs\\sysfile.dat"));
    }
    
}