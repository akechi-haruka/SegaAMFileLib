using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

public class ApmOptFile : AppFile {
    private static readonly ILogger LOG = Log.GetOrCreate("AOpt");

    public new ApmOptFile Parent { get; }

    public ApmOptFile(Stream data, ApmOptFile parent = null) : base(data, parent) {
        Parent = parent;
    }

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