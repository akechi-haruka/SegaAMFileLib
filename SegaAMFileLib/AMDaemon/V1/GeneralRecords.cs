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
    /// <param name="offset">The amount of seconds to offset from the current time.</param>
    /// <returns>A new timestamp containing the current date/time.</returns>
    public static Timestamp Now(int offset = 0) {
        return new Timestamp(DateTime.Now + TimeSpan.FromSeconds(offset));
    }

    /// <summary>
    /// Converts this Timestamp to a <see cref="DateTime"/> on the same date/time (with 0 ms/ns)
    /// </summary>
    /// <returns>A new DateTime with the current time.</returns>
    public DateTime ToDateTime() {
        if (year <= 0 || month <= 0 || day <= 0) {
            return DateTime.MinValue;
        }

        try {
            return new DateTime(year, month, day, hour, minute, second);
        } catch (ArgumentOutOfRangeException ex) {
            throw new ArgumentException("Timestamp record is invalid and cannot be converted to string (" + year + ", " + month + ", " + day + ")", ex);
        }
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
public record struct Version {
    /// <summary>
    /// Constant version 0.0.0
    /// </summary>
    public static Version Empty { get; } = new Version();

    /// <summary>
    /// Creates a version with all fields set to zero. (0.0.0)
    /// </summary>
    public Version() {
    }

    /// <summary>
    /// Creates a version that is a copy of a <see cref="System.Version"/>.
    /// </summary>
    /// <param name="version">The version to copy.</param>
    public Version(System.Version version) {
        major = (ushort)version.Major;
        minor = (byte)version.Minor;
        build = (byte)version.Build;
    }

    /// <summary>
    /// Creates a version with the given values.
    /// </summary>
    /// <param name="major">The "major" part of the version number.</param>
    /// <param name="minor">The "minor" part of the version number.</param>
    /// <param name="build">The "build" part of the version number.</param>
    public Version(ushort major, byte minor, byte build) {
        this.major = major;
        this.minor = minor;
        this.build = build;
    }

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

    /// <summary>
    /// Converts the given (SEGA) Version to a <see cref="System.Version"/>.
    /// </summary>
    /// <returns>A new System.Version with the same values.</returns>
    public System.Version ToVersion() {
        return new System.Version(major, minor, build);
    }
}