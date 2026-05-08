using System.Security.Cryptography;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

/// <summary>
/// The normal AES-128-CBC algorithm.
/// </summary>
public static class Aes128Cbc {
    /// <summary>
    /// Decrypts the given byte array.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="param">The <see cref="EncryptionParameters"/> from the current <see cref="EncryptionEnvironment"/>.</param>
    /// <returns>The decrypted data (same length as input array)</returns>
    public static byte[] DecryptFromEnv(byte[] data, EncryptionParameters param) {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(param);
        return Decrypt(data, param.Key, param.Iv);
    }

    /// <summary>
    /// Decrypts the given byte array.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="key">The encryption key to use.</param>
    /// <param name="iv">The IV to use.</param>
    /// <returns>The decrypted data (same length as input array)</returns>
    public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv) {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);

        Aes rijndaelManaged = Aes.Create();
        rijndaelManaged.Mode = CipherMode.CBC;
        rijndaelManaged.Padding = PaddingMode.None;

        using (MemoryStream memoryStream = new MemoryStream(data)) {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(key, iv), CryptoStreamMode.Read)) {
                using (MemoryStream decryptStream = new MemoryStream()) {
                    cryptoStream.CopyTo(decryptStream);
                    return decryptStream.ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Encrypts the given byte array.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key to use.</param>
    /// <param name="iv">The IV to use.</param>
    /// <returns>The encrypted data (same length as input array)</returns>
    public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv) {
        Aes rijndaelManaged = Aes.Create();
        rijndaelManaged.Mode = CipherMode.CBC;
        rijndaelManaged.Padding = PaddingMode.None;

        using (MemoryStream memoryStream = new MemoryStream(data)) {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateEncryptor(key, iv), CryptoStreamMode.Read)) {
                using (MemoryStream encryptStream = new MemoryStream()) {
                    cryptoStream.CopyTo(encryptStream);
                    return encryptStream.ToArray();
                }
            }
        }
    }
}