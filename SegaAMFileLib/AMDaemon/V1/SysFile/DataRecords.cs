using System.Runtime.InteropServices;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable RedundantUnsafeContext
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordCredit {
    public uint crc;
    public uint uuid;
    public CreditConfig creditConfig;
    private fixed byte padding[472];
}

public enum CreditShareType : byte {
    SHARE_TYPE_DEFAULT = 0x0,
    SHARE_TYPE_COMMON = 0x1,
    SHARE_TYPE_INDIVIDUAL = 0x2
}

public enum CreditOperation : byte {
    OPERATION_DEFAULT = 0x0,
    OPERATION_COIN = 0x1,
    OPERATION_FREEPLAY = 0x2
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CreditConfig {
    public CreditShareType chuteType;
    public CreditShareType serviceType;
    public CreditOperation operation;
    public fixed byte coinRate[2];
    public byte bonusAdder;
    public byte creditRate;
    public fixed byte cost[8];
    private fixed byte reserved[1];
    public ushort coinAmount;
    private fixed byte padding[14];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordNetwork {
    public uint crc;
    public uint uuid;
    public NetworkConfig networkConfig;
    private fixed byte padding[376];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct NetworkConfig {
    public uint flag;
    public uint ipAddress;
    public uint subnetMask;
    public uint gateway;
    public uint primaryDns;
    public uint secondaryDns;
    fixed byte padding[104];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordBackup {
    public uint crc;
    public uint uuid;
    public CreditData creditData;
    public Bookkeeping bookkeeping;
    private fixed byte padding[296];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CreditData {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public CreditDataPlayer[] player;

    private fixed byte padding[48];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CreditDataPlayer {
    public byte credit;
    public byte remain;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct Bookkeeping {
    public fixed uint coinChute[8];
    private fixed byte padding[88];
    public uint emoneyCoin;
    public uint emoneyCredit;
    public uint totalCoin;
    public uint coinCredit;
    public uint serviceCredit;
    public uint totalCredit;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordErrorLog {
    public uint crc;
    public uint uuid;
    public byte writePointer;
    public byte logNum;
    public byte activeLastError;
    private fixed byte padding[21];

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
    public ErrorBody[] body;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ErrorBody {
    public ulong timestamp;
    public fixed byte gameId[4];
    public ushort error;
    public byte appStartCount;
    private fixed byte reserved[1];
    public ushort subError;
    private fixed byte padding[14];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordTimezone {
    private fixed byte padding[0x0200];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordLocalize {
    public uint crc;
    public uint uuid;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public String unk;

    private fixed byte padding[496];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordWlan {
    public uint crc;
    public uint uuid;
    public uint unk;
    public uint unk2;
    private fixed byte padding[496];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordDisplay {
    public uint crc;
    public uint uuid;
    public uint unk;
    private fixed byte padding[500];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordAime {
    public uint crc;
    public uint uuid;
    public UpdateProgress updateProgress;
    private fixed byte padding[500];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct UpdateProgress {
    public byte comPort;
    public byte unitIndex;
    public byte busy;
    private fixed byte padding[1];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordEmoney {
    public uint crc;
    public uint uuid;
    public uint availableBrandList;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public String terminalEndpoint;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public String dealNumber;

    public uint terminalDealNumber;
    public int closingNumber;
    public DealLog dealLog;
    public DealLog cashDealLog;
    public ReportLog reportLog;
    public SendCounterLog sendCounterLog;
    public uint authBrandList;
    public CurrentDealInfo currentDealInfo;
    private fixed byte padding[48];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLog {
    public ushort total;
    public ushort addPoint;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public DealLogBody[] body;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLogBody {
    public byte status;
    public EMoneyBrand brand;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public String dealNumber;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
    public String cardNumber;

    public byte state;
    public Timestamp time;
    public uint amount;
    public uint beforeBalance;
    public uint afterBalance;
    public uint price;
    public uint quantity;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct Timestamp {
    public ushort year;
    public byte month;
    public byte day;
    public byte hour;
    public byte minute;
    public byte second;
    private fixed byte padding[1];

    public static Timestamp Now() {
        DateTime now = DateTime.Now;
        return new Timestamp() {
            year = (ushort)now.Year,
            month = (byte)now.Month,
            day = (byte)now.Day,
            hour = (byte)now.Hour,
            minute = (byte)now.Minute,
            second = (byte)now.Second
        };
    }
}

public enum EMoneyBrand : byte {
    BRAND_UNKNOWN = 0x0,
    BRAND_NANACO = 0x1,
    BRAND_EDY = 0x2,
    BRAND_ID = 0x3,
    BRAND_TRANSPORT = 0x4,
    BRAND_WAON = 0x5,
    BRAND_PASELI = 0x6,
    BRAND_SAPICA = 0x7,
    BRAND_NUM = 0x8,
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ReportLog {
    public ushort total;
    public ushort addPoint;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public ReportLogBody[] body;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ReportLogBody {
    public int status;
    public Timestamp timestamp;
    public int count;
    public int amount;
    public int alarmCount;
    public int alarmAmount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct SendCounterLog {
    public fixed uint coin[8];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CurrentDealInfo {
    public AdditionalBody additional;
    public uint price;
    public uint quantity;
    public byte state;
    private fixed byte padding[3];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct AdditionalBody {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public String keychipId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
    public String gameId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public String userId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public String itemId;

    private fixed byte padding[5];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordDipsw {
    public uint crc;
    public uint uuid;
    public byte value;
    private fixed byte padding[503];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordCreditClear {
    public uint crc;
    public uint uuid;
    private fixed byte padding[504];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordAimePay {
    public uint crc;
    public uint uuid;
    public ActivationInfo activationInfo;
    private fixed byte reserved2[8];
    public DealLogAimePay dealLog;
    public CurrentDealInfoAimePay currentDealInfo;
    private fixed byte padding[384];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ActivationInfo {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public String storeCode;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 61)]
    public String storeName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public String companyCode;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 121)]
    public String companyName;

    private fixed byte padding[4];
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLogAimePay {
    public ushort total;
    public ushort addPoint;
    private fixed byte padding[4];

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public DealLogBodyAimePay[] body;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLogBodyAimePay {
    public byte state;
    public byte status;
    private fixed byte reserved[2];
    public uint errorCategory;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public String errorCode;

    public Timestamp time;
    public fixed byte accessCode[10];
    public fixed byte itemId[8];
    private fixed byte reserved2[6];
    public ulong receiptId;
    public uint quantity;
    public uint amount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CurrentDealInfoAimePay {
    public fixed byte accessCode[10];
    private fixed byte reserved[6];
    public fixed byte itemId[8];
    public Timestamp time;
    public uint amount;
    public uint quantity;
    public byte state;
    private fixed byte padding[7];
}