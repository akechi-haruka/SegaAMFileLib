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

    public Timestamp() {
    }

    public Timestamp(DateTime date) {
        year = (ushort)date.Year;
        month = (byte)date.Month;
        day = (byte)date.Day;
        hour = (byte)date.Hour;
        minute = (byte)date.Minute;
        second = (byte)date.Second;
    }

    public static Timestamp Now() {
        return new Timestamp(DateTime.Now);
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