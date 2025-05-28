using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ICFHeaderRecord {
    public uint mainCrc;
    public uint dataSize;
    private fixed byte padding[8];
    public ulong entryCount;
    public fixed byte appId[4];
    public fixed byte platformId[3];
    public byte platformGeneration;
    public uint entryCrc;
    private fixed byte padding_[28];

    public uint GetEntryCount() {
        return (uint)entryCount;
    }

    public String GetAppId() {
        fixed (byte* ptr = appId) {
            return new String((sbyte*)ptr, 0, 4);
        }
    }

    public String GetPlatformId(bool withGeneration = true) {
        fixed (byte* ptr = platformId) {
            return new String((sbyte*)ptr, 0, 3) + platformGeneration;
        }
    }

    public void SetAppId(String str) {
        fixed (byte* ptr = appId) {
            StructUtils.Copy(str, ptr, 4);
        }
    }

    public void SetPlatformId(String str) {
        fixed (byte* ptr = platformId) {
            StructUtils.Copy(str, ptr, 3);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ICFEntryRecord {
    public EntryFlags entryFlags;
    public ICFType typeFlags;
    private fixed byte padding[24];
    public Version version;
    public Timestamp timestamp;
    public Version requiredVersion;
    public Version patchVersion;
    public Timestamp patchTimestamp;
    public Version patchRequiredVersion;
}

public enum ICFType : uint {
    System = 0x0000,
    App = 0x0001,
    Option = 0x0002,
    Patch = 0x0101
}

[Flags]
public enum EntryFlags : uint {
    Enabled1 = 0x0002,
    Enabled2 = 0x0100
}