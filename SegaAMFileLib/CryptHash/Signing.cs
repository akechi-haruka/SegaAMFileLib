using System.Security.Cryptography;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

public static class Signing {
    public static byte[] Sign(byte[] data, String key) {
        using (RSA rsa = RSA.Create()) {
            rsa.ImportFromPem("-----BEGIN PUBLIC KEY-----\n" + key + "\n-----END PUBLIC KEY-----");
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
        }
    }

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