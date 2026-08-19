using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;

/// <summary>
/// Class for reading and writing ICF files, most commonly known as ICF1 and ICF2.
/// </summary>
public class InstallationConfigurationFile {
    private static readonly ILogger LOG = Log.GetOrCreate("ICF ");

    /// <summary>
    /// The header of the ICF data, holding CRC, size, game and platform information.
    /// </summary>
    public IcfHeaderRecord Header;

    private readonly List<IcfEntryRecord> records;

    /// <summary>
    /// Creates a new (blank) ICF file with zeroed contents.
    /// </summary>
    public InstallationConfigurationFile() {
        Header = new IcfHeaderRecord();
        records = new List<IcfEntryRecord>();
    }

    /// <summary>
    /// Reads a ICF file from the given data.
    /// </summary>
    /// <param name="data">The raw content of an ICF file (retrieved by <see cref="File.ReadAllBytes"/> or similar)</param>
    /// <param name="encryption">The <see cref="EncryptionParameters"/> to use from the current <see cref="EncryptionEnvironment"/>, or null if <paramref name="data"/> is not encrypted.</param>
    /// <param name="ignoreCrc">If true, the checksums stored in the ICF are not verified.</param>
    /// <exception cref="ArgumentException">If the data array is invalid</exception>
    /// <exception cref="IOException">If there is an error while deserializing data</exception>
    public InstallationConfigurationFile(byte[] data, EncryptionParameters encryption = null, bool ignoreCrc = false) : this() {
        ArgumentNullException.ThrowIfNull(data);

        if (encryption != null) {
            data = SegaAes.DecryptFromEnv(data, encryption);
        }

        int headerLen = Marshal.SizeOf<IcfHeaderRecord>();
        int entryLen = Marshal.SizeOf<IcfEntryRecord>();

        if (data.Length < headerLen) {
            throw new ArgumentException("data given is " + data.Length + " bytes, but at least" + headerLen + " are expected");
        }

        byte[] headerBytes = new byte[headerLen];
        Array.Copy(data, headerBytes, headerLen);
        CheckCrc(data, "main CRC", ignoreCrc);
        Header = StructUtils.FromBytes<IcfHeaderRecord>(headerBytes);

        long fullLen = headerLen + Header.GetEntryCount() * entryLen;
        if (fullLen != data.Length) {
            String error = "Size error in ICF for total size: Expected " + fullLen + " bytes but got " + data.Length + " bytes";
            LOG.LogError(error);
            throw new IOException(error);
        }

        uint dcrc = 0;
        for (int i = 0; i < Header.GetEntryCount(); i++) {
            byte[] entryBytes = new byte[entryLen];
            Array.Copy(data, headerLen + i * entryLen, entryBytes, 0, entryLen);
            IcfEntryRecord entry = StructUtils.FromBytes<IcfEntryRecord>(entryBytes);
            if ((entry.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0) {
                dcrc ^= SegaCrc32.CalcCrc32(entryBytes);
            }

            records.Add(entry);
        }

        if (dcrc != Header.entryCrc) {
            String error = "CRC error in ICF for entries: Expected " + Header.entryCrc.ToString("X2") + " but got " + dcrc.ToString("X2");
            LOG.LogError(error);
            if (!ignoreCrc) {
                throw new IOException(error);
            }
        }
    }

    private static void CheckCrc(byte[] data, string name, bool ignoreError) {
        LOG.LogDebug("CRC-ing " + data.Length + " bytes for " + name);
        byte[] crcableData = new byte[data.Length - 4];
        Array.Copy(data, 4, crcableData, 0, data.Length - 4);
        uint calculated = SegaCrc32.CalcCrc32(crcableData);
        uint stored = BitConverter.ToUInt32(data, 0);
        if (stored != calculated) {
            String error = "CRC error in ICF for " + name + ": Expected " + calculated.ToString("X2") + " but got " + stored.ToString("X2");
            LOG.LogError(error);
            if (!ignoreError) {
                throw new IOException(error);
            }
        } else {
            LOG.LogTrace("ICF passed " + name + " check");
        }
    }

    /// <summary>
    /// Returns the number of <see cref="IcfEntryRecord"/>s in this ICF.
    /// </summary>
    /// <returns>The number of <see cref="IcfEntryRecord"/>s in this ICF.</returns>
    public int GetRecordCount() {
        return records.Count;
    }

    /// <summary>
    /// Returns the <see cref="IcfEntryRecord"/> at the given index.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="IndexOutOfRangeException">If the entry with the given index does not exist.</exception>
    /// <returns>The <see cref="IcfEntryRecord"/> at the given index.</returns>
    public IcfEntryRecord GetRecord(int index) {
        return records[index];
    }

    /// <summary>
    /// Gets the enabled record of the given type.
    /// </summary>
    /// <param name="type">The type to search for.</param>
    /// <returns>The <see cref="IcfEntryRecord"/> matching the given type, which also has <see cref="EntryFlags.Enabled1"/> and <see cref="EntryFlags.Enabled2"/> set, or null.</returns>
    public IcfEntryRecord? GetRecord(IcfType type) {
        return records.FirstOrDefault(r => (r.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0 && r.typeFlags == type);
    }

    /// <summary>
    /// Gets all enabled records of the given type.
    /// </summary>
    /// <param name="type">The type to search for.</param>
    /// <returns>The <see cref="IcfEntryRecord"/>s matching the given type, which also has <see cref="EntryFlags.Enabled1"/> and <see cref="EntryFlags.Enabled2"/> set,.</returns>
    public IcfEntryRecord[] GetRecords(IcfType type) {
        return records.Where(r => (r.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0 && r.typeFlags == type).ToArray();
    }

    /// <summary>
    /// Gets all records.
    /// </summary>
    /// <returns>All records stored in this ICF.</returns>
    public IcfEntryRecord[] GetRecords() {
        return records.ToArray();
    }

    /// <summary>
    /// Gets the record of <see cref="IcfType.System"/>.
    /// </summary>
    /// <returns>The record of <see cref="IcfType.System"/> or null.</returns>
    public IcfEntryRecord? GetSystemRecord() {
        return GetRecord(IcfType.System);
    }

    /// <summary>
    /// Gets the record of <see cref="IcfType.App"/>.
    /// </summary>
    /// <returns>The record of <see cref="IcfType.App"/> or null.</returns>
    public IcfEntryRecord? GetAppRecord() {
        return GetRecord(IcfType.App);
    }

    /// <summary>
    /// Adds the given record and updates <see cref="IcfHeaderRecord.entryCount"/> and <see cref="IcfHeaderRecord.dataSize"/>.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void AddRecord(IcfEntryRecord record) {
        records.Add(record);
        UpdateHeaderAfterModification();
    }

    /// <summary>
    /// Deletes all records (excluding header) and updates <see cref="IcfHeaderRecord.entryCount"/> and <see cref="IcfHeaderRecord.dataSize"/>.
    /// </summary>
    public void ClearRecords() {
        records.Clear();
        UpdateHeaderAfterModification();
    }

    private void UpdateHeaderAfterModification() {
        int headerLen = Marshal.SizeOf<IcfHeaderRecord>();
        int entryLen = Marshal.SizeOf<IcfEntryRecord>();

        Header.entryCount = (ulong)records.Count;
        Header.dataSize = (uint)(headerLen + records.Count * entryLen);
    }

    /// <summary>
    /// Serializes this ICF file to a byte array (unencrypted).
    /// </summary>
    /// <returns>A byte array of the serialized data (header + entries) of this ICF file.</returns>
    public byte[] Save() {
        int headerLen = Marshal.SizeOf<IcfHeaderRecord>();
        int entryLen = Marshal.SizeOf<IcfEntryRecord>();
        long fullLen = headerLen + Header.GetEntryCount() * entryLen;
        LOG.LogTrace("Writing " + fullLen + " bytes (" + headerLen + " + " + entryLen + " * " + Header.GetEntryCount() + ")");

        byte[] output = new byte[fullLen];
        uint dcrc = 0;

        for (int i = 0; i < records.Count; i++) {
            LOG.LogDebug("Writing record: " + records[i]);
            byte[] record = StructUtils.GetBytes(records[i]);
            EntryFlags flags = records[i].entryFlags;
            if ((flags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0) {
                dcrc ^= SegaCrc32.CalcCrc32(record);
            }

            Array.Copy(record, 0, output, headerLen + i * entryLen, record.Length);
        }

        Header.entryCrc = dcrc;
        Header.mainCrc = 0;

        byte[] headerBytes = StructUtils.GetBytes(Header);
        Array.Copy(headerBytes, output, headerBytes.Length);

        output = SegaCrc32.WriteCrcIntoFirst4Bytes(output);

        return output;
    }
}