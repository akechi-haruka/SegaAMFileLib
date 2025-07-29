using Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash {
    
    /// <summary>
    /// Implementation of the CRC32 algorithm that SEGA uses.
    /// </summary>
    public static class SegaCrc32 {
        private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(SegaCrc32));

        private static readonly Crc32Managed INSTANCE = new Crc32Managed(); 
        
        /// <summary>
        /// Calculates the CRC32 for the given byte array. Remember that if the CRC field is part of this array, it should be zeroed out.
        /// </summary>
        /// <param name="data">The byte array to use.</param>
        /// <returns>The computed CRC32 checksum.</returns>
        public static uint CalcCrc32(byte[] data) {
            return INSTANCE.GetCrc32(data);
        }

        /// <summary>
        /// Calculates the CRC32 for the given byte array, and writes it into the first 4 bytes of the given byte array.
        /// </summary>
        /// <param name="struc"></param>
        /// <returns></returns>
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
                crc = (crc >> 8) ^ table[index];
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
                        temp = (temp >> 1) ^ poly;
                    } else {
                        temp >>= 1;
                    }
                }

                table[i] = temp;
            }
        }
    }
}