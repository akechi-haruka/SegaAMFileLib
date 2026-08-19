using System.Globalization;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.Misc;

/// <summary>
/// A parsed name for an fscrypt container.
/// </summary>
public class InstallFileName {
    private static readonly ILogger LOG = Log.GetOrCreate("InstallFileName");

    /// <summary>
    /// The type of the container (app/opt/pack).
    /// </summary>
    public FileType Type { get; internal set; }

    /// <summary>
    /// The 4-letter game ID.
    /// </summary>
    public String GameId { get; internal set; }

    /// <summary>
    /// The version of the container.
    /// </summary>
    public Version VersionNumber { get; internal set; }

    /// <summary>
    /// The name of the option package. Null on non-<see cref="FileType.Option"/>.
    /// </summary>
    public String OptionName { get; internal set; }

    /// <summary>
    /// The date of the container.
    /// </summary>
    public DateTime Date { get; internal set; }

    /// <summary>
    /// The sequence number of the container. (1+ = patch)
    /// </summary>
    public byte Sequence { get; internal set; }

    /// <summary>
    /// The parent version of this container. null if <see cref="Sequence"/> is 0.
    /// </summary>
    public Version RequiredVersion { get; internal set; }

    /// <summary>
    /// The required option for this option. Only exists on APMv3 inner .opts.
    /// </summary>
    public string RequiredOption { get; set; }

    /// <summary>
    /// Parses a file name.
    /// </summary>
    /// <example>
    /// * AAV_0001.00.00_20130101010100_0.pack<br />
    /// * SBXX_1.01.00_20130101010200_1_1.00.00.app<br />
    /// * SBXX_A003_20130101010300_0.opt
    /// </example>
    /// <param name="filename">The filename to parse.</param>
    /// <param name="apmOpt">Whether this file in an APM .opt file (which is actually an .app)</param>
    /// <returns>A parsed <see cref="InstallFileName"/>.</returns>
    /// <exception cref="ArgumentNullException">if filename is null.</exception>
    /// <exception cref="ArgumentException">if the filename is not a valid InstallFileName.</exception>
    public static InstallFileName Parse(string filename, bool apmOpt = false) {
        ArgumentNullException.ThrowIfNull(filename);
        InstallFileName f = new InstallFileName();
        if (filename.EndsWith(".pack")) {
            f.Type = FileType.Pack;
        } else if (filename.EndsWith(".app")) {
            f.Type = FileType.App;
        } else if (filename.EndsWith(".opt")) {
            f.Type = FileType.Option;
        }

        String[] fparts = Path.GetFileNameWithoutExtension(filename).Split("_");

        if (fparts.Length is < 4 or > 6) {
            throw new ArgumentException("Filename has invalid section count: " + filename);
        }

        String gameId = fparts[0];
        if (f.Type != FileType.Pack && !GameID.IsValid(gameId)) {
            throw new ArgumentException("Invalid game ID for app/opt: " + gameId);
        }

        if (f.Type == FileType.Pack && gameId.Any(c => !Char.IsAscii(c))) {
            throw new ArgumentException("Invalid ID for pack: " + gameId);
        }

        f.GameId = gameId;

        String version = fparts[1];
        if (f.Type != FileType.Option) {
            if (!Version.TryParse(version, out Version parsedVersion)) {
                throw new ArgumentException("Invalid version: " + version);
            }

            f.VersionNumber = parsedVersion;
        } else {
            if (version.Length != 4) {
                throw new ArgumentException("Option ID is invalid: " + version);
            }

            if (version.Any(c => !Char.IsAsciiDigit(c) && !Char.IsAsciiLetterUpper(c))) {
                throw new ArgumentException("Option ID contains non-uppercase, non-digit character: " + version);
            }

            f.OptionName = version;
        }

        String datestr = fparts[2];
        if (!DateTime.TryParseExact(datestr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)) {
            throw new ArgumentException("Invalid date: " + datestr);
        }

        f.Date = date;

        String sequenceStr = fparts[3];
        if (!Byte.TryParse(sequenceStr, out byte sequence)) {
            throw new ArgumentException("Sequence is invalid: " + sequenceStr);
        }

        f.Sequence = sequence;

        if (fparts.Length > 4) {
            String version2 = fparts[4];
            if (apmOpt) {
                f.RequiredOption = version2;
            } else {
                if (f.Type != FileType.App) {
                    throw new ArgumentException("Only app files can have a required version:" + filename);
                }

                if (!Version.TryParse(version2, out Version parsedVersion)) {
                    throw new ArgumentException("Invalid version: " + version2);
                }

                f.RequiredVersion = parsedVersion;
            }
        }

        LOG.LogDebug("Parsed " + filename + " as " + f.Type + " / " + f.GameId);

        return f;
    }

