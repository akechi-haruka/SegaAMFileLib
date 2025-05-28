using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash {
    public static class SegaCrc32 {
        private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(SegaCrc32));

        public static uint CalcCrc32(byte[] data) {
            return new Crc32Managed().GetCrc32(data);
        }

        public static byte[] WriteCrcIntoFirst4Bytes(byte[] struc) {
            byte[] crcableBytes = new byte[struc.Length - 4];
            Array.Copy(struc, 0 + sizeof(uint), crcableBytes, 0, struc.Length - sizeof(uint));
            
            uint crcnum = CalcCrc32(crcableBytes);
            byte[] crc = BitConverter.GetBytes(crcnum);
            
            Array.Copy(crc, 0, struc, 0, crc.Length);
            
            LOG.LogDebug("New CRC for given data: 0x" + crcnum.ToString("X2"));

            return struc;
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