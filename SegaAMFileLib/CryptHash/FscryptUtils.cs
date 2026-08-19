using System.Text;
using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

static class FscryptUtils {
    internal static void CalculatePageIv(ulong fileOffset, byte[] fileIv, ref byte[] pageIv) {
        for (int i = 0; i < fileIv.Length && i < pageIv.Length; i++) {
            pageIv[i] = (byte)(fileIv[i] ^ (fileOffset >> (8 * (i % 8))));
        }
    }

    internal static byte[] CalculateFileIv(byte[] key, byte[] expectedHeader, byte[] firstBytes) {
        byte[] iv = new byte[16];
        byte[] header = new byte[16];

        Array.Copy(firstBytes, header, 16);
        CalculatePageIv(0, expectedHeader, ref iv);
        return Aes128Cbc.Decrypt(header, key, iv);
    }

    internal static EncryptionParameters CalculateApmEncryptionParameters(String gameId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        if (!GameID.IsValid(gameId)) {
            throw new ArgumentException("Invalid game id: " + gameId);
        }

        byte[] gameIdBytes = Encoding.ASCII.GetBytes(gameId);

        EncryptionParameters apm = EncryptionEnvironment.Apm;
        byte[] data = Aes128Cbc.Decrypt(EncryptionEnvironment.ApmSecondaryEncryptionData, apm.Key, apm.Iv);

        byte[] dataKey = new byte[16];
        byte[] dataIv = new byte[16];
        byte[] data2 = new byte[32];

        Array.Copy(data, dataKey, dataKey.Length);
        Array.Copy(data, 16, dataIv, 0, dataIv.Length);
        Array.Copy(data, 64, data2, 0, data2.Length);

        byte[] data3 = Aes128Cbc.Encrypt(data2, dataKey, dataIv);
        byte[] data3Key = new byte[16];
        byte[] data3Iv = new byte[16];

        Array.Copy(data3, 0, data3Key, 0, data3Key.Length);
        Array.Copy(data3, 16, data3Iv, 0, data3Iv.Length);

        for (int i = 0; i < data3Key.Length; i++) {
            data3Key[i] ^= gameIdBytes[i % 4];
            data3Iv[i] ^= data3Iv[i % 4];
        }

        return new EncryptionParameters(data3Key, data3Iv);
    }
}