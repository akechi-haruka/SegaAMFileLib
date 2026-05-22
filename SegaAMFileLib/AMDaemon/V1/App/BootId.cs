using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct BootId {
    public const string SIGNATURE = "BTID";
    public const int SIZE = 0x2800;
    public const long NORMAL_BLOCK_SIZE = 0x40000;

    public uint crc;
    public uint length;
    public fixed byte signature[4];
    private fixed byte padding[1];
    public InstallFile.FileType containerType;
    public byte sequenceNumber;
    public byte useCustomIV;
    public fixed byte appId[4];
    public Timestamp gameTimestamp;
    public Version gameVersion;
    public ulong blockCount;
    public ulong blockSize;
    public ulong headerBlockCount;
    private fixed byte padding2[8];
    public fixed byte platformId[3];
    public byte platformGeneration;
    public Timestamp sourceTimestamp;
    public Version sourceVersion;
    public Version platformVersion;
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

    public void Verify() {
        if (length != SIZE) {
            throw new ArgumentException("BootId has invalid length: " + length);
        }

        if (containerType > InstallFile.FileType.Max) {
            throw new ArgumentException("BootId has invalid container type: " + containerType);
        }

        string appIdString = GetAppId();
        if (!GameID.IsValid(appIdString)) {
            throw new ArgumentException("BootId has invalid app ID: " + appIdString);
        }
    }

    public long GetFileSystemSize() {
        return (long)((blockCount - headerBlockCount) * blockSize);
    }

    public long GetHeaderSize() {
        return (long)(headerBlockCount * blockSize);
    }

    public ulong GetFullContainerSize() {
        return blockCount * blockSize;
    }

    public long GetOffsetOfFileSystem() {
        return (long)(headerBlockCount * blockSize);
    }

    public override string ToString() {
        return GetPlatformId() + ":" + GetAppId() + " ver. " + gameVersion + " - " + gameTimestamp;
    }

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

    public bool IsApmOption() {
        return containerType == InstallFile.FileType.Option && GameID.IsApm(GetAppId());
    }

    public String DumpStrings() {
        byte[] buf = new byte[10156];
        fixed (byte* ptr = platformId) {
            StructUtils.Copy(ptr, buf, buf.Length);
        }

        return Hex.Dump(buf);
    }
}