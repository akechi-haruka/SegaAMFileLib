using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.Misc;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;

/// <summary>
/// A record containing header (definition) data for a ICF file.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ICFHeaderRecord {
    /// <summary>
    /// The CRC checksum of the entire ICF file.
    /// </summary>
    public uint mainCrc;
    /// <summary>
    /// The size in bytes of the entire ICF file.
    /// </summary>
    public uint dataSize;
    private fixed byte padding[8];
    /// <summary>
    /// The number of <see cref="ICFEntryRecord"/>s in the ICF file.
    /// </summary>
    public ulong entryCount;
    /// <summary>
    /// The "app ID" of the ICF file (no zero terminator)
    /// </summary>
    public fixed byte appId[4];
    /// <summary>
    /// The "platform ID" of the ICF file (no zero terminator)
    /// </summary>
    public fixed byte platformId[3];
    /// <summary>
    /// The "platform generation" of the ICF file.
    /// </summary>
    public byte platformGeneration;
    /// <summary>
    /// The CRC checksum of all <see cref="ICFEntryRecord"/>s, which have <see cref="EntryFlags.Enabled1"/> and <see cref="EntryFlags.Enabled2"/> set.
    /// </summary>
    public uint entryCrc;
    private fixed byte padding_[28];

    /// <summary>
    /// Returns the number of <see cref="ICFEntryRecord"/>s in the ICF file.
    /// </summary>
    /// <returns>the number of <see cref="ICFEntryRecord"/>s in the ICF file.</returns>
    public uint GetEntryCount() {
        return (uint)entryCount;
    }

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
    /// Sets the app ID for this header record.
    /// </summary>
    /// <param name="str">The new app ID.</param>
    public void SetAppId(String str) {
        fixed (byte* ptr = appId) {
            StructUtils.Copy(str, ptr, 4);
        }
    }

    /// <summary>
    /// Sets the platform ID for this header record.
    /// </summary>
    /// <param name="str">The new platform ID.</param>
    public void SetPlatformId(String str) {
        fixed (byte* ptr = platformId) {
            StructUtils.Copy(str, ptr, 3);
        }
    }
}

/// <summary>
/// A record containing an entry (version information) in an ICF file.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ICFEntryRecord {
    /// <summary>
    /// Flags for the entry.
    /// </summary>
    public EntryFlags entryFlags;
    /// <summary>
    /// The type of the entry. (app, option, ...)
    /// </summary>
    public ICFType typeFlags;
    private fixed byte padding[24];
    /// <summary>
    /// The version this entry is depicting.
    /// </summary>
    public Version version;
    /// <summary>
    /// The timestamp of when this entry was made.
    /// </summary>
    public Timestamp timestamp;
    /// <summary>
    /// The prerequisite version that is needed for this entry.
    /// </summary>
    public Version requiredVersion;
    /// <summary>
    /// The patch version this entry is depicting (zeroed if this is not a <see cref="ICFType.Patch"/> entry)
    /// </summary>
    public Version patchVersion;
    /// <summary>
    /// The timestamp of when this patch entry was made (zeroed if this is not a <see cref="ICFType.Patch"/> entry)
    /// </summary>
    public Timestamp patchTimestamp;
    /// <summary>
    /// The required patch version this entry is depicting (zeroed if this is not a <see cref="ICFType.Patch"/> entry)
    /// </summary>
    public Version patchRequiredVersion;
}

/// <summary>
/// The type of the ICF entry.
/// </summary>
public enum ICFType : uint {
    /// <summary>
    /// This entry depicts the system version (OS, drivers, etc.)
    /// Required version will match the entry version.
    /// </summary>
    System = 0x0000,
    /// <summary>
    /// This entry depicts the app (game) version.
    /// Required version will be the system version.
    /// </summary>
    App = 0x0001,
    /// <summary>
    /// This entry depicts an option version.
    /// Required version will be the game or previous option version.
    /// </summary>
    Option = 0x0002,
    /// <summary>
    /// Unknown.
    /// </summary>
    Patch = 0x0101
}

/// <summary>
/// Unknown.
/// </summary>
[Flags]
public enum EntryFlags : uint {
    /// <summary>
    /// Unknown. Both Enabled1 and Enabled2 must be set for the entry to be valid/enabled.
    /// </summary>
    Enabled1 = 0x0002,
    /// <summary>
    /// Unknown. Both Enabled1 and Enabled2 must be set for the entry to be valid/enabled.
    /// </summary>
    Enabled2 = 0x0100
}