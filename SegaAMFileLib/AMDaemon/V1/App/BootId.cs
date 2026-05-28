using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

/// <summary>
/// The header of a fscrypt container.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct BootId {
    /// <summary>
    /// The constant value of the <see cref="signature"/> field.
    /// </summary>
    public const string SIGNATURE = "BTID";

    /// <summary>
    /// The constant size of a BootId.
    /// </summary>
    public const int SIZE = 0x2800;

    /// <summary>
    /// The block size that is usually used.
    /// </summary>
    public const long NORMAL_BLOCK_SIZE = 0x40000;

    /// <summary>
    /// The CRC of the entire BootId minus this field.
    /// </summary>
    public uint crc;

    /// <summary>
    /// The length of the BootId. Always equal to <see cref="SIZE"/>.
    /// </summary>
    public uint length;

    /// <summary>
    /// The signature. Always equal to <see cref="SIGNATURE"/>.
    /// </summary>
    public fixed byte signature[4];

    /// <summary>
    /// Sometimes 0, most of the time 1.
    /// </summary>
    public byte unknown;

    /// <summary>
    /// The type of this container (app, opt, pack)
    /// </summary>
    public InstallFileName.FileType containerType;

    /// <summary>
    /// The sequence of this container. 0 is a base container, 1+ is an incremental one.
    /// </summary>
    public byte sequenceNumber;

    /// <summary>
    /// Do not set this.
    /// </summary>
    public byte useCustomIV;

    /// <summary>
    /// The 4-letter game ID this container is for.
    /// </summary>
    public fixed byte appId[4];

    /// <summary>
    /// The creation time of this container.
    /// </summary>
    public Timestamp gameTimestamp;

    /// <summary>
    /// The game version that is in this container.
    /// </summary>
    public Version gameVersion;

    /// <summary>
    /// The TOTAL number of blocks this container has.
    /// </summary>
    public ulong blockCount;

    /// <summary>
    /// The size of a block. Normally equal to <see cref="NORMAL_BLOCK_SIZE"/>.
    /// </summary>
    public ulong blockSize;

    /// <summary>
    /// The number of "header" blocks. Normally 8.
    /// </summary>
    public ulong headerBlockCount;

    /// <summary>
    /// Unknown.
    /// </summary>
    public ulong unknown2;

    /// <summary>
    /// The 3-letter platform ID of the system where this container can be installed on.
    /// </summary>
    public fixed byte platformId[3];

    /// <summary>
    /// The platform generation of the system where this container can be installed on.
    /// </summary>
    public byte platformGeneration;

    /// <summary>
    /// The parent container's creation date. Zeroed if this is not a patch. (<see cref="sequenceNumber"/> > 0)
    /// </summary>
    public Timestamp sourceTimestamp;

    /// <summary>
    /// The parent container's version. Zeroed if this is not a patch. (<see cref="sequenceNumber"/> > 0)
    /// </summary>
    public Version sourceVersion;

    /// <summary>
    /// The version of the system (pack) where this game can be installed on.
    /// </summary>
    public Version platformVersion;

    /// <summary>
    /// Unknown.
    /// </summary>
    public fixed byte strings[10156];

    /// <summary>
    /// Converts the app ID in this header record to a string. (ex. SDAA)
    /// </summary>
    /// <returns>the app ID in this header record to a string.</returns>
    public String GetAppId() {
        fixed (byte* ptr = appId) {
            return new String((sbyte*)ptr, 0, 4);
        }
    }

    /// <summary>
    /// Converts the platform ID in this header record to a string. (ex. AAV)
    /// </summary>
    /// <param name="withGeneration">If true, the <see cref="platformGeneration"/> will be appended to the string (ex. AAV1)</param>
    /// <returns>the platform ID in this header record to a string.</returns>
    public String GetPlatformId(bool withGeneration = true) {
        fixed (byte* ptr = platformId) {
            return new String((sbyte*)ptr, 0, 3) + (withGeneration ? platformGeneration : "");
        }
    }

    /// <summary>
    /// Verifies this BootId for correctness.
    /// </summary>
    /// <exception cref="ArgumentException">if a field is invalid</exception>
    public void Verify() {
        if (length != SIZE) {
            throw new ArgumentException("BootId has invalid length: " + length);
        }

        if (containerType > InstallFileName.FileType.Max) {
            throw new ArgumentException("BootId has invalid container type: " + containerType);
        }

        string appIdString = GetAppId();
        if (containerType == InstallFileName.FileType.Pack) {
            if (appIdString != GameID.SYSTEM_APP_ID) {
                throw new ArgumentException("BootId has invalid app ID for system file: " + appIdString);
            }
        } else if (!GameID.IsValid(appIdString)) {
            throw new ArgumentException("BootId has invalid app ID: " + appIdString);
        }
    }

    /// <summary>
    /// Returns the size in bytes that the contained (outer) filesystem has.
    /// </summary>
    /// <returns>the size in bytes that the contained (outer) filesystem has.</returns>
    public long GetFileSystemSize() {
        return (long)((blockCount - headerBlockCount) * blockSize);
    }

    /// <summary>
    /// Returns the size in bytes that the fscrypt header occupies (including the BootId)
    /// </summary>
    /// <returns>the size in bytes that the fscrypt header occupies (including the BootId)</returns>
    public long GetHeaderSize() {
        return (long)(headerBlockCount * blockSize);
    }

    /// <summary>
    /// Returns the size in bytes for the entire container, including all headers.
    /// </summary>
    /// <returns>the size in bytes for the entire container, including all headers.</returns>
    public ulong GetFullContainerSize() {
        return blockCount * blockSize;
    }

    /// <summary>
    /// Returns the byte offset where the outer filesystem begins.
    /// </summary>
    /// <returns>the byte offset where the outer filesystem begins.</returns>
    public long GetOffsetOfFileSystem() {
        return (long)(headerBlockCount * blockSize);
    }

    /// <inheritdoc/>
    public override string ToString() {
        return GetPlatformId() + ":" + GetAppId() + " ver. " + gameVersion + " - " + gameTimestamp;
    }

    /// <summary>
    /// Returns the signature in this BootId as a string.
    /// </summary>
    /// <returns>the signature in this BootId.</returns>
    public string GetSignature() {
        fixed (byte* ptr = signature) {
            return new String((sbyte*)ptr, 0, 4);
        }
    }

    /// <summary>
    /// Sets the app ID for this bootID.
    /// </summary>
    /// <param name="str">The new app ID.</param>
    public void SetAppId(String str) {
        fixed (byte* ptr = appId) {
            StructUtils.Copy(str, ptr, 4);
        }
    }

    /// <summary>
    /// Sets the signature for this bootID.
    /// </summary>
    /// <param name="str">The new signature.</param>
    public void SetSignature(String str = SIGNATURE) {
        fixed (byte* ptr = signature) {
            StructUtils.Copy(str, ptr, 4);
        }
    }

    /// <summary>
    /// Sets the platform ID for this bootID.
    /// </summary>
    /// <param name="str">The new platform ID.</param>
    public void SetPlatform(String str) {
        fixed (byte* ptr = platformId) {
            StructUtils.Copy(str, ptr, 3);
        }
    }

    /// <summary>
    /// Checks if this BootId is an option file that belongs to APM v3.
    /// </summary>
    /// <returns>true if this BootId is an option file that belongs to APM v3.</returns>
    public bool IsApmOption() {
        return containerType == InstallFileName.FileType.Option && GameID.IsApm(GetAppId());
    }

    /// <summary>
    /// Creates a hex dump of the unknown string table (<see cref="strings"/>.
    /// </summary>
    /// <returns>a hex dump of the strings</returns>
    public String DumpStrings() {
        byte[] buf = new byte[10156];
        fixed (byte* ptr = platformId) {
            StructUtils.Copy(ptr, buf, buf.Length);
        }

        return Hex.Dump(buf);
    }

    /// <summary>
    /// Creates a BootId from the given (encrypted) bytes.
    /// </summary>
    /// <param name="data">The bytes to read. These must be encrypted and exactly <see cref="SIZE"/> bytes long.</param>
    /// <returns>The decrypted BootId.</returns>
    /// <exception cref="ArgumentNullException">data is null</exception>
    /// <exception cref="ArgumentException">data length is invalid</exception>
    /// <exception cref="IOException">decryption failure, crc failure or read failure</exception>
    public static BootId FromEncryptedBytes(byte[] data) {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length != SIZE) {
            throw new ArgumentException("Invalid data length: bootId must be " + SIZE + " bytes, " + data.Length + " given");
        }

        data = Aes128Cbc.DecryptFromEnv(data, EncryptionEnvironment.BootId);

        BootId bootId = StructUtils.FromBytes<BootId>(data);

        uint crcExpected = bootId.crc;
        uint crcCalculated = SegaCrc32.CalcCrc32(data, 4);
        if (crcExpected != crcCalculated) {
            throw new IOException("CRC failure for BootID: Expected " + crcExpected + ", got " + crcCalculated);
        }

        bootId.Verify();

        return bootId;
    }
}