using System.Security.Cryptography;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

public static class SegaAes {
    public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv) {
        int blockSize = 4096;
        byte[] output = new byte[data.Length];

        Aes rijndaelManaged = Aes.Create();
        rijndaelManaged.Mode = CipherMode.CBC;
        rijndaelManaged.Padding = PaddingMode.None;

        for (int i = 0; i < data.Length; i += blockSize) {
            int remaining = data.Length - i;
            if (data.Length - i < blockSize) {
                blockSize = remaining;
            }

            byte[] blockArray = new byte[blockSize];
            Array.Copy(data, i, blockArray, 0, blockArray.Length);
            using (MemoryStream memoryStream = new MemoryStream(blockArray)) {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(key, iv), CryptoStreamMode.Read)) {
                    using (MemoryStream decryptStream = new MemoryStream()) {
                        cryptoStream.CopyTo(decryptStream);
                        byte[] blockDecryptedArray = decryptStream.ToArray();
                        long value = BitConverter.ToInt64(blockDecryptedArray, 0) ^ i;
                        long value2 = BitConverter.ToInt64(blockDecryptedArray, 8) ^ i;
                        Array.Copy(BitConverter.GetBytes(value), 0, output, i, 8);
                        Array.Copy(BitConverter.GetBytes(value2), 0, output, i + 8, 8);
                        Array.Copy(blockDecryptedArray, 16, output, i + 16, blockSize - 16);
                    }
                }
            }
        }

        return output;
    }

    public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv) {
        int blockSize = 4096;
        byte[] output = new byte[data.Length];

        Aes rijndaelManaged = Aes.Create();
        rijndaelManaged.Mode = CipherMode.CBC;
        rijndaelManaged.Padding = PaddingMode.None;
        for (int i = 0; i < data.Length; i += blockSize) {
            int remaining = data.Length - i;
            if (remaining < blockSize) {
                blockSize = remaining;
            }

            byte[] blockArray = new byte[blockSize];
            Array.Copy(data, i, blockArray, 0, blockArray.Length);
            using (MemoryStream memoryStream = new MemoryStream(blockArray)) {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateEncryptor(key, iv), CryptoStreamMode.Read)) {
                    using (MemoryStream decryptStream = new MemoryStream()) {
                        cryptoStream.CopyTo(decryptStream);
                        byte[] blockDecryptedArray = decryptStream.ToArray();
                        long value = BitConverter.ToInt64(blockDecryptedArray, 0) ^ i;
                        long value2 = BitConverter.ToInt64(blockDecryptedArray, 8) ^ i;
                        Array.Copy(BitConverter.GetBytes(value), 0, output, i, 8);
                        Array.Copy(BitConverter.GetBytes(value2), 0, output, i + 8, 8);
                        Array.Copy(blockDecryptedArray, 16, output, i + 16, blockSize - 16);
                    }
                }
            }
        }

        return output;
    }
}