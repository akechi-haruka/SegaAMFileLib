using System.Security.Cryptography;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

/// <summary>
/// Methods used to sign and/or hash data related to SEGA files.
/// </summary>
public static class Signing {
    /// <summary>
    /// Creates a HMAC-SHA1 hash from the given data.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="key">The key to use.</param>
    /// <returns>The calculated hash.</returns>
    public static byte[] Hash(byte[] data, byte[] key) {
        using (HMACSHA1 hmac = new HMACSHA1(key)) {
            for (int i = 0; i < data.Length; i += FscryptStream.PAGE_SIZE) {
                int toRead = Math.Min(FscryptStream.PAGE_SIZE, data.Length - i);
                hmac.TransformBlock(data, i, toRead, null, 0);
            }

            hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return hmac.Hash;
        }
    }
}