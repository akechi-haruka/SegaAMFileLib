using System.Reflection;
using System.Runtime.InteropServices;
using Haruka.Arcade.SegaAMFileLib.CryptHash;
using Haruka.Arcade.SegaAMFileLib.Debugging;
using Haruka.Arcade.SegaAMFileLib.Misc;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;

/// <summary>
/// Class holding values that are contained in an instance of sysfile.dat.
/// </summary>
public class SysData {
    private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(SysData));

    private const uint FILE_LENGTH = 0x6000;
    private const uint OFFSET_CRC = 0;
    private const uint OFFSET_UID = 4;

    internal static readonly BackupRecordDefinition[] RECORDS = new BackupRecordDefinition[] {
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

    /// <summary>
    /// Aime Reader data.
    /// </summary>
    public DataRecordAime Aime;
    /// <summary>
    /// AimePay history data.
    /// </summary>
    public DataRecordAimePay AimePay;
    /// <summary>
    /// Credit and Bookkeeping data.
    /// </summary>
    public DataRecordBackup Backup;
    /// <summary>
    /// Credit configuration data.
    /// </summary>
    public DataRecordCredit Credit;
    /// <summary>
    /// Credit clear history data.
    /// </summary>
    public DataRecordCreditClear CreditClear;
    /// <summary>
    /// Dip switch data.
    /// </summary>
    public DataRecordDipsw Dipsw;
    /// <summary>
    /// Unknown.
    /// </summary>
    public DataRecordDisplay Display;
    /// <summary>
    /// E-Money authentication and history data.
    /// </summary>
    public DataRecordEmoney Emoney;
    /// <summary>
    /// System error history data.
    /// </summary>
    public DataRecordErrorLog ErrorLog;
    /// <summary>
    /// Unknown.
    /// </summary>
    public DataRecordLocalize Localize;
    /// <summary>
    /// Network configuration data for adapter 1.
    /// </summary>
    public DataRecordNetwork Network0;
    /// <summary>
    /// Network configuration data for adapter 2.
    /// </summary>
    public DataRecordNetwork Network1;
    /// <summary>
    /// Unknown.
    /// </summary>
    public DataRecordTimezone Timezone;
    /// <summary>
    /// Unknown.
    /// </summary>
    public DataRecordWlan Wlan;

    /// <summary>
    /// Reads the content of sysfile.dat.
    /// </summary>
    /// <param name="data">The raw file data (obtained with <see cref="File.ReadAllBytes"/> or similar)</param>
    /// <exception cref="ArgumentException">If the file length is invalid or a required record is missing.</exception>
    /// <exception cref="IOException">If there is an error deserializing the data.</exception>
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

    /// <summary>
    /// Loads a record from the given raw content of sysfile.dat.
    /// </summary>
    /// <param name="data">The raw file data to read from.</param>
    /// <param name="slot">The backup slot to load from (0-4).</param>
    /// <typeparam name="T">The type of record to load.</typeparam>
    /// <returns>An instantiated object of the given record type.</returns>
    /// <exception cref="ArgumentException">If the record type is not known.</exception>
    /// <exception cref="IndexOutOfRangeException">If the backup slot does not exist.</exception>
    /// <exception cref="IOException">If there is an error deserializing the data.</exception>
    public static T GetRecord<T>(byte[] data, int slot = 0) where T : struct {
        List<BackupRecordDefinition> records = RECORDS.Where(record => record.Structure == typeof(T)).ToList();
        if (records.Count == 0) {
            throw new ArgumentException("no such record: " + typeof(T));
        }

        BackupRecordDefinition record = records[slot];
        byte[] struc = new byte[record.Size];
        Array.Copy(data, record.StartAddress, struc, 0, record.Size);

        bool initialized = false;

        if (record.HasCrc) {
            byte[] crcableData = new byte[record.Size - 4];
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

    /// <summary>
    /// Updates a given record in the raw file data. (which can then be saved back to a file)
    /// </summary>
    /// <param name="data">The raw file data to read from.</param>
    /// <param name="recordData">The record to save back into the file data.</param>
    /// <param name="slot">The backup slot to load from (0-4).</param>
    /// <typeparam name="T">The type of record to load.</typeparam>
    /// <returns>The updated raw file data.</returns>
    /// <exception cref="ArgumentException">If the record type is not known.</exception>
    /// <exception cref="IndexOutOfRangeException">If the backup slot does not exist.</exception>
    /// <exception cref="IOException">If there is an error serializing the data.</exception>
    public static byte[] UpdateRecord<T>(byte[] data, T recordData, int slot = 0) where T : struct {
        List<BackupRecordDefinition> records = RECORDS.Where(record => record.Structure == typeof(T)).ToList();
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
            
            LOG.LogDebug("New CRC for " + record.Structure + ": 0x" + crcnum.ToString("X2"));
        }

        Array.Copy(struc, 0, data, record.StartAddress, record.Size);
        
        LOG.LogTrace("Updated " + record.Structure + " in SysData");

        return data;
    }
    
}