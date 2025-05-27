using System.Runtime.InteropServices;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1;

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct Timestamp {
    public ushort year;
    public byte month;
    public byte day;
    public byte hour;
    public byte minute;
    public byte second;
    private fixed byte padding[1];

    public static Timestamp Now() {
        DateTime now = DateTime.Now;
        return new Timestamp() {
            year = (ushort)now.Year,
            month = (byte)now.Month,
            day = (byte)now.Day,
            hour = (byte)now.Hour,
            minute = (byte)now.Minute,
            second = (byte)now.Second
        };
    }

    public DateTime ToDateTime() {
        return new DateTime(year, month, day, hour, minute, second);
    }
}


[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct Version {
    public byte build;
    public byte minor;
    public ushort major;

    public override string ToString() {
        return $"{major:00}.{minor:00}.{build:00}";
    }
}