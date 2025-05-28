using System.Globalization;
using System.Runtime.InteropServices;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1;

/// <summary>
/// A record containing a timestamp.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct Timestamp {
    /// <summary>
    /// The year value.
    /// </summary>
    public ushort year;
    /// <summary>
    /// The month value.
    /// </summary>
    public byte month;
    /// <summary>
    /// The day value.
    /// </summary>
    public byte day;
    /// <summary>
    /// The hour value.
    /// </summary>
    public byte hour;
    /// <summary>
    /// The minute value.
    /// </summary>
    public byte minute;
    /// <summary>
    /// The second value.
    /// </summary>
    public byte second;
    private fixed byte padding[1];

    /// <summary>
    /// Creates a blank timestamp (0000-00-00 00:00:00)
    /// </summary>
    public Timestamp() {
    }

    /// <summary>
    /// Creates a timestamp from a <see cref="DateTime"/> object.
    /// </summary>
    /// <param name="date">The date to use.</param>
    public Timestamp(DateTime date) {
        year = (ushort)date.Year;
        month = (byte)date.Month;
        day = (byte)date.Day;
        hour = (byte)date.Hour;
        minute = (byte)date.Minute;
        second = (byte)date.Second;
    }

    /// <summary>
    /// Creates a timestamp that is set to the current time.
    /// </summary>
    /// <returns>A new timestamp containing the current date/time.</returns>
    public static Timestamp Now() {
        return new Timestamp(DateTime.Now);
    }

    /// <summary>
    /// Converts this Timestamp to a <see cref="DateTime"/> on the same date/time (with 0 ms/ns)
    /// </summary>
    /// <returns>A new DateTime with the current time.</returns>
    public DateTime ToDateTime() {
        return new DateTime(year, month, day, hour, minute, second);
    }

    /// <inheritdoc />
    public override string ToString() {
        return ToDateTime().ToString(CultureInfo.InvariantCulture);
    }
}


/// <summary>
/// A record containing a version number.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct Version {
    /// <summary>
    /// The "build" part of the version, the last part.
    /// </summary>
    public byte build;
    /// <summary>
    /// The "minor" part of the version, the middle part.
    /// </summary>
    public byte minor;
    /// <summary>
    /// The "major" part of the version, the first part.
    /// </summary>
    public ushort major;

    /// <inheritdoc />
    public override string ToString() {
        return $"{major:00}.{minor:00}.{build:00}";
    }
}