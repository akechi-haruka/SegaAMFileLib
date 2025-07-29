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

    private static uint GetDownloadId(params String[] filenames) {
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

    /// <summary>
    /// Calculates the download ID for the given DLI. This is based on EXIST##, INSTALL## and PRIVATE_INSTALL##.
    /// </summary>
    /// <param name="dli">The DLI to use.</param>
    /// <seealso cref="DownloadInstructionFile.CommonInfo.DownloadId"/>
    /// <returns>The value for DOWNLOAD_ID.</returns>
    public static uint GetDownloadId(DownloadInstructionFile dli) {
        ArgumentNullException.ThrowIfNull(dli);
        return GetDownloadId(dli.Common.InstallUrls.Select(url => "0" + url.Split("/").Last())
            .Concat(dli.Common.ExistUrls.Select(url => "1" + url.Split("/").Last()))
            .Concat(dli.Common.PrivateInstallUrls.Select(url => "2" + url.Split("/").Last()))
            .Concat(dli.Option?.InstallUrls?.Select(url => "2" + url.Split("/").Last()) ?? Array.Empty<string>())
            .Concat(dli.Option?.PrivateInstallUrls?.Select(url => "3" + url.Split("/").Last()) ?? Array.Empty<string>())
            .ToArray());
    }
}