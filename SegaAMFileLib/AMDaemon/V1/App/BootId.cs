using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.App;

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct BootId {
    public uint crc;
    public uint length;
    public fixed byte signature[4];
    public fixed byte padding[1];
    public ContainerType containerType;
    public byte sequenceNumber;
    public byte useCustomIV;
    public fixed byte appId[4];
    public Timestamp gameTimestamp;
    public Version gameVersion;
    public ulong blockCount;
    public ulong blockSize;
    public ulong headerBlockCount;
    public fixed byte padding2[8];
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
        // TODO: crc

        if (containerType > ContainerType.Max) {
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

    public long GetOffsetOfFileSystem() {
        return (long)(headerBlockCount * blockSize);
    }
}

public enum ContainerType : byte {
    Pack = 0x00,
    App = 0x01,
    Option = 0x02,
    Max = 0x02
}