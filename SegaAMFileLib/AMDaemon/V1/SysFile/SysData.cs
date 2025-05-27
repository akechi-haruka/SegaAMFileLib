using System.Reflection;
using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;

public class SysData {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(SysData));

    private const uint FILE_LENGTH = 0x6000;
    private const uint OFFSET_CRC = 0;
    private const uint OFFSET_UID = 4;

    internal static readonly BackupRecordDefinition[] Records = new BackupRecordDefinition[] {
        new BackupRecordDefinition(typeof(DataRecordCredit), 0x0000, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordCredit), 0x3000, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordNetwork), 0x0200, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordNetwork), 0x3200, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordNetwork), 0x0400, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordNetwork), 0x3400, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordBackup), 0x1000, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordBackup), 0x4000, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordTimezone), 0x1400, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordTimezone), 0x4400, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordErrorLog), 0x1600, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordErrorLog), 0x4600, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordLocalize), 0x0C00, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordLocalize), 0x3C00, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordWlan), 0x0E00, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordWlan), 0x3E00, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordDisplay), 0x1800, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordDisplay), 0x4800, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordAime), 0x2000, 0x0200, true, true, 0x2D2D2D2D),
        new BackupRecordDefinition(typeof(DataRecordAime), 0x5000, 0x0200, true, true, 0x2D2D2D2D),
        new BackupRecordDefinition(typeof(DataRecordEmoney), 0x2200, 0x0600, true, true, 0x2D2D2D2D),
        new BackupRecordDefinition(typeof(DataRecordEmoney), 0x5200, 0x0600, true, true, 0x2D2D2D2D),
        new BackupRecordDefinition(typeof(DataRecordDipsw), 0x2800, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordDipsw), 0x5800, 0x0200, true, false, 0),
        new BackupRecordDefinition(typeof(DataRecordCreditClear), 0x2A00, 0x0200, true, true, 0x2D2D2D2D),
        new BackupRecordDefinition(typeof(DataRecordCreditClear), 0x5A00, 0x0200, true, true, 0x2D2D2D2D),
        new BackupRecordDefinition(typeof(DataRecordAimePay), 0x2C00, 0x0400, true, true, 0x2D2D2D2D),
        new BackupRecordDefinition(typeof(DataRecordAimePay), 0x5C00, 0x0400, true, true, 0x2D2D2D2D),
    };

    public DataRecordAime Aime;
    public DataRecordAimePay AimePay;
    public DataRecordBackup Backup;
    public DataRecordCredit Credit;
    public DataRecordCreditClear CreditClear;
    public DataRecordDipsw Dipsw;
    public DataRecordDisplay Display;
    public DataRecordEmoney Emoney;
    public DataRecordErrorLog ErrorLog;
    public DataRecordLocalize Localize;
    public DataRecordNetwork Network0;
    public DataRecordNetwork Network1;
    public DataRecordTimezone Timezone;
    public DataRecordWlan Wlan;

    public SysData(byte[] data) {
        if (data.Length != FILE_LENGTH) {
            throw new ArgumentException("data given is " + data.Length + " bytes, but " + FILE_LENGTH + " are expected");
        }

        Aime = GetRecord<DataRecordAime>(data);
        AimePay = GetRecord<DataRecordAimePay>(data);
        Backup = GetRecord<DataRecordBackup>(data);
        Credit = GetRecord<DataRecordCredit>(data);
        CreditClear = GetRecord<DataRecordCreditClear>(data);
        Dipsw = GetRecord<DataRecordDipsw>(data);
        Display = GetRecord<DataRecordDisplay>(data);
        Emoney = GetRecord<DataRecordEmoney>(data);
        ErrorLog = GetRecord<DataRecordErrorLog>(data);
        Localize = GetRecord<DataRecordLocalize>(data);
        Network0 = GetRecord<DataRecordNetwork>(data);
        Network1 = GetRecord<DataRecordNetwork>(data);
        Timezone = GetRecord<DataRecordTimezone>(data);
        Wlan = GetRecord<DataRecordWlan>(data);
    }

    public static T GetRecord<T>(byte[] data, int slot = 0) where T : struct {
        List<BackupRecordDefinition> records = Records.Where(record => record.Structure == typeof(T)).ToList();
        if (records.Count == 0) {
            throw new ArgumentException("no such record: " + typeof(T));
        }

        BackupRecordDefinition record = records[slot];
        byte[] struc = new byte[record.Size];
        Array.Copy(data, record.StartAddress, struc, 0, record.Size);

        bool initialized = false;

        if (record.HasCrc) {
            byte[] crcableData = new byte[record.Size - 4];
            ;
            Array.Copy(data, record.StartAddress + 4, crcableData, 0, record.Size - 4);
            uint calculated = SegaCrc32.CalcCrc32(crcableData);
            uint stored = BitConverter.ToUInt32(struc, 0);
            if (stored == 0x0) {
                LOG.LogWarning(record.Structure + " is uninitialized");
            } else if (stored != calculated) {
                String error = "CRC error in SysData for record " + record.Structure + " at " + record.StartAddress + ": Expected " + calculated.ToString("X2") + " but got " + stored.ToString("X2");
                LOG.LogError(error);
                throw new IOException(error);
            } else {
                LOG.LogTrace(record.Structure + " passed CRC check");
                initialized = true;
            }
        }

        if (record.HasUniqueId && initialized) {
            uint stored = BitConverter.ToUInt32(struc, 4);
            uint expected = record.UniqueId;
            if (stored != expected) {
                String error = "UID error in SysData for record " + record.Structure + " at " + record.StartAddress + ": Expected " + expected.ToString("X2") + " but got " + stored.ToString("X2");
                LOG.LogError(error);
                throw new IOException(error);
            }

            LOG.LogTrace(record.Structure + " passed UID check");
        }

        uint givenStruct = (uint)Marshal.SizeOf<T>();
        uint expectedStruct = record.Size;
        if (expectedStruct != givenStruct) {
            String error = "Size error in SysData for record " + record.Structure + " at " + record.StartAddress + ": Expected " + expectedStruct + " bytes but got " + givenStruct + " bytes";
            LOG.LogError(error);
            throw new IOException(error);
        }

        return StructUtils.FromBytes<T>(struc);
    }

    public static byte[] UpdateRecord<T>(byte[] data, T recordData, int slot = 0) where T : struct {
        List<BackupRecordDefinition> records = Records.Where(record => record.Structure == typeof(T)).ToList();
        if (records.Count == 0) {
            throw new ArgumentException("no such record: " + typeof(T));
        }
        
        LOG.LogTrace("Updating record " + typeof(T) + " in slot " + slot);

        BackupRecordDefinition record = records[slot];
        byte[] struc = StructUtils.GetBytes(recordData);

        int givenStruct = struc.Length;
        uint expectedStruct = record.Size;
        if (expectedStruct != givenStruct) {
            String error = "Size error in SysData for record " + record.Structure + " at " + record.StartAddress + ": Expected " + expectedStruct + " bytes but got " + givenStruct + " bytes";
            LOG.LogError(error);
            throw new IOException(error);
        }

        if (record.HasUniqueId) {
            byte[] uid = BitConverter.GetBytes(record.UniqueId);
            Array.Copy(uid, 0, struc, OFFSET_UID, uid.Length);
        }

        if (record.HasCrc) {
            byte[] crcableBytes = new byte[struc.Length - 4];
            Array.Copy(struc, OFFSET_CRC + sizeof(uint), crcableBytes, 0, struc.Length - sizeof(uint));
            
            uint crcnum = SegaCrc32.CalcCrc32(crcableBytes);
            byte[] crc = BitConverter.GetBytes(crcnum);
            
            Array.Copy(crc, 0, struc, OFFSET_CRC, crc.Length);
            
            LOG.LogDebug("New CRC for " + struc + ": 0x" + crcnum.ToString("X2"));
        }

        Array.Copy(struc, 0, data, record.StartAddress, record.Size);
        
        LOG.LogTrace("Updated " + struc + " in SysData");

        return data;
    }
    
}