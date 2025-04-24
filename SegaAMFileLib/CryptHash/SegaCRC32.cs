namespace Haruka.Arcade.SegaAMFileLib.CryptHash {
    public class SegaCrc32 {
        public static uint CalcCrc32(byte[] data) {
            return new Crc32Managed().GetCrc32(data);
        }
    }

    class Crc32Managed {
        private static uint[] table;

        public uint GetCrc32(byte[] data) {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < data.Length; i++) {
                byte index = (byte)(((crc) & 0xFF) ^ data[i]);
                crc = (uint)((crc >> 8) ^ table[index]);
            }

            return ~crc;
        }

        public Crc32Managed() {
            const uint poly = 0xEDB88320;
            table = new uint[256];
            for (uint i = 0; i < table.Length; i++) {
                uint temp = i;
                for (int j = 8; j > 0; j--) {
                    if ((temp & 1) == 1) {
                        temp = (uint)((temp >> 1) ^ poly);
                    } else {
                        temp >>= 1;
                    }
                }

                table[i] = temp;
            }
        }
    }
}