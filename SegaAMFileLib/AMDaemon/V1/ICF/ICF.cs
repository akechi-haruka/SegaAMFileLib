using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace ICFReader;

public class InstallationConfigurationFile {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(InstallationConfigurationFile));

    private ICFHeaderRecord Header { get; }
    private readonly List<ICFEntryRecord> records;

    public InstallationConfigurationFile(byte[] data) {
        int headerLen = Marshal.SizeOf<ICFHeaderRecord>();
        int entryLen = Marshal.SizeOf<ICFEntryRecord>();

        if (data.Length < headerLen) {
            throw new ArgumentException("data given is " + data.Length + " bytes, but at least" + headerLen + " are expected");
        }

        byte[] headerBytes = new byte[headerLen];
        Array.Copy(data, headerBytes, headerLen);
        CheckCrc(headerBytes, "main CRC");
        Header = StructUtils.FromBytes<ICFHeaderRecord>(headerBytes);

        long fullLen = headerLen + Header.entryCount * entryLen;
        if (fullLen != data.Length) {
            String error = "Size error in ICF for total size: Expected " + fullLen + " bytes but got " + data.Length + " bytes";
            LOG.LogError(error);
            throw new IOException(error);
        }

        records = new List<ICFEntryRecord>();
        for (int i = 0; i < Header.entryCount; i++) {
            byte[] entryBytes = new byte[entryLen];
            Array.Copy(headerBytes, headerLen + i * entryLen, entryBytes, 0, entryLen);
            CheckCrc(entryBytes, "Entry " + i + " CRC");
            ICFEntryRecord entry = StructUtils.FromBytes<ICFEntryRecord>(headerBytes);
            records.Add(entry);
        }
    }

    public InstallationConfigurationFile(byte[] data, byte[] key, byte[] iv) : this(SegaAes.Decrypt(data, key, iv)) {
    }

    private void CheckCrc(byte[] data, string name) {
        byte[] crcableData = new byte[data.Length - 4];
        Array.Copy(data, data.Length + 4, crcableData, 0, data.Length - 4);
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
        return records.FirstOrDefault(r => r.entryFlags == EntryFlags.Enabled1 && r.entryFlags == EntryFlags.Enabled2 && r.typeFlags == type);
    }

    public ICFEntryRecord[] GetRecords(ICFType type) {
        return records.Where(r => r.entryFlags == EntryFlags.Enabled1 && r.entryFlags == EntryFlags.Enabled2 && r.typeFlags == type).ToArray();
    }

    public ICFEntryRecord? GetSystemRecord() {
        return GetRecord(ICFType.System);
    }

    public ICFEntryRecord? GetAppRecord() {
        return GetRecord(ICFType.App);
    }

    public byte[] Save() {
        // TODO
        return null;
    }
}