    /// <summary>
    /// Creates an InstallFileName from a <see cref="BootId"/>.
    /// </summary>
    /// <param name="bootId">The BootId to use.</param>
    /// <returns>An InstallFileName that describes the data in the given BootId.</returns>
    public static InstallFileName FromBootId(BootId bootId) {
        return new InstallFileName() {
            Type = bootId.containerType,
            GameId = bootId.GetAppId(),
            VersionNumber = bootId.gameVersion.ToVersion(),
            OptionName = null,
            Date = bootId.gameTimestamp.ToDateTime(),
            Sequence = bootId.sequenceNumber,
            RequiredVersion = bootId.sourceVersion.ToVersion()
        };
    }

    /// <summary>
    /// Creates an InstallFileName from the given parameters.
    /// </summary>
    /// <param name="type">The type of the container (app, opt, pack)</param>
    /// <param name="gameId">The 4-letter game ID.</param>
    /// <param name="version">The version number.</param>
    /// <param name="optionName">The name of the option data if the container is an option, otherwise null.</param>
    /// <param name="timestamp">The date of the container.</param>
    /// <param name="sequence">The sequence of the container.</param>
    /// <param name="requiredVersion">If the container is a patch (sequence > 0), the version of the parent container, null otherwise.</param>
    /// <returns>An InstallFileName that contains the given parameters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gameId"/> or <paramref name="version"/> are null.</exception>
    /// <exception cref="ArgumentException">if incompatible parameters are specified.</exception>
    public static InstallFileName Create(FileType type, String gameId, Version version, String optionName, DateTime timestamp, byte sequence = 0, Version requiredVersion = null) {
        ArgumentNullException.ThrowIfNull(gameId);
        ArgumentNullException.ThrowIfNull(version);

        if (type != FileType.Option && optionName != null) {
            throw new ArgumentException("Only options can have an optionName");
        }

        if (type != FileType.App && requiredVersion != null) {
            throw new ArgumentException("Only apps can have a requiredVersion");
        }

        if (sequence > 0 && requiredVersion == null) {
            throw new ArgumentException("If the file is part of a sequence, requiredVersion is required");
        }

        return new InstallFileName() {
            Type = type,
            GameId = gameId,
            VersionNumber = version,
            OptionName = optionName,
            Date = timestamp,
            Sequence = sequence,
            RequiredVersion = requiredVersion
        };
    }

    /// <summary>
    /// Creates a .app InstallFileName from the given parameters.
    /// </summary>
    /// <param name="gameId">The 4-letter game ID.</param>
    /// <param name="version">The version number.</param>
    /// <param name="sequence">The sequence of the container.</param>
    /// <param name="timestamp">The date of the container.</param>
    /// <param name="requiredVersion"></param>
    /// <returns>An InstallFileName that contains the given parameters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gameId"/> or <paramref name="version"/> are null.</exception>
    /// <exception cref="ArgumentException">if incompatible parameters are specified.</exception>
    public static InstallFileName CreateApp(String gameId, Version version, byte sequence, DateTime? timestamp = null, Version requiredVersion = null) {
        if (!GameID.IsValid(gameId)) {
            throw new ArgumentException("Game ID is invalid: " + gameId);
        }

        return Create(FileType.App, gameId, version, null, timestamp ?? DateTime.Now, sequence, requiredVersion);
    }

    /// <summary>
    /// Creates a .pack InstallFileName from the given parameters.
    /// </summary>
    /// <param name="gameId">The 4-letter game ID.</param>
    /// <param name="version">The version number.</param>
    /// <param name="sequence">The sequence of the container.</param>
    /// <param name="timestamp">The date of the container.</param>
    /// <returns>An InstallFileName that contains the given parameters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gameId"/> or <paramref name="version"/> are null.</exception>
    /// <exception cref="ArgumentException">if incompatible parameters are specified.</exception>
    public static InstallFileName CreatePack(String gameId, Version version, byte sequence, DateTime? timestamp = null) {
        return Create(FileType.Pack, gameId, version, null, timestamp ?? DateTime.Now, sequence);
    }

    /// <summary>
    /// Creates a .opt InstallFileName from the given parameters.
    /// </summary>
    /// <param name="gameId">The 4-letter game ID.</param>
    /// <param name="optionName">The name of the option data if the container is an option, otherwise null.</param>
    /// <param name="version">The version number.</param>
    /// <param name="sequence">The sequence of the container.</param>
    /// <param name="timestamp">The date of the container.</param>
    /// <returns>An InstallFileName that contains the given parameters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gameId"/> or <paramref name="version"/> are null.</exception>
    /// <exception cref="ArgumentException">if incompatible parameters are specified.</exception>
    public static InstallFileName CreateOption(String gameId, String optionName, Version version, byte sequence, DateTime? timestamp = null) {
        if (!GameID.IsValid(gameId)) {
            throw new ArgumentException("Game ID is invalid: " + gameId);
        }

        return Create(FileType.Option, gameId, version, optionName, timestamp ?? DateTime.Now, sequence);
    }

    /// <summary>
    /// Gets the file extension for this container.
    /// </summary>
    /// <example>.app</example>
    /// <returns>the file extension for this container.</returns>
    /// <exception cref="ArgumentException">if the <see cref="Type"/> is invalid.</exception>
    public String GetContainerFileExtension() {
        return Type switch {
            FileType.App => ".app",
            FileType.Option => ".opt",
            FileType.Pack => ".pack",
            _ => throw new ArgumentException("Invalid container type: " + Type)
        };
    }

    /// <inheritdoc/>
    public override string ToString() {
        return GetFileName();
    }

    /// <summary>
    /// Converts the given InstallFileName to a string that represents the installation file.
    /// </summary>
    /// <example>SBXX_1.01.00_20130101010200_1_1.00.00.app</example>
    /// <returns>a string representation of this object.</returns>
    public string GetFileName() {
        return (Type == FileType.Pack ? "ACA" : GameId) + // TODO: stop hardcoding ACA everywhere
               "_" +
               (Type == FileType.Option ? OptionName : (Type == FileType.Pack ? $"{VersionNumber.Major:D4}.{VersionNumber.Minor:D2}.{VersionNumber.Build:D2}" : $"{VersionNumber.Major:D}.{VersionNumber.Minor:D2}.{VersionNumber.Build:D2}")) +
               "_" +
               Date.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) +
               "_" +
               Sequence +
               (RequiredVersion != null ? "_" + $"{RequiredVersion.Major:D}.{RequiredVersion.Minor:D2}.{RequiredVersion.Build:D2}" : "") +
               GetContainerFileExtension();
    }

    /// <summary>
    /// Returns true if this InstallFileName is for APM v3.
    /// </summary>
    /// <returns>true if this InstallFileName is for APM v3</returns>
    public bool IsApm() {
        return GameID.IsApm(GameId);
    }

    /// <summary>
    /// Converts the given InstallFileName to a filesystem label.
    /// </summary>
    /// <returns>a string representation of this object for the purpose of being used as a file system label.</returns>
    public string GetFileSystemLabel() {
        return GameId + "_" + VersionNumber + "_" + Sequence;
    }

    private InstallFileName() {
    }

    /// <summary>
    /// Possible file types for an installation file.
    /// </summary>
    public enum FileType : byte {
        /// <summary>
        /// A .pack file.
        /// </summary>
        Pack = 0x0,

        /// <summary>
        /// A .app file.
        /// </summary>
        App = 0x1,

        /// <summary>
        /// A .opt file.
        /// </summary>
        Option = 0x2,

        /// <summary>
        /// The maximum possible value of this enum.
        /// </summary>
        Max = 0x2
    }
}