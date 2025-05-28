using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.ICF;

public class InstallationConfigurationFile {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(InstallationConfigurationFile));

    public ICFHeaderRecord Header;
    private readonly List<ICFEntryRecord> records;

    public InstallationConfigurationFile() {
        Header = new ICFHeaderRecord();
        records = new List<ICFEntryRecord>();
    }
    
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

    public InstallationConfigurationFile(byte[] data, byte[] key, byte[] iv) : this(SegaAes.Decrypt(data, key, iv)) {
    }

    private void CheckCrc(byte[] data, string name) {
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

    public int GetRecordCount() {
        return records.Count;
    }

    public ICFEntryRecord GetRecord(int index) {
        return records[index];
    }

    public ICFEntryRecord? GetRecord(ICFType type) {
        return records.FirstOrDefault(r => (r.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0 && r.typeFlags == type);
    }

    public ICFEntryRecord[] GetRecords(ICFType type) {
        return records.Where(r => (r.entryFlags & (EntryFlags.Enabled1 | EntryFlags.Enabled2)) != 0 && r.typeFlags == type).ToArray();
    }

    public ICFEntryRecord? GetSystemRecord() {
        return GetRecord(ICFType.System);
    }

    public ICFEntryRecord? GetAppRecord() {
        return GetRecord(ICFType.App);
    }

    public void AddRecord(ICFEntryRecord record) {
        records.Add(record);
        UpdateHeaderAfterModification();
    }

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