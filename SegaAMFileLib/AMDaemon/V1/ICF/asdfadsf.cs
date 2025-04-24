#if false
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Haruka.Arcade.SegaAMFileLib.CryptHash;

namespace ICFReader {
    public static class Program3 {

        static String lastError;
        static List<string> dataList;
        static DateTime datetime, appdate;
        static Version sysver, appver;
        static string appid, platid;
        static byte platgen;


        public static void Mai2n(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Usage: icfedit <file> <view|edit <major=X, minor=X, build=X, gameid=X, platform=X> [output]>");
                return;
            }
            string file = args[0];
            if (!File.Exists(file)) {
                Console.WriteLine("File not found: " + file);
            }
            Console.WriteLine("--- " + file + " ---");
            if (args[1] == "view") {
                if (ReadStorageInfo(file)) {
                    Console.WriteLine("Game ID: " + appid);
                    Console.WriteLine("Game Version: " + appver);
                    Console.WriteLine("Platform ID: " + appid);
                    Console.WriteLine("System Version: " + sysver);
                    Console.WriteLine("Platform \"Generation\": " + platgen);
                    Console.WriteLine("System Date: " + datetime);
                    Console.WriteLine("Game Date: " + appdate);

                } else {
                    Console.WriteLine("failed to read file: " + lastError);
                }

            } else if (args[1] == "edit") {

                String major = null;
                String minor = null;
                String build = null;
                String gameid = null;
                String platid = null;
                String outfile = file;

                for (int i = 2; i < args.Length; i++) {
                    var c = args[i];
                    if (c.StartsWith("major=")) {
                        major = c.Substring(6);
                    } else if (c.StartsWith("minor=")) {
                        minor = c.Substring(6);
                    } else if (c.StartsWith("build=")) {
                        build = c.Substring(6);
                    } else if (c.StartsWith("gameid=")) {
                        gameid = c.Substring(7);
                    } else if (c.StartsWith("platform=")) {
                        platid = c.Substring(9);
                    } else {
                        outfile = c;
                    }
                }

                if (WriteStorageInfo(file, major, minor, build, gameid, platid)) {
                    Console.WriteLine("OK");
                } else {
                    Console.WriteLine("failed to read/write file: " + lastError);
                }

            }
        }

        private static bool ReadStorageInfo(string path) {
            var encoding = Encoding.UTF8;
            short vmajor; byte vminor, vbuild; // Version
            short yy, mm, dd, hh, mi, ss; // Date
            datetime = DateTime.MinValue;
            appdate = DateTime.MinValue;
            sysver = new Version();
            appver = new Version();
            var vzero = new Version(0, 0, 0);

            if (!File.Exists(path)) {
                lastError = "File Not Found";
                return false;
            }

            var decrypted = Decrypt.DecryptICF(path);
            if (decrypted == null) { lastError = "Invalid ICF file"; return false; }

            var data = new byte[decrypted.Length - 4];
            Array.Copy(decrypted, 4, data, 0, data.Length);
            var dtc1 = SegaCRC32.CRC32(data);

            using (var rd = new BinaryReader(new MemoryStream(decrypted))) {
                // Check Main CRC32
                var crc1 = rd.ReadUInt32();
                if (crc1 != dtc1) { lastError = "Main Data Error"; return false; }
                // Check Size
                var size = rd.ReadUInt32();
                if (size != decrypted.Length) { lastError = "Data Size Error"; return false; }
                // Check padding
                var padding = rd.ReadUInt64();
                if (padding != 0) { lastError = "Data Padding Error"; return false; }

                // Entry count
                var count = (int)rd.ReadUInt64();
                Console.WriteLine("Entry Count = " + count);
                var expsz = 0x40 * (count + 1);
                if (expsz != decrypted.Length) { lastError = "Data Info Error"; return false; }
                // Read App Id
                appid = encoding.GetString(rd.ReadBytes(4));
                // Read Platform Id
                platid = encoding.GetString(rd.ReadBytes(3));
                // Read Platform Generation
                platgen = rd.ReadByte();

                // Check Sub CRC32
                var crc2 = rd.ReadUInt32();
                uint dtc2 = 0;
                for (int i = 1; i <= count; i++) {
                    data = new byte[0x40];
                    Array.Copy(decrypted, 0x40 * i, data, 0, data.Length);
                    if (data[0] == 2 && data[1] == 1) dtc2 ^= SegaCRC32.CRC32(data);
                }
                if (crc2 != dtc2) { lastError = "Sub Data Error"; return false; }
                // Check padding
                for (int i = 0; i < 7; i++) {
                    padding = rd.ReadUInt32();
                    if (padding != 0) { lastError = "Data Padding Error"; return false; }
                }

                // Begin Parse Data
                dataList = new List<string>();
                for (int c = 0; c < count; c++) {
                    // Begin Entry
                    data = rd.ReadBytes(4);
                    // Part Start
                    var enabled = (data[0] == 2 && data[1] == 1);
                    // Part Type
                    // 00 00 : System, 01 00 : Main , 01 01 : Patch , 02 00 : Option
                    var type = rd.ReadUInt32();
                    // Check padding
                    for (int i = 0; i < 3; i++) {
                        padding = rd.ReadUInt64();
                        if (padding != 0) { lastError = "Data Padding Error"; return false; }
                    }
                    switch (type) {
                        case 0x0000: // SYSTEM
                            vbuild = rd.ReadByte();
                            vminor = rd.ReadByte();
                            vmajor = rd.ReadInt16();
                            sysver = new Version(vmajor, vminor, vbuild);
                            yy = rd.ReadInt16();
                            mm = rd.ReadByte();
                            dd = rd.ReadByte();
                            hh = rd.ReadByte();
                            mi = rd.ReadByte();
                            ss = rd.ReadByte();
                            rd.ReadByte(); // ms, not use
                            datetime = new DateTime(yy, mm, dd, hh, mi, ss);
                            // Check SystemVersion Requirement
                            vbuild = rd.ReadByte();
                            vminor = rd.ReadByte();
                            vmajor = rd.ReadInt16();
                            var ver = new Version(vmajor, vminor, vbuild);
                            if (!ver.Equals(sysver)) { lastError = "System Version Error"; return false; }
                            // Check Padding
                            for (int i = 0; i < 2; i++) {
                                padding = rd.ReadUInt64();
                                if (padding != 0) { lastError = "Data Padding Error"; return false; }
                            }
                            dataList.Add(String.Format("{0}_{1:D4}.{2:D2}.{3:D2}_{4:yyyyMMddHHmmss}_0.pack", platid, sysver.Major, sysver.Minor, sysver.Build, datetime));
                            break;
                        case 0x0001: // APP
                            vbuild = rd.ReadByte();
                            vminor = rd.ReadByte();
                            vmajor = rd.ReadInt16();
                            appver = new Version(vmajor, vminor, vbuild);
                            yy = rd.ReadInt16();
                            mm = rd.ReadByte();
                            dd = rd.ReadByte();
                            hh = rd.ReadByte();
                            mi = rd.ReadByte();
                            ss = rd.ReadByte();
                            rd.ReadByte(); // ms, not use
                            appdate = new DateTime(yy, mm, dd, hh, mi, ss);
                            // Check SystemVersion Requirement
                            vbuild = rd.ReadByte();
                            vminor = rd.ReadByte();
                            vmajor = rd.ReadInt16();
                            ver = new Version(vmajor, vminor, vbuild);
                            if (!ver.Equals(sysver)) { lastError = "System Version Error"; return false; }
                            // Check Padding
                            for (int i = 0; i < 2; i++) {
                                padding = rd.ReadUInt64();
                                if (padding != 0) { lastError = "Data Padding Error"; return false; }
                            }
                            dataList.Add(String.Format("{0}_{1:D}.{2:D2}.{3:D2}_{4:yyyyMMddHHmmss}_0.app", appid, appver.Major, appver.Minor, appver.Build, appdate));
                            break;
                        case 0x0101: // PATCH
                            var vers = new List<Version>();
                            var dats = new List<DateTime>();
                            for (int i = 0; i < 2; i++) {
                                vbuild = rd.ReadByte();
                                vminor = rd.ReadByte();
                                vmajor = rd.ReadInt16();
                                vers.Add(new Version(vmajor, vminor, vbuild));
                                yy = rd.ReadInt16();
                                mm = rd.ReadByte();
                                dd = rd.ReadByte();
                                hh = rd.ReadByte();
                                mi = rd.ReadByte();
                                ss = rd.ReadByte();
                                rd.ReadByte(); // ms, not use
                                dats.Add(new DateTime(yy, mm, dd, hh, mi, ss));
                                // Check SystemVersion Requirement
                                vbuild = rd.ReadByte();
                                vminor = rd.ReadByte();
                                vmajor = rd.ReadInt16();
                                ver = new Version(vmajor, vminor, vbuild);
                                if (!ver.Equals(sysver) && !ver.Equals(vzero)) { lastError = "System Version Error"; return false; }
                            }
                            // Check Patch Info
                            if (!vers[1].Equals(appver)) { lastError = "Application Version Error"; return false; }
                            if (!dats[1].Equals(appdate)) { lastError = "Application Timestamp Error"; return false; }
                            dataList.Add(String.Format("{0}_{1:D}.{2:D2}.{3:D2}_{4:yyyyMMddHHmmss}_1_{5:D}.{6:D2}.{7:D2}.app", appid, vers[0].Major, vers[0].Minor, vers[0].Build, dats[0], vers[1].Major, vers[1].Minor, vers[1].Build));
                            break;
                        case 0x0002: // OPTION
                            var optid = encoding.GetString(rd.ReadBytes(4));
                            yy = rd.ReadInt16();
                            mm = rd.ReadByte();
                            dd = rd.ReadByte();
                            hh = rd.ReadByte();
                            mi = rd.ReadByte();
                            ss = rd.ReadByte();
                            rd.ReadByte(); // ms, not use
                            datetime = new DateTime(yy, mm, dd, hh, mi, ss);
                            // Check SystemVersion Requirement
                            vbuild = rd.ReadByte();
                            vminor = rd.ReadByte();
                            vmajor = rd.ReadInt16();
                            ver = new Version(vmajor, vminor, vbuild);
                            if (!ver.Equals(sysver) && !ver.Equals(vzero)) { lastError = "System Version Error"; return false; }
                            // Check Padding
                            for (int i = 0; i < 2; i++) {
                                padding = rd.ReadUInt64();
                                if (padding != 0) { lastError = "Data Padding Error"; return false; }
                            }
                            dataList.Add(String.Format("{0}_{1}_{2:yyyyMMddHHmmss}_0.opt", appid, optid, datetime));
                            break;
                        default:
                            rd.ReadBytes(0x20);
                            break;
                    }
                }
                // dataList.Sort();
                return true;
            }
        }

        private static bool WriteStorageInfo(string path, String major, String minor, String patch, String gameid, String platid) {
            var encoding = Encoding.UTF8;

            if (!File.Exists(path)) {
                lastError = "File Not Found";
                return false;
            }

            var decrypted = Decrypt.DecryptICF(path);
            if (decrypted == null) { lastError = "Invalid ICF file"; return false; }



            var data = new byte[decrypted.Length - 4];
            Array.Copy(decrypted, 4, data, 0, data.Length);
            var dtc1 = SegaCRC32.CRC32(data);

            var output = new byte[decrypted.Length];


            using (var rd = new BinaryReader(new MemoryStream(decrypted))) {
                var wr = new BinaryWriter(new MemoryStream());
                wr.Write((UInt32)0); // checksum
                wr.Write(rd.ReadUInt32()); // size;
                ulong padding = rd.ReadUInt64();
                wr.Write(padding); // padding
                Console.WriteLine("padding = " + padding);
                ulong count = rd.ReadUInt64();
                Console.WriteLine("count = " + count);
                wr.Write(count); // count
                String icf_gameid = encoding.GetString(rd.ReadBytes(4));
                if (gameid != null) {
                    icf_gameid = gameid;
                }
                wr.Write(encoding.GetBytes(icf_gameid));
                Console.WriteLine("icf_gameid = " + icf_gameid);
                String icf_platid = encoding.GetString(rd.ReadBytes(3));
                if (platid != null) {
                    icf_platid = platid;
                }
                wr.Write(encoding.GetBytes(icf_platid));
                Console.WriteLine("icf_platid = " + icf_platid);
                wr.Write(rd.ReadByte()); // generation
                wr.Write(rd.ReadUInt32()); // checksum
                for (int i = 0; i < 7; i++) { // padding
                    wr.Write(rd.ReadUInt32());
                }
                Console.WriteLine("count = " + count);
                for (ulong c = 0; c < count; c++) {
                    wr.Write(rd.ReadBytes(4)); // ???
                    uint type = rd.ReadUInt32();
                    wr.Write(type); // type
                    for (int i = 0; i < 3; i++) { // padding
                        wr.Write(rd.ReadUInt64());
                    }
                    byte icf_build = rd.ReadByte();
                    byte icf_minor = rd.ReadByte();
                    short icf_major = rd.ReadInt16();
                    if (major != null) {
                        icf_major = short.Parse(major);
                    }
                    if (minor != null) {
                        icf_minor = byte.Parse(minor);
                    }
                    if (patch != null) {
                        icf_build = byte.Parse(patch);
                    }
                    wr.Write(icf_build);
                    wr.Write(icf_minor);
                    wr.Write(icf_major);
                    wr.Write(rd.ReadInt16()); // year
                    wr.Write(rd.ReadByte()); // month
                    wr.Write(rd.ReadByte()); // day
                    wr.Write(rd.ReadByte()); // hour
                    wr.Write(rd.ReadByte()); // minute
                    wr.Write(rd.ReadByte()); // second
                    wr.Write(rd.ReadByte()); // ??
                    wr.Write(icf_build);
                    wr.Write(icf_minor);
                    wr.Write(icf_major);
                    for (int i = 0; i < 2; i++) { // padding
                        wr.Write(rd.ReadUInt64());
                    }
                }
                wr.Close();
            }

            Array.Copy(BitConverter.GetBytes(SegaCRC32.CRC32(data)), 0, output, 0, 4);

            byte[] enc = Decrypt.EncryptICF(data);

            File.WriteAllBytes(path, enc);

            return true;
        }


    }
}
#endif