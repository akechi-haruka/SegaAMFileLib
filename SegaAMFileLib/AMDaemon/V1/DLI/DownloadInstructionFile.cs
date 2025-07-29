using System.Globalization;
using System.Text;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.DLI;

/// <summary>
/// Class to read and write Download Instrucion Files. (sic)
/// </summary>
public class DownloadInstructionFile {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(DownloadInstructionFile));

    private const string COMMON_SECTION = "COMMON";
    private const string FOREGROUND_SECTION = "FOREGROUND";
    private const string OPTION_IMAGE_SECTION = "OPTION_IMAGE";
    private const string OPTION_SECTION = "OPTIONAL";
    private const string IGNORE_TIME_SECTION = "IGNORE_RELEASE_TIME";

    /// <summary>
    /// Common data. This specifies download ID, game ID, download times, speeds, URLs and more.
    /// </summary>
    public CommonInfo Common { get; private set; }

    /// <summary>
    /// Foreground download settings. Unknown if this is used anywhere. May be null.
    /// Requires DLFORMAT 5.00+.
    /// </summary>
    public ForegroundInfo Foreground { get; set; }

    /// <summary>
    /// Option Image URL. Unknown if this is used anywhere. May be null if this is not an option DLI.
    /// </summary>
    public OptionImage OptionImg { get; set; }

    /// <summary>
    /// Option data. Contains option information and download URLs. May be null if this is not an option DLI.
    /// </summary>
    public Optional Option { get; set; }

    /// <summary>
    /// Creates a new blank DLI file.
    /// </summary>
    public DownloadInstructionFile() {
        Common = new CommonInfo();
    }

    /// <summary>
    /// Reads a DLI from file.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="type">The type of DLI (app, opt).</param>
    /// <param name="checkDownloadId">Whether or not to validate the download ID in the file.</param>
    /// <exception cref="IOException">if there is an I/O error, a syntax or logical error reading the DLI file.</exception>
    public DownloadInstructionFile(String filePath, DliType type, bool checkDownloadId = true) {
        LOG.LogInformation("Reading DLI file from {f} of type {t}", filePath, type);
        Read(File.ReadAllLines(filePath, Encoding.UTF8), type);
    }

    /// <summary>
    /// Reads a DLI from string.
    /// </summary>
    /// <param name="content">The lines of the DLI.</param>
    /// <param name="type">The type of DLI (app, opt).</param>
    /// <param name="checkDownloadId">Whether or not to validate the download ID in the file.</param>
    /// <exception cref="IOException">if there is a syntax or logical error reading the DLI file.</exception>
    public DownloadInstructionFile(String[] content, DliType type, bool checkDownloadId = true) {
        LOG.LogInformation("Reading DLI file of type {t}", type);
        Read(content, type);
    }

    private void Read(String[] content, DliType type, bool checkDownloadId = true) {
        IniParser dli = new IniParser(content);
        if (!dli.HasSection(COMMON_SECTION)) {
            throw new IOException(COMMON_SECTION + " is missing");
        }

        Common = new CommonInfo();

        ReadDlFormat(dli);
        ReadGameId(dli);
        ReadOrderTime(dli);
        ReadReleaseTime(dli);
        ReadPartSize(dli);
        Common.IsdnDownloadInterval = ReadInterval(dli, "INTERVAL");
        Common.AdslDownloadInterval = ReadInterval(dli, "DSL_INTERVAL");
        Common.BroadbandDownloadInterval = ReadInterval(dli, "BB_INTERVAL");
        ReadCloud(dli);
        ReadReport(dli);
        ReadReportInterval(dli);
        ReadGameDesc(dli);
        ReadReleaseType(dli, type);
        Common.InstallUrls = ReadUrls(dli, COMMON_SECTION, "INSTALL");
        Common.ExistUrls = ReadUrls(dli, COMMON_SECTION, "EXIST");
        ReadDownloadId(dli);
        ReadImmediatelyRelease(dli);
        ReadReleaseWithOption(dli);
        Common.PrivateInstallUrls = ReadUrls(dli, COMMON_SECTION, "PRIVATE_INSTALL");
        ReadObsoleteCommon(dli);

        OptionImg = new OptionImage();

        ReadOptionImage(dli, type);

        Option = new Optional();

        ReadOptionSection(dli, type);

        if (ReadForegroundSection(dli)) {
            Foreground = new ForegroundInfo();

            ReadForegroundPartSize(dli);
            ReadForegroundInterval(dli);
            Foreground.ImageUrls = ReadUrls(dli, FOREGROUND_SECTION, "IMAGE");
            ReadForegroundOrderTime(dli);
            ReadForegroundReleaseConfirm(dli);
        }

        if (checkDownloadId && (Common.DlFormat <= 4.10 || (Common.DlFormat >= 5.00 && type != DliType.Opt))) {
            uint calculated = DownloadIdCalculator.GetDownloadId(this);

            if (calculated != Common.DownloadId) {
                throw new IOException("Download ID mismatch: given " + Common.DownloadId + ", expected: " + calculated);
            }
        }
    }

    /// <summary>
    /// Writes this DLI to a string.
    /// </summary>
    /// <param name="type">The type of DLI (app, opt).</param>
    /// <returns>A string containing the DLI in .ini form.</returns>
    public String Write(DliType type) {
        IniParser output = new IniParser(new string[0]);
        output.AddSetting(COMMON_SECTION, "DLFORMAT", Common.DlFormat.ToString("F2", CultureInfo.InvariantCulture));
        output.AddSetting(COMMON_SECTION, "GAME_ID", Common.GameId);
        output.AddSetting(COMMON_SECTION, "RELEASE_TIME", Common.ReleaseTime.ToUniversalTime());
        output.AddSetting(COMMON_SECTION, "ORDER_TIME", Common.OrderTime.ToUniversalTime());
        output.AddSetting(COMMON_SECTION, "PART_SIZE", String.Join(',', Common.PartSize));
        output.AddSetting(COMMON_SECTION, "INTERVAL", String.Join(',', Common.IsdnDownloadInterval));
        output.AddSetting(COMMON_SECTION, "DSL_INTERVAL", String.Join(',', Common.AdslDownloadInterval));
        output.AddSetting(COMMON_SECTION, "BB_INTERVAL", String.Join(',', Common.BroadbandDownloadInterval));
        output.AddSetting(COMMON_SECTION, "CLOUD", CreateCloudString(Common.CloudDownloadAllowed));
        output.AddSetting(COMMON_SECTION, "REPORT", Common.ReportUrl);
        if (Common.ReportInterval > 0) {
            output.AddSetting(COMMON_SECTION, "REPORT_INTERVAL", Common.ReportInterval);
        }

        if (Common.GameDescription != null) {
            output.AddSetting(COMMON_SECTION, "GAME_DESC", Common.GameDescription);
        }

        if (type == DliType.Opt && Common.ReleaseType != null) {
            output.AddSetting(COMMON_SECTION, "RELEASE_TYPE", (int)Common.ReleaseType);
        }

        for (int i = 0; i < Common.InstallUrls.Length; i++) {
            string url = Common.InstallUrls[i];
            output.AddSetting(COMMON_SECTION, "INSTALL" + (i + 1), url);
        }

        for (int i = 0; i < Common.ExistUrls.Length; i++) {
            string url = Common.ExistUrls[i];
            output.AddSetting(COMMON_SECTION, "EXIST" + (i + 1), url);
        }

        for (int i = 0; i < Common.PrivateInstallUrls.Length; i++) {
            string url = Common.PrivateInstallUrls[i];
            output.AddSetting(COMMON_SECTION, "PRIVATE_INSTALL" + (i + 1), url);
        }

        output.AddSetting(COMMON_SECTION, "DOWNLOAD_ID", DownloadIdCalculator.GetDownloadId(this));

        output.AddSetting(COMMON_SECTION, "IMMEDIATELY_RELEASE", Common.ImmediatelyRelease ? 1 : 0);
        output.AddSetting(COMMON_SECTION, "RELEASE_WITH_OPTION", Common.ReleaseWithOption ? 1 : 0);

        if (type == DliType.Opt) {
            if (OptionImg != null && OptionImg.Url != null) {
                output.AddSetting(OPTION_IMAGE_SECTION, "URL", OptionImg.Url);
            }

            if (Option != null) {
                string section = Option.IgnoreReleaseTime ? IGNORE_TIME_SECTION : OPTION_SECTION;

                for (int i = 0; i < Option.InstallUrls.Length; i++) {
                    string url = Option.InstallUrls[i];
                    output.AddSetting(section, "INSTALL" + (i + 1), url);
                }

                for (int i = 0; i < Option.PrivateInstallUrls.Length; i++) {
                    string url = Option.PrivateInstallUrls[i];
                    output.AddSetting(section, "PRIVATE_INSTALL" + (i + 1), url);
                }
            }
        }

        if (Foreground != null) {
            output.AddSetting(FOREGROUND_SECTION, "PART_SIZE", Foreground.PartSize);
            output.AddSetting(FOREGROUND_SECTION, "INTERVAL", Foreground.Interval);
            output.AddSetting(FOREGROUND_SECTION, "ORDER_TIME", Foreground.OrderTime.ToUniversalTime());
            output.AddSetting(FOREGROUND_SECTION, "RELEASE_CONFIRM", Foreground.ReleaseConfirm ? 1 : 0);
            for (int i = 0; i < Foreground.ImageUrls.Length; i++) {
                string url = Foreground.ImageUrls[i];
                output.AddSetting(FOREGROUND_SECTION, "IMAGE" + (i + 1), url);
            }
        }

        return output.SaveSettings();
    }

    private static String CreateCloudString(CloudDownload[] c) {
        StringBuilder sb = new StringBuilder();
        foreach (CloudDownload d in c) {
            sb.Append((char)(((int)d) + '0'));
        }

        return sb.ToString();
    }

    private void ReadForegroundReleaseConfirm(IniParser dli) {
        String str = dli.GetSetting(FOREGROUND_SECTION, "RELEASE_CONFIRM");
        if (String.IsNullOrWhiteSpace(str)) {
            LOG.LogDebug("RELEASE_CONFIRM is unset");
            return;
        }

        if (!Int32.TryParse(str, out int rc)) {
            throw new IOException("RELEASE_CONFIRM format is invalid: " + str);
        }

        if (rc != 0 && rc != 1) {
            throw new IOException("RELEASE_CONFIRM value is invalid: " + rc);
        }

        Foreground.ReleaseConfirm = rc == 1;
    }

    private void ReadForegroundOrderTime(IniParser dli) {
        if (!DateTime.TryParse(dli.GetSetting(FOREGROUND_SECTION, "ORDER_TIME"), out DateTime date)) {
            throw new IOException("Failed to parse ORDER_TIME");
        }

        Foreground.OrderTime = date;
    }

    private void ReadForegroundInterval(IniParser dli) {
        String str = dli.GetSetting(FOREGROUND_SECTION, "INTERVAL");
        if (String.IsNullOrWhiteSpace(str)) {
            throw new IOException("INTERVAL is unset");
        }

        if (!Int32.TryParse(str, out int interval)) {
            throw new IOException("INTERVAL format is invalid: " + str);
        }

        if (interval < 0) {
            throw new IOException("INTERVAL value is invalid: " + interval);
        }

        Foreground.Interval = interval;
    }

    private static bool IsPowerOfTwo(ulong x) {
        return (x != 0) && ((x & (x - 1)) == 0);
    }

    private void ReadForegroundPartSize(IniParser dli) {
        String str = dli.GetSetting(FOREGROUND_SECTION, "PART_SIZE");
        if (String.IsNullOrWhiteSpace(str)) {
            throw new IOException("PART_SIZE is unset");
        }

        if (!UInt32.TryParse(str, out uint size)) {
            throw new IOException("PART_SIZE format is invalid: " + str);
        }

        if (size <= 1 || !IsPowerOfTwo(size)) {
            throw new IOException("PART_SIZE value is invalid: " + size);
        }

        Foreground.PartSize = size;
    }

    private bool ReadForegroundSection(IniParser dli) {
        return dli.HasSection(FOREGROUND_SECTION);
    }

    private void ReadOptionSection(IniParser dli, DliType type) {
        bool hasOptional = dli.HasSection(OPTION_SECTION);
        bool hasIgnoreTime = dli.HasSection(IGNORE_TIME_SECTION);

        if (hasOptional && hasIgnoreTime) {
            throw new IOException(OPTION_SECTION + " and " + IGNORE_TIME_SECTION + " can not exist at the same time");
        }

        if (type == DliType.App && (hasOptional || hasIgnoreTime)) {
            throw new IOException("DLI of type " + type + " can't have a " + OPTION_SECTION + " or " + IGNORE_TIME_SECTION + " section");
        }

        if (hasOptional || hasIgnoreTime) {
            if (hasOptional) {
                Option.InstallUrls = ReadUrls(dli, OPTION_SECTION, "INSTALL");
                Option.PrivateInstallUrls = ReadUrls(dli, OPTION_SECTION, "PRIVATE_INSTALL");
            } else {
                Option.InstallUrls = ReadUrls(dli, IGNORE_TIME_SECTION, "INSTALL");
                Option.PrivateInstallUrls = ReadUrls(dli, IGNORE_TIME_SECTION, "PRIVATE_INSTALL");
            }

            if (hasIgnoreTime) {
                Option.IgnoreReleaseTime = true;
            }
        }
    }

    private void ReadOptionImage(IniParser dli, DliType type) {
        if (type != DliType.Opt) {
            if (dli.HasSection(OPTION_IMAGE_SECTION)) {
                throw new IOException("DLI of type " + type + " cannot have a " + OPTION_IMAGE_SECTION + " section");
            }

            return;
        }

        if (!dli.HasSection(OPTION_IMAGE_SECTION)) {
            LOG.LogDebug("Missing " + OPTION_IMAGE_SECTION + " section");
            return;
        }

        String str = dli.GetSetting(OPTION_IMAGE_SECTION, "URL");
        if (String.IsNullOrWhiteSpace(str)) {
            LOG.LogWarning(OPTION_IMAGE_SECTION + ", URL is unset");
            return;
        }

        if (str.Length >= 256) {
            throw new IOException("URL is too long: " + str);
        }

        if (str.Any(c => !Char.IsAscii(c))) {
            throw new IOException("URL contains a non-ASCII char: " + str);
        }

        OptionImg.Url = str;
    }

    private void ReadObsoleteCommon(IniParser dli) {
        CheckObsoleteKey(dli, COMMON_SECTION, "GAME_VER");
        CheckObsoleteKey(dli, COMMON_SECTION, "GAME_TIME_STAMP");
        CheckObsoleteKey(dli, COMMON_SECTION, "TOTAL_PARTITION");
        CheckObsoleteKey(dli, COMMON_SECTION, "PARTITION01");
        CheckObsoleteKey(dli, COMMON_SECTION, "PARTITION02");
        CheckObsoleteKey(dli, COMMON_SECTION, "REPORT_TIME");
    }

    private void CheckObsoleteKey(IniParser dli, string s, string k) {
        if (dli.GetSetting(COMMON_SECTION, k) != null) {
            LOG.LogWarning("Obsolete key found: {s}, {k}", s, k);
        }
    }

    private void ReadReleaseWithOption(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "RELEASE_WITH_OPTION");
        if (String.IsNullOrWhiteSpace(str)) {
            LOG.LogDebug("RELEASE_WITH_OPTION is unset");
            return;
        }

        if (!Int32.TryParse(str, out int rwo)) {
            throw new IOException("RELEASE_WITH_OPTION format is invalid: " + str);
        }

        if (rwo != 0 && rwo != 1) {
            throw new IOException("RELEASE_WITH_OPTION value is invalid: " + rwo);
        }

        Common.ReleaseWithOption = rwo == 1;
    }

    private void ReadImmediatelyRelease(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "IMMEDIATELY_RELEASE");
        if (String.IsNullOrWhiteSpace(str)) {
            LOG.LogDebug("IMMEDIATELY_RELEASE is unset");
            return;
        }

        if (!Int32.TryParse(str, out int ir)) {
            throw new IOException("IMMEDIATELY_RELEASE format is invalid: " + str);
        }

        if (ir != 0 && ir != 1) {
            throw new IOException("IMMEDIATELY_RELEASE value is invalid: " + ir);
        }

        Common.ImmediatelyRelease = ir == 1;
    }

    private void ReadDownloadId(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "DOWNLOAD_ID");
        if (String.IsNullOrWhiteSpace(str)) {
            throw new IOException("DOWNLOAD_ID is unset");
        }

        if (!UInt32.TryParse(str, out uint downloadId)) {
            throw new IOException("DOWNLOAD_ID is invalid: " + str);
        }

        Common.DownloadId = downloadId;
    }

    private string[] ReadUrls(IniParser dli, string section, string key) {
        List<string> urls = new List<string>();
        for (int i = 1;; i++) {
            String url = dli.GetSetting(section, key + i);
            if (String.IsNullOrWhiteSpace(url)) {
                break;
            }

            if (url.Length >= 256) {
                throw new IOException(key + i + " is too long: " + url);
            }

            if (url.Any(c => !Char.IsAscii(c))) {
                throw new IOException(key + i + " contains a non-ASCII char: " + url);
            }

            String filename = url.Split('/').Last();

            try {
                InstallFile file = InstallFile.Parse(filename);
                if (file.Type == InstallFile.FileType.Pack && section == "INSTALL") {
                    throw new IOException(key + i + " is a operating system image, which is not allowed here");
                }
            } catch (ArgumentException e) {
                throw new IOException("Failed to parse filename: " + filename, e);
            }

            urls.Add(url);
        }

        return urls.ToArray();
    }

    private void ReadReleaseType(IniParser dli, DliType type) {
        String str = dli.GetSetting(COMMON_SECTION, "RELEASE_TYPE");
        if (String.IsNullOrWhiteSpace(str)) {
            if (type == DliType.Opt) {
                throw new IOException("RELEASE_TYPE is unset");
            }

            return;
        }

        if (type == DliType.App) {
            throw new IOException("RELEASE_TYPE is not allowed with DLI type " + type);
        }

        if (!Int32.TryParse(str, out int rtype)) {
            throw new IOException("RELEASE_TYPE format is invalid: " + str);
        }

        if (rtype is < 0 or > (int)OptReleaseType.Max) {
            throw new IOException("RELEASE_TYPE value is invalid: " + rtype);
        }

        Common.ReleaseType = (OptReleaseType)rtype;
    }

    private void ReadGameDesc(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "GAME_DESC");
        if (String.IsNullOrWhiteSpace(str)) {
            Logging.Main.LogWarning("GAME_DESC is unset");
            return;
        }

        if (str.Length >= 2048) {
            throw new IOException("GAME_DESC is too long: " + str);
        }

        Common.GameDescription = str;
    }

    private void ReadReportInterval(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "REPORT_INTERVAL");
        if (String.IsNullOrWhiteSpace(str)) {
            Logging.Main.LogWarning("REPORT_INTERVAL is unset");
            Common.ReportInterval = 3600;
            return;
        }

        if (!Int32.TryParse(str, out int interval)) {
            throw new IOException("REPORT_INTERVAL is invalid: " + str);
        }

        interval = Math.Max(60, interval);

        if (interval is <= 900 or >= 86400) {
            LOG.LogWarning("REPORT_INTERVAL is too long/short: {i}", interval);
        }

        Common.ReportInterval = interval;
    }

    private void ReadReport(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "REPORT");
        if (String.IsNullOrWhiteSpace(str)) {
            throw new IOException("REPORT is unset");
        }

        if (str.Length >= 256) {
            throw new IOException("REPORT is too long: " + str);
        }

        if (str.Any(c => !Char.IsAscii(c))) {
            throw new IOException("REPORT contains a non-ASCII character: " + str);
        }

        Common.ReportUrl = str;
    }

    private void ReadCloud(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "CLOUD");
        if (String.IsNullOrWhiteSpace(str)) {
            throw new IOException("CLOUD is unset");
        }

        const int cloudLength = 48;

        if (str.Length != cloudLength) {
            throw new IOException("CLOUD does not have " + cloudLength + " values: " + str);
        }

        CloudDownload[] values = new CloudDownload[cloudLength];
        for (int i = 0; i < str.Length; i++) {
            char c = str[i];
            values[i] = c switch {
                '0' => CloudDownload.Allowed,
                '1' => CloudDownload.NotAllowed,
                '2' => CloudDownload.UnknownValue,
                _ => throw new IOException("Syntax error in CLOUD: " + str)
            };
        }

        if (values.Count(c => c == CloudDownload.NotAllowed) == cloudLength) {
            throw new IOException("CLOUD download is never allowed");
        }

        Common.CloudDownloadAllowed = values;
    }

    private int[] ReadInterval(IniParser dli, String intervalKey) {
        String str = dli.GetSetting(COMMON_SECTION, intervalKey);
        if (String.IsNullOrWhiteSpace(str)) {
            throw new IOException(intervalKey + " is unset");
        }

        int[] intervals = str.Split(',').Select(s => {
            if (!Int32.TryParse(s, out int interval)) {
                throw new IOException(intervalKey + " contains an invalid value:" + s);
            }

            if (interval < 1000 && interval != -1) {
                throw new IOException(intervalKey + " contains a too low value: " + s);
            }

            return interval;
        }).ToArray();

        if (intervals.Length > 4) {
            throw new IOException(intervalKey + " contains too many values: " + str);
        }

        return intervals;
    }

    private void ReadPartSize(IniParser dli) {
        String str = dli.GetSetting(COMMON_SECTION, "PART_SIZE");
        if (String.IsNullOrWhiteSpace(str)) {
            throw new IOException("PART_SIZE is unset");
        }

        uint[] sizes = str.Split(',').Select(s => {
            if (!UInt32.TryParse(s, out uint size)) {
                throw new IOException("PART_SIZE contains an invalid format: " + s);
            }

            if (size <= 1 || !IsPowerOfTwo(size)) {
                throw new IOException("PART_SIZE contains an invalid value: " + s);
            }

            return size;
        }).ToArray();

        if (sizes.Length > 3) {
            throw new IOException("PART_SIZE contains too many values: " + str);
        }

        for (int m = 0; m < 2u; ++m) {
            for (int n = m + 1; n < 3u; ++n) {
                if (sizes[m] > 0 && sizes[n] > 0 && sizes[n] < sizes[m]) {
                    LOG.LogWarning("PART_SIZE value {n} is lower than {m}", sizes[n], sizes[m]);
                }
            }
        }

        Common.PartSize = sizes;
    }

    private void ReadOrderTime(IniParser dli) {
        if (!DateTime.TryParse(dli.GetSetting(COMMON_SECTION, "ORDER_TIME"), out DateTime date)) {
            throw new IOException("Failed to parse ORDER_TIME");
        }

        Common.OrderTime = date;
    }

    private void ReadReleaseTime(IniParser dli) {
        if (!DateTime.TryParse(dli.GetSetting(COMMON_SECTION, "RELEASE_TIME"), out DateTime date)) {
            throw new IOException("Failed to parse RELEASE_TIME");
        }

        Common.ReleaseTime = date;
    }

    private void ReadGameId(IniParser dli) {
        Common.GameId = dli.GetSetting(COMMON_SECTION, "GAME_ID");
        if (Common.GameId == null) {
            throw new IOException("GAME_ID is unset");
        }

        if (!GameID.IsValid(Common.GameId)) {
            throw new IOException("GAME_ID is invalid: " + Common.GameId);
        }
    }

    private void ReadDlFormat(IniParser dli) {
        if (!Single.TryParse(dli.GetSetting(COMMON_SECTION, "DLFORMAT"), CultureInfo.InvariantCulture, out float dlformat)) {
            throw new IOException("DLFORMAT is invalid");
        }

        if (dlformat < 4.00 || dlformat > 5.00) {
            throw new IOException("DLFORMAT is out of range: " + dlformat);
        }

        Common.DlFormat = dlformat;
    }

    /// <summary>
    /// Common data. This specifies download ID, game ID, download times, speeds, URLs and more.
    /// </summary>
    public class CommonInfo {
        /// <summary>
        /// The format of this DLI. Observed values are 4.10, 4.20 and 5.00.
        /// </summary>
        public float DlFormat { get; set; }

        /// <summary>
        /// The 4-letter SEGA game ID which is being downloaded.
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// The timestamp when the contents in this DLI should be downloaded.
        /// </summary>
        public DateTime OrderTime { get; set; }

        /// <summary>
        /// The timestamp when the contents in this DLI should be installed.
        /// </summary>
        public DateTime ReleaseTime { get; set; }

        /// <summary>
        /// For ISDN, ADSL and broadband respectively the chunk size for which HTTP requests are sent. This array must have exactly 3 elements and values must be a power of 2.
        /// </summary>
        public uint[] PartSize { get; set; }

        /// <summary>
        /// The download delay between requests on ISDN.
        /// </summary>
        public int[] IsdnDownloadInterval { get; set; }

        /// <summary>
        /// The download delay between requests on ADSL.
        /// </summary>
        public int[] AdslDownloadInterval { get; set; }

        /// <summary>
        /// The download delay between requests on Broadband.
        /// </summary>
        public int[] BroadbandDownloadInterval { get; set; }

        /// <summary>
        /// A 48 element long array, depicting 30 minute segments on a 24-hour clock, when downloading is allowed and when not.
        /// </summary>
        public CloudDownload[] CloudDownloadAllowed { get; set; } = new CloudDownload[48];

        /// <summary>
        /// URL receiving download status reports.
        /// </summary>
        public string ReportUrl { get; set; }

        /// <summary>
        /// Interval in seconds until a download status report is sent.
        /// </summary>
        public int ReportInterval { get; set; }

        /// <summary>
        /// Unknown use.
        /// </summary>
        public string GameDescription { get; set; }

        /// <summary>
        /// If multiple .opt files are present, whether they should be installed one-by-one in sequential order, or all at the same time whenever they are downloaded.
        /// This must be null on App DLIs.
        /// </summary>
        public OptReleaseType? ReleaseType { get; set; }

        /// <summary>
        /// URLs which have files to be downloaded and installed.
        /// </summary>
        public string[] InstallUrls { get; set; }

        /// <summary>
        /// URLs to files which must already exist.
        /// </summary>
        public string[] ExistUrls { get; set; }

        /// <summary>
        /// Integrity hash of the given URLs.
        /// </summary>
        public uint DownloadId { get; set; }

        /// <summary>
        /// Whether to immediately install the downloaded files (true), or wait until next reboot (false).
        /// Requires DLFORMAT 4.20+.
        /// </summary>
        public bool ImmediatelyRelease { get; set; }

        /// <summary>
        /// Whether to install options at the same time with app files (true), or seperately (false).
        /// Requires DLFORMAT 5.00+
        /// </summary>
        public bool ReleaseWithOption { get; set; }

        /// <summary>
        /// URLs for LAN distribution server.
        /// Requires DLFORMAT 5.00+
        /// </summary>
        public string[] PrivateInstallUrls { get; set; }
    }

    /// <summary>
    /// Option Image URL. Unknown if this is used anywhere. May be null if this is not an option DLI.
    /// </summary>
    public class OptionImage {
        /// <summary>
        /// Unknown.
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// Option data. Contains option information and download URLs. May be null if this is not an option DLI.
    /// </summary>
    public class Optional {
        /// <summary>
        /// Unknown.
        /// </summary>
        public bool IgnoreReleaseTime { get; set; }

        /// <summary>
        /// URLs which have files to be downloaded and installed.
        /// </summary>
        public string[] InstallUrls { get; set; }

        /// <summary>
        /// URLs for LAN distribution server.
        /// Requires DLFORMAT 5.00+
        /// </summary>
        public string[] PrivateInstallUrls { get; set; }
    }

    /// <summary>
    /// Foreground download settings. Unknown if this is used anywhere. May be null.
    /// Requires DLFORMAT 5.00+.
    /// </summary>
    public class ForegroundInfo {
        /// <summary>
        /// The chunk size for which HTTP requests are sent. This array must have exactly 3 elements and values must be a power of 2.
        /// </summary>
        public uint PartSize { get; set; }

        /// <summary>
        /// The download delay between requests.
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Unknown. Not spotted in the wild yet.
        /// </summary>
        public string[] ImageUrls { get; set; }

        /// <summary>
        /// The timestamp when the contents in this DLI should be downloaded.
        /// </summary>
        public DateTime OrderTime { get; set; }

        /// <summary>
        /// Whether or not "installation has to be confirmed on the UI".
        /// </summary>
        public bool ReleaseConfirm { get; set; }
    }

    /// <summary>
    /// States for allowing downloads or not to a given time segment.
    /// <seealso cref="DownloadInstructionFile.CommonInfo.CloudDownloadAllowed"/>
    /// </summary>
    public enum CloudDownload {
        /// <summary>
        /// Download is allowed.
        /// </summary>
        Allowed = 0,

        /// <summary>
        /// Download is not allowed.
        /// </summary>
        NotAllowed = 1,

        /// <summary>
        /// Unknown value. (As in it's unknown what this value (2) does)
        /// </summary>
        UnknownValue = 2
    }
}

/// <summary>
/// Type of a DLI file.
/// </summary>
public enum DliType {
    /// <summary>
    /// This DLI file is for option data.
    /// </summary>
    Opt = 0,

    /// <summary>
    /// This DLI file is for app data.
    /// </summary>
    App = 1
}

/// <summary>
/// Values for <see cref="DownloadInstructionFile.CommonInfo.ReleaseType"/>
/// </summary>
public enum OptReleaseType {
    /// <summary>
    /// If even one of the files specified in the instructions has been downloaded, the file(s) will be installed immediately.
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Installation will only occur when all files have been downloaded.
    /// </summary>
    Bulk = 1,

    /// <summary>
    /// Maximum value.
    /// </summary>
    Max = 1
}