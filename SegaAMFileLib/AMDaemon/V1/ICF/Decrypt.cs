using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ICFReader {
	internal sealed class Decrypt {
		// Token: 0x06000010 RID: 16 RVA: 0x00002EE8 File Offset: 0x000010E8
		private static byte[] smethod_0_dec(byte[] array, byte[] byte_2, byte[] byte_3, int int_0 = 2097152) {
			int num = 4096;
			int num2 = array.Length;
			if (num2 < int_0) {
				return null;
			}
			byte[] array2 = new byte[num2 - int_0];
			using (RijndaelManaged rijndaelManaged = new RijndaelManaged {
				Mode = CipherMode.CBC,
				Padding = PaddingMode.None
			}) {
				for (int i = int_0; i < num2; i += num) {
					int num3 = i - int_0;
					int num4 = num2 - num3;
					if (num4 < num) {
						num = num4;
					}
					byte[] array3 = new byte[num];
					Array.Copy(array, i, array3, 0, array3.Length);
					using (MemoryStream memoryStream = new MemoryStream(array3)) {
						using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(byte_2, byte_3), CryptoStreamMode.Read)) {
							using (MemoryStream memoryStream2 = new MemoryStream()) {
								cryptoStream.CopyTo(memoryStream2);
								byte[] array4 = memoryStream2.ToArray();
								long value = BitConverter.ToInt64(array4, 0) ^ (long)num3;
								long value2 = BitConverter.ToInt64(array4, 8) ^ (long)num3;
								Array.Copy(BitConverter.GetBytes(value), 0, array2, num3, 8);
								Array.Copy(BitConverter.GetBytes(value2), 0, array2, num3 + 8, 8);
								Array.Copy(array4, 16, array2, num3 + 16, num - 16);
							}
						}
					}
				}
			}
			return array2;
		}

		private static byte[] smethod_0_enc(byte[] array, byte[] byte_2, byte[] byte_3, int int_0 = 2097152) {
			int num = 4096;
			int num2 = array.Length;
			if (num2 < int_0) {
				return null;
			}
			Console.WriteLine("encrypt len = " + array.Length);
			byte[] array2 = new byte[num2 - int_0];
			using (RijndaelManaged rijndaelManaged = new RijndaelManaged {
				Mode = CipherMode.CBC,
				Padding = PaddingMode.None
			}) {
				for (int i = int_0; i < num2; i += num) {
					int num3 = i - int_0;
					int num4 = num2 - num3;
					if (num4 < num) {
						num = num4;
					}
					byte[] array3 = new byte[num];
					Array.Copy(array, i, array3, 0, array3.Length);
					using (MemoryStream memoryStream = new MemoryStream(array3)) {
						using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateEncryptor(byte_2, byte_3), CryptoStreamMode.Read)) {
							using (MemoryStream memoryStream2 = new MemoryStream()) {
								cryptoStream.CopyTo(memoryStream2);
								byte[] array4 = memoryStream2.ToArray();
								long value = BitConverter.ToInt64(array4, 0) ^ (long)num3;
								long value2 = BitConverter.ToInt64(array4, 8) ^ (long)num3;
								Array.Copy(BitConverter.GetBytes(value), 0, array2, num3, 8);
								Array.Copy(BitConverter.GetBytes(value2), 0, array2, num3 + 8, 8);
								Array.Copy(array4, 16, array2, num3 + 16, num - 16);
							}
						}
					}
				}
			}
			return array2;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002119 File Offset: 0x00000319
		public static byte[] DecryptICF(string string_0) {
			byte[] array = File.ReadAllBytes(string_0);
			Console.WriteLine("File size = " + array.Length);
			return smethod_0_dec(array, byte_0, byte_1, 0);
		}

		public static byte[] EncryptICF(byte[] array) {
			return smethod_0_enc(array, byte_0, byte_1, 0);
		}

		// Token: 0x0400000A RID: 10
		private static readonly byte[] byte_0 = new byte[]
		{
			9,
			202,
			94,
			253,
			48,
			201,
			170,
			239,
			56,
			4,
			208,
			167,
			227,
			250,
			113,
			32
		};

		// Token: 0x0400000B RID: 11
		private static readonly byte[] byte_1 = new byte[]
		{
			177,
			85,
			194,
			44,
			46,
			127,
			4,
			145,
			250,
			127,
			15,
			220,
			33,
			122,
			byte.MaxValue,
			144
		};
	}
}
