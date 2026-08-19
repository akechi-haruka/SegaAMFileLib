using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

/// <summary>
/// .opt file container contained inside regular .opt files which is specific to APM v3 games (next to app.json, movie.wmv, etc.). In reality, the contents are equivalent to an .app container with the decryption key derived differently.
/// </summary>
/// <seealso cref="OptFile.OpenInnerApmOptFilesystem"/>
/// <seealso cref="OptFile.ExtractInnerApmTo"/>
public class ApmOptFile : AppFile {
    private static readonly ILogger LOG = Log.GetOrCreate("AOpt");

    /// <summary>
    /// The parent app container.
    /// </summary>
    public new ApmOptFile Parent { get; }

    /// <summary>
    /// Loads an .opt fscrypt container specific to APMv3 from a stream.
    /// </summary>
    /// <param name="data">The stream to read from</param>
    /// <param name="parent">The parent container in case this container is a patch of another container.</param>
    /// <exception cref="ArgumentNullException">if data is null</exception>
    /// <exception cref="ArgumentException">there given stream is invalid, the given file is not for APMv3</exception>
    /// <exception cref="IOException">error reading BootId or header data</exception>
    public ApmOptFile(Stream data, ApmOptFile parent = null) : base(data, parent, false) { // APM opts have no signature?
        Parent = parent;
    }

    /// <inheritdoc/>
    protected override void GenerateEncryptionKeys() {
        byte[] initialBytes = new byte[16];
        SourceStream.ReadExactly(initialBytes);
        SourceStream.Seek(-initialBytes.Length, SeekOrigin.Current);

        EncryptionParameters ep = FscryptUtils.CalculateApmEncryptionParameters(BootId.GetAppId());

        Key = ep.Key;
        Iv = FscryptUtils.CalculateFileIv(Key, NTFS_HEADER, initialBytes);

        LOG.LogInformation("APM3 opt Key was derived to be " + Hex.To(Key));
        LOG.LogInformation("APM3 opt IV was derived to be " + Hex.To(Iv));
    }
}