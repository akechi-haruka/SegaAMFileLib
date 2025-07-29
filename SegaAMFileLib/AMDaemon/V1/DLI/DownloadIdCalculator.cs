using System.Text;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.DLI;

/// <summary>
/// Helper class to calculate the DOWNLOAD_ID field in a DLI.
/// </summary>
public static class DownloadIdCalculator {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(DownloadIdCalculator));

    /// <summary>
    /// Calculates the DOWNLOAD_ID for the given download paths.
    /// </summary>
    /// <param name="filenames">The list of .app/.opt/.pack files that are supposed to be downloaded.</param>
    /// <returns>The value for DOWNLOAD_ID.</returns>
    /// <exception cref="ArgumentException">If <see cref="filenames"/> is null or any of it's values are null.</exception>
    public static uint GetDownloadId(params String[] filenames) {
        ArgumentNullException.ThrowIfNull(filenames);
        foreach (string t in filenames) {
            ArgumentNullException.ThrowIfNull(t);
        }

        uint ret = 0;
        foreach (string file in filenames) {
            uint c = SegaCrc32.CalcCrc32(Encoding.Unicode.GetBytes(file).ToArray());
            LOG.LogTrace("Adding to CRC: {f} ({c}, {x})", file, c, c.ToString("X8"));
            ret ^= c;
        }

        LOG.LogTrace("Final CRC: {ret}", ret);
        return ret;
    }

    public static uint GetDownloadId(DownloadInstructionFile dli) {
        return GetDownloadId(dli.Common.InstallUrls.Select(url => "0" + url.Split("/").Last())
            .Concat(dli.Common.ExistUrls.Select(url => "1" + url.Split("/").Last()))
            .Concat(dli.Common.PrivateInstallUrls.Select(url => "2" + url.Split("/").Last()))
            .Concat(dli.Option?.InstallUrls?.Select(url => "2" + url.Split("/").Last()) ?? Array.Empty<string>())
            .Concat(dli.Option?.PrivateInstallUrls?.Select(url => "3" + url.Split("/").Last()) ?? Array.Empty<string>())
            .ToArray());
    }
}