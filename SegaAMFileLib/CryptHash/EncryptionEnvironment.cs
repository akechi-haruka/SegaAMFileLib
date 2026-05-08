using System.Collections.Immutable;
using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

/// <summary>
/// Defines environment specific encryption parameters for other SegaAMFileLib functions, such as AES keys/IVs.
/// </summary>
public class EncryptionEnvironment {
    /// <summary>
    /// Encryption parameters for the bootid header in .app/.opt/.pack files.
    /// </summary>
    public static EncryptionParameters BootId { get; private set; }

    /// <summary>
    /// Encryption parameters for .icf files.
    /// </summary>
    public static EncryptionParameters Icf { get; private set; }

    /// <summary>
    /// Encryption parameters for .opt files.
    /// </summary>
    public static EncryptionParameters Option { get; private set; }

    /// <summary>
    /// Game specific encryption parameters, where the key
    /// </summary>
    public static ImmutableDictionary<string, EncryptionParameters> Games { get; private set; }

    /// <summary>
    /// Loads the encryption environment .ini file from a file.
    /// The file is expected to be in the standard .ini format.
    /// </summary>
    /// <param name="filename">The file to read (usually keys.txt)</param>
    public static void Initialize(string filename) {
        try {
            IniParser keylist = new IniParser(File.ReadAllLines(filename));

            BootId = new EncryptionParameters(keylist, "BootId");
            Icf = new EncryptionParameters(keylist, "ICF");
            Option = new EncryptionParameters(keylist, "Option");

            Games = ImmutableDictionary.CreateRange(keylist.GetSections().ToDictionary(section => section, section => new EncryptionParameters(keylist, section)));
        } catch (Exception ex) {
            throw new IOException("Failed to read key file from " + filename, ex);
        }
    }

    public static EncryptionParameters GetGame(string appId) {
        if (!Games.TryGetValue(appId, out EncryptionParameters value)) {
            throw new ArgumentException("No decryption key exists for app " + appId);
        }

        return value;
    }
}

public class EncryptionParameters {
    public byte[] Key { get; }
    public byte[] Iv { get; }

    internal EncryptionParameters(IniParser keylist, string section) {
        Key = ConvertKey(keylist.GetSetting(section, "Key"));
        Iv = ConvertKey(keylist.GetSetting(section, "IV"));
    }

    private static byte[] ConvertKey(string str) {
        if (String.IsNullOrWhiteSpace(str)) {
            return Array.Empty<byte>();
        }

        return Hex.From(str);
    }
}