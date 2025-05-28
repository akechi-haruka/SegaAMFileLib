using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;

/// <summary>
/// Class for reading and writing ICF files, most commonly known as ICF1 and ICF2.
/// </summary>
public class InstallationConfigurationFile {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(InstallationConfigurationFile));

    /// <summary>
    /// The header of the ICF data, holding CRC, size, game and platform information.
    /// </summary>
    public ICFHeaderRecord Header;
    private readonly List<ICFEntryRecord> records;

    /// <summary>
    /// Creates a new (blank) ICF file with zeroed contents.
    /// </summary>
    public InstallationConfigurationFile() {
        Header = new ICFHeaderRecord();
        records = new List<ICFEntryRecord>();
    }
    
    /// <summary>
    /// Reads a ICF file from the given (decrypted) data.
    /// </summary>
    /// <param name="data">The raw content of an ICF file (retrieved by <see cref="File.ReadAllBytes"/> or similar)</param>
    /// <exception cref="ArgumentException">If the data array is invalid</exception>
    /// <exception cref="IOException">If there is an error while deserializing data</exception>
    public InstallationConfigurationFile(byte[] data) : this() {
        int headerLen = Marshal.SizeOf<ICFHeaderRecord>();
        int entryLen = Marshal.SizeOf<ICFEntryRecord>();

        if (data.Length < headerLen) {
            throw new ArgumentException("data given is " + data.Length + " bytes, but at least" + headerLen + " are expected");
        }

        byte[] headerBytes = new byte[headerLen];
        Array.Copy(data, headerBytes, headerLen);
        CheckCrc(data, "main CRC");
        Header = StructUtils.FromBytes<ICFHeaderRecord>(headerBytes);

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
            ICFEntryRecord entry = StructUtils.FromBytes<ICFEntryRecord>(entryBytes);
            if ((entry.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0) {
                dcrc ^= SegaCrc32.CalcCrc32(entryBytes);
            }
            records.Add(entry);
        }

        if (dcrc != Header.entryCrc) {
            String error = "CRC error in ICF for entries: Expected " + Header.entryCrc.ToString("X2") + " but got " + dcrc.ToString("X2");
            LOG.LogError(error);
            throw new IOException(error);
        }
    }

    /// <summary>
    /// Reads a ICF file from the given encrypted data.
    /// </summary>
    /// <param name="data">The encrypted content of an ICF file (retrieved by <see cref="File.ReadAllBytes"/> or similar)</param>
    /// <param name="key">The 16-byte long key used to decrypt the data.</param>
    /// <param name="iv">The 16-byte long IV used to decrypt the data.</param>
    /// <exception cref="ArgumentException">If the data array is invalid</exception>
    /// <exception cref="IOException">If there is an error while deserializing data</exception>
    public InstallationConfigurationFile(byte[] data, byte[] key, byte[] iv) : this(SegaAes.Decrypt(data, key, iv)) {
    }

    private static void CheckCrc(byte[] data, string name) {
        LOG.LogDebug("CRC-ing " + data.Length + " bytes for " + name);
        byte[] crcableData = new byte[data.Length - 4];
        Array.Copy(data, 4, crcableData, 0, data.Length - 4);
        uint calculated = SegaCrc32.CalcCrc32(crcableData);
        uint stored = BitConverter.ToUInt32(data, 0);
        if (stored != calculated) {
            String error = "CRC error in ICF for " + name + ": Expected " + calculated.ToString("X2") + " but got " + stored.ToString("X2");
            LOG.LogError(error);
            throw new IOException(error);
        } else {
            LOG.LogTrace("ICF passed " + name + " check");
        }
    }

    /// <summary>
    /// Returns the number of <see cref="ICFEntryRecord"/>s in this ICF.
    /// </summary>
    /// <returns>The number of <see cref="ICFEntryRecord"/>s in this ICF.</returns>
    public int GetRecordCount() {
        return records.Count;
    }

    /// <summary>
    /// Returns the <see cref="ICFEntryRecord"/> at the given index.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="IndexOutOfRangeException">If the entry with the given index does not exist.</exception>
    /// <returns>The <see cref="ICFEntryRecord"/> at the given index.</returns>
    public ICFEntryRecord GetRecord(int index) {
        return records[index];
    }

    /// <summary>
    /// Gets the enabled record of the given type.
    /// </summary>
    /// <param name="type">The type to search for.</param>
    /// <returns>The <see cref="ICFEntryRecord"/> matching the given type, which also has <see cref="EntryFlags.Enabled1"/> and <see cref="EntryFlags.Enabled2"/> set, or null.</returns>
    public ICFEntryRecord? GetRecord(ICFType type) {
        return records.FirstOrDefault(r => (r.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0 && r.typeFlags == type);
    }
    /// <summary>
    /// Gets all enabled records of the given type.
    /// </summary>
    /// <param name="type">The type to search for.</param>
    /// <returns>The <see cref="ICFEntryRecord"/>s matching the given type, which also has <see cref="EntryFlags.Enabled1"/> and <see cref="EntryFlags.Enabled2"/> set,.</returns>
    public ICFEntryRecord[] GetRecords(ICFType type) {
        return records.Where(r => (r.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0 && r.typeFlags == type).ToArray();
    }

    /// <summary>
    /// Gets the record of <see cref="ICFType.System"/>.
    /// </summary>
    /// <returns>The record of <see cref="ICFType.System"/> or null.</returns>
    public ICFEntryRecord? GetSystemRecord() {
        return GetRecord(ICFType.System);
    }

    /// <summary>
    /// Gets the record of <see cref="ICFType.App"/>.
    /// </summary>
    /// <returns>The record of <see cref="ICFType.App"/> or null.</returns>
    public ICFEntryRecord? GetAppRecord() {
        return GetRecord(ICFType.App);
    }

    /// <summary>
    /// Adds the given record and updates <see cref="ICFHeaderRecord.entryCount"/> and <see cref="ICFHeaderRecord.dataSize"/>.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void AddRecord(ICFEntryRecord record) {
        records.Add(record);
        UpdateHeaderAfterModification();
    }

    /// <summary>
    /// Deletes all records (excluding header) and updates <see cref="ICFHeaderRecord.entryCount"/> and <see cref="ICFHeaderRecord.dataSize"/>.
    /// </summary>
    public void ClearRecords() {
        records.Clear();
        UpdateHeaderAfterModification();
    }

    private void UpdateHeaderAfterModification() {
        int headerLen = Marshal.SizeOf<ICFHeaderRecord>();
        int entryLen = Marshal.SizeOf<ICFEntryRecord>();
        
        Header.entryCount = (ulong)records.Count;
        Header.dataSize = (uint)(headerLen + records.Count * entryLen);
    }

    /// <summary>
    /// Serializes this ICF file to a byte array (unencrypted).
    /// </summary>
    /// <returns>A byte array of the serialized data (header + entries) of this ICF file.</returns>
    public byte[] Save() {
        int headerLen = Marshal.SizeOf<ICFHeaderRecord>();
        int entryLen = Marshal.SizeOf<ICFEntryRecord>();
        long fullLen = headerLen + Header.GetEntryCount() * entryLen;

        byte[] output = new byte[fullLen];
        uint dcrc = 0;

        for (int i = 0; i < records.Count; i++) {
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