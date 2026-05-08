namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

class AppFsEncryption {
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
}