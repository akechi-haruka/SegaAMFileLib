using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

/// <summary>
/// Defines environment specific encryption parameters for other SegaAMFileLib functions, such as AES keys/IVs.
/// </summary>
public static class EncryptionEnvironment {
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
    /// Encryption parameters for APM .opt files.
    /// </summary>
    public static EncryptionParameters Apm { get; private set; }

    /// <summary>
    /// Secondary encryption data specific to APM .opt files.
    /// </summary>
    public static byte[] ApmSecondaryEncryptionData { get; private set; }

    /// <summary>
    /// The HMAC key used for verifying fscrypt containers.
    /// </summary>
    public static byte[] BootIdHmac { get; private set; }

    private static Dictionary<string, EncryptionParameters> Games { get; set; }

    /// <summary>
    /// Loads the encryption environment .ini file from a file.
    /// The file is expected to be in the standard .ini format.
    /// </summary>
    /// <param name="filename">The file to read (usually keys.txt)</param>
    public static void Initialize(string filename) {
        try {
            if (String.IsNullOrWhiteSpace(filename) || !File.Exists(filename)) {
                DirectoryInfo di = Directory.GetParent(typeof(EncryptionEnvironment).Assembly.Location);
                if (di == null) {
                    throw new Exception("Could not find parent path of current assembly directory");
                }

                FileInfo fi = new FileInfo(Path.Combine(di.FullName, "keys.txt"));
                if (!fi.Exists) {
                    throw new FileNotFoundException("keys.txt was not found");
                }

                filename = fi.FullName;
            }

            IniParser keylist = new IniParser(File.ReadAllLines(filename));

            BootId = new EncryptionParameters(keylist, "BootId");
            Icf = new EncryptionParameters(keylist, "ICF");
            Option = new EncryptionParameters(keylist, "Option");
            Apm = new EncryptionParameters(keylist, "APM");
            ApmSecondaryEncryptionData = EncryptionParameters.ConvertKey(keylist.GetSetting("APM", "SecondaryEncryptionData"));
            BootIdHmac = EncryptionParameters.ConvertKey(keylist.GetSetting("BootId", "Hmac"));
            Games = keylist.GetSections().ToDictionary(section => section, section => new EncryptionParameters(keylist, section));
        } catch (Exception ex) {
            throw new IOException("Failed to read key file from " + filename, ex);
        }
    }

    /// <summary>
    /// Returns the encryption parameters for the specified game.
    /// </summary>
    /// <param name="appId">The 4-letter game ID, or "----" for the system encryption parameters.</param>
    /// <returns>The <see cref="EncryptionParameters"/> for the given game.</returns>
    /// <exception cref="ArgumentException">if no decryption key exists for the given game.</exception>
    public static EncryptionParameters GetGame(string appId) {
        if (appId == GameID.SYSTEM_APP_ID) {
            appId = "ACA";
        }

        if (!Games.TryGetValue(appId, out EncryptionParameters value)) {
            throw new ArgumentException("No decryption key exists for app " + appId);
        }

        return value;
    }

    internal static void SetEncryptionParametersForGame(String appId, byte[] key, byte[] iv) {
        Games[appId] = new EncryptionParameters(key, iv);
    }
}

/// <summary>
/// Container that bundles an encryption key and IV.
/// </summary>
public class EncryptionParameters {
    /// <summary>
    /// The encryption key.
    /// </summary>
    public byte[] Key { get; }

    /// <summary>
    /// The initialization vector.
    /// </summary>
    public byte[] Iv { get; }

    internal EncryptionParameters(byte[] key, byte[] iv) {
        Key = key;
        Iv = iv;
    }

    internal EncryptionParameters(IniParser keylist, string section) {
        Key = ConvertKey(keylist.GetSetting(section, "Key"));
        Iv = ConvertKey(keylist.GetSetting(section, "IV"));
    }

    internal static byte[] ConvertKey(string str) {
        if (String.IsNullOrWhiteSpace(str)) {
            return Array.Empty<byte>();
        }

        return Hex.From(str);
    }
}