using System.Globalization;
using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

namespace Haruka.Arcade.SegaAMFileLib.Misc;

public class InstallFile {
    public FileType Type { get; internal set; }
    public String GameId { get; internal set; }
    public Version VersionNumber { get; internal set; }
    public String OptionName { get; internal set; }
    public DateTime Date { get; internal set; }
    public byte Sequence { get; internal set; }
    public Version RequiredAppVersion { get; internal set; }

    public static InstallFile Parse(string filename) {
        // AAV_0001.00.00_20130101010100_0.pack
        // SBXX_1.01.00_20130101010200_1_1.00.00.app
        // SBXX_A003_20130101010300_0.opt

        ArgumentNullException.ThrowIfNull(filename);
        InstallFile f = new InstallFile();
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
            if (f.Type != FileType.App) {
                throw new ArgumentException("Only app files can have a required version:" + filename);
            }

            if (!Version.TryParse(version2, out Version parsedVersion)) {
                throw new ArgumentException("Invalid version: " + version2);
            }

            f.RequiredAppVersion = parsedVersion;
        }

        return f;
    }

    public static InstallFile FromBootId(BootId bootId) {
        return new InstallFile() {
            Type = bootId.containerType,
            GameId = bootId.GetAppId(),
            VersionNumber = bootId.gameVersion.ToVersion(),
            OptionName = null,
            Date = bootId.gameTimestamp.ToDateTime(),
            Sequence = bootId.sequenceNumber,
            RequiredAppVersion = bootId.sourceVersion.ToVersion()
        };
    }

    public static InstallFile Create(FileType type, String gameId, Version version, String optionName, DateTime timestamp, byte sequence, Version requiredVersion) {
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

        return new InstallFile() {
            Type = type,
            GameId = gameId,
            VersionNumber = version,
            OptionName = optionName,
            Date = timestamp,
            Sequence = sequence,
            RequiredAppVersion = requiredVersion
        };
    }

    public static InstallFile CreateApp(String gameId, Version version, byte sequence, DateTime? timestamp = null, Version requiredVersion = null) {
        if (!GameID.IsValid(gameId)) {
            throw new ArgumentException("Game ID is invalid: " + gameId);
        }

        return Create(FileType.App, gameId, version, null, timestamp ?? DateTime.Now, sequence, requiredVersion);
    }

    public static InstallFile CreatePack(String gameId, Version version, byte sequence, DateTime? timestamp = null) {
        return Create(FileType.Pack, gameId, version, null, timestamp ?? DateTime.Now, sequence, null);
    }

    public static InstallFile CreateOption(String gameId, String optionName, Version version, byte sequence, DateTime? timestamp = null) {
        if (!GameID.IsValid(gameId)) {
            throw new ArgumentException("Game ID is invalid: " + gameId);
        }

        return Create(FileType.Option, gameId, version, optionName, timestamp ?? DateTime.Now, sequence, null);
    }

    public String GetContainerFileExtension() {
        return Type switch {
            FileType.App => ".app",
            FileType.Option => ".opt",
            FileType.Pack => ".pack",
            _ => throw new ArgumentException("Invalid container type: " + Type)
        };
    }

    public override string ToString() {
        return GetFileName();
    }

    public string GetFileName() {
        return GameId +
               "_" +
               (Type == FileType.Option ? OptionName : $"{VersionNumber.Major:D}.{VersionNumber.Minor:D2}.{VersionNumber.Build:D2}") +
               "_" +
               Date.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) +
               "_" +
               Sequence +
               (RequiredAppVersion != null ? "_" + $"{RequiredAppVersion.Major:D}.{RequiredAppVersion.Minor:D2}.{RequiredAppVersion.Build:D2}" : "") +
               GetContainerFileExtension();
    }

    public bool IsApm() {
        return GameID.IsApm(GameId);
    }

    public string GetFileSystemLabel() {
        return GameId + "_" + VersionNumber + "_" + Sequence;
    }

    private InstallFile() {
        // AAV_0001.00.00_20130101010100_0.pack
        // SBXX_1.01.00_20130101010200_1_1.00.00.app
        // SBXX_A003_20130101010300_0.opt
    }


    public enum FileType : byte {
        Pack = 0x0,
        App = 0x1,
        Option = 0x2,
        Max = 0x2
    }
}