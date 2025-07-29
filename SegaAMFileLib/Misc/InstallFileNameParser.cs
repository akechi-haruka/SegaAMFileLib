using System.Globalization;

namespace Haruka.Arcade.SegaAMFileLib.Misc;

public class InstallFile {
    public FileType Type { get; internal set; }
    public String GameId { get; internal set; }
    public Version VersionNumber { get; internal set; }
    public String OptionName { get; internal set; }
    public DateTime Date { get; internal set; }
    public int Generation { get; internal set; }
    public Version RequiredAppVersion { get; internal set; }

    public static InstallFile Parse(string filename) {
        // AAV_0001.00.00_20130101010100_0.pack
        // SBXX_1.01.00_20130101010200_1_1.00.00.app
        // SBXX_A003_20130101010300_0.opt

        ArgumentNullException.ThrowIfNull(filename);
        InstallFile f = new InstallFile();
        if (filename.Contains(".pack")) {
            f.Type = FileType.Pack;
        } else if (filename.Contains(".app")) {
            f.Type = FileType.App;
        } else if (filename.Contains(".opt")) {
            f.Type = FileType.Opt;
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
        if (f.Type != FileType.Opt) {
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

        String generationstr = fparts[3];
        if (!Int32.TryParse(generationstr, out int generation)) {
            throw new ArgumentException("Generation is invalid: " + generationstr);
        }
        if (generation is < 0 or > 255) {
            throw new ArgumentException("Generation is out of range: " + generation);
        }
        f.Generation = generation;

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

    private InstallFile() {
    }


    public enum FileType {
        Pack,
        App,
        Opt
    }
}