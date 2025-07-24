using System.Runtime.InteropServices;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable RedundantUnsafeContext
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global

namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;

/// <summary>
/// Record holding credit configuration data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordCredit {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Credit configuration data.
    /// </summary>
    public CreditConfig creditConfig;
    private fixed byte padding[472];
}

/// <summary>
/// Unknown.
/// </summary>
public enum CreditShareType : byte {
    /// <summary>
    /// Unknown.
    /// </summary>
    SHARE_TYPE_DEFAULT = 0x0,
    /// <summary>
    /// Unknown.
    /// </summary>
    SHARE_TYPE_COMMON = 0x1,
    /// <summary>
    /// Unknown.
    /// </summary>
    SHARE_TYPE_INDIVIDUAL = 0x2
}

/// <summary>
/// Unknown.
/// </summary>
public enum CreditOperation : byte {
    /// <summary>
    /// Unknown.
    /// </summary>
    OPERATION_DEFAULT = 0x0,
    /// <summary>
    /// Unknown.
    /// </summary>
    OPERATION_COIN = 0x1,
    /// <summary>
    /// Unknown.
    /// </summary>
    OPERATION_FREEPLAY = 0x2
}

/// <summary>
/// Main credit configuration object.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CreditConfig {
    /// <summary>
    /// Unknown.
    /// </summary>
    public CreditShareType chuteType;
    /// <summary>
    /// Unknown.
    /// </summary>
    public CreditShareType serviceType;
    /// <summary>
    /// Unknown.
    /// </summary>
    public CreditOperation operation;
    /// <summary>
    /// Unknown.
    /// </summary>
    public fixed byte coinRate[2];
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte bonusAdder;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte creditRate;
    /// <summary>
    /// Unknown.
    /// </summary>
    public fixed byte cost[8];
    private fixed byte reserved[1];
    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort coinAmount;
    private fixed byte padding[14];
}

/// <summary>
/// Record holding network configuration data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordNetwork {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Main object holding network configuration data.
    /// </summary>
    public NetworkConfig networkConfig;
    private fixed byte padding[376];
}

/// <summary>
/// Main object holding network configuration data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct NetworkConfig {
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint flag;
    /// <summary>
    /// The machine's IP address.
    /// </summary>
    public uint ipAddress;
    /// <summary>
    /// The machine's subnet mask.
    /// </summary>
    public uint subnetMask;
    /// <summary>
    /// The machine's default gateway.
    /// </summary>
    public uint gateway;
    /// <summary>
    /// The machine's primary DNS server.
    /// </summary>
    public uint primaryDns;
    /// <summary>
    /// The machine's secondary DNS server.
    /// </summary>
    public uint secondaryDns;
    fixed byte padding[104];
}

/// <summary>
/// Record holding credit and bookkeeping data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordBackup {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Main object holding credit data.
    /// </summary>
    public CreditData creditData;
    /// <summary>
    /// Main object holding bookkeeping data.
    /// </summary>
    public Bookkeeping bookkeeping;
    private fixed byte padding[296];

    /// <summary>
    /// Inserts the given number of coins and updates bookkeeping.
    /// </summary>
    /// <param name="amount">The number of coins to insert.</param>
    /// <param name="chute">The coin chute in which coins have been inserted.</param>
    /// <param name="player">The player slot who inserted coins.</param>
    public void InsertCoins(uint amount, uint chute = 0, uint player = 0) {
        creditData.player[player].credit += (byte)amount;
        bookkeeping.coinChute[chute] += amount;
        bookkeeping.coinCredit += amount;
        bookkeeping.totalCoin += amount;
        bookkeeping.totalCredit += amount;
    }

    /// <summary>
    /// Removes the given number of coins. Does not update bookeeping.
    /// </summary>
    /// <param name="amount">The number of coins to remove.</param>
    /// <param name="player">The player slot who removed coins.</param>
    public void RemoveCoins(uint amount, uint player = 0) {
        int c = creditData.player[player].credit;
        c -= (int)amount;
        creditData.player[player].credit = (byte)Math.Max(0, c);
    }
}

/// <summary>
/// Main object holding credit data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CreditData {
    /// <summary>
    /// Array holding credit data for all player slots. It seems only [0] is ever used.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public CreditDataPlayer[] player;

    private fixed byte padding[48];
}

/// <summary>
/// Main object holding player credit data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CreditDataPlayer {
    /// <summary>
    /// The counter of currently available credits.
    /// </summary>
    public byte credit;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte remain;
}

/// <summary>
/// Main object holding bookkeeping data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct Bookkeeping {
    /// <summary>
    /// Per coin-type coin data (coin 1, coin 2, ...)
    /// </summary>
    public fixed uint coinChute[8];
    private fixed byte padding[88];
    /// <summary>
    /// Number of coins inserted via e-money.
    /// </summary>
    public uint emoneyCoin;
    /// <summary>
    /// Number of credits inserted via e-money.
    /// </summary>
    public uint emoneyCredit;
    /// <summary>
    /// Total number of coins inserted (all chutes + e-money)
    /// </summary>
    public uint totalCoin;
    /// <summary>
    /// Total number of credits added by coins.
    /// </summary>
    public uint coinCredit;
    /// <summary>
    /// Total number of credits added by the service button.
    /// </summary>
    public uint serviceCredit;
    /// <summary>
    /// Grand total number of credits added.
    /// </summary>
    public uint totalCredit;
}

/// <summary>
/// Record holding error history information.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordErrorLog {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte writePointer;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte logNum;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte activeLastError;
    private fixed byte padding[21];

    /// <summary>
    /// List of occurred errors.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
    public ErrorBody[] body;
}

/// <summary>
/// Record holding error information.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ErrorBody {
    /// <summary>
    /// Time of error occurrence.
    /// </summary>
    public ulong timestamp;
    /// <summary>
    /// The app ID the error occurred in (ex. SDAA)
    /// </summary>
    public fixed byte gameId[4];
    /// <summary>
    /// The error number that has occurred. (ex. 8401)
    /// </summary>
    public ushort error;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte appStartCount;
    private fixed byte reserved[1];
    /// <summary>
    /// The sub error number that has occurred. (usually on some printer-related errors)
    /// </summary>
    public ushort subError;
    private fixed byte padding[14];
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordTimezone {
    private fixed byte padding[0x0200];
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordLocalize {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public String unk;

    private fixed byte padding[496];
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordWlan {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint unk;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint unk2;
    private fixed byte padding[496];
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordDisplay {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint unk;
    private fixed byte padding[500];
}

/// <summary>
/// Record holding Aime reader data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordAime {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Unknown.
    /// </summary>
    public UpdateProgress updateProgress;
    private fixed byte padding[500];
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct UpdateProgress {
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte comPort;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte unitIndex;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte busy;
    private fixed byte padding[1];
}

/// <summary>
/// Record for E-Money activation and purchase history data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordEmoney {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// The list of available brands (bitfield?)
    /// </summary>
    public uint availableBrandList;
    /// <summary>
    /// The URL for the /terminals endpoint from the initial authentication.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public String terminalEndpoint;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public String dealNumber;

    /// <summary>
    /// Unknown.
    /// </summary>
    public uint terminalDealNumber;
    /// <summary>
    /// Unknown.
    /// </summary>
    public int closingNumber;
    /// <summary>
    /// The purchase history of this cabinet.
    /// </summary>
    public DealLog dealLog;
    /// <summary>
    /// Unknown.
    /// </summary>
    public DealLog cashDealLog;
    /// <summary>
    /// Unknown.
    /// </summary>
    public ReportLog reportLog;
    /// <summary>
    /// Unknown.
    /// </summary>
    public SendCounterLog sendCounterLog;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint authBrandList;
    /// <summary>
    /// Unknown.
    /// </summary>
    public CurrentDealInfo currentDealInfo;
    private fixed byte padding[48];
}

/// <summary>
/// The record holding e-money purchase history.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLog {
    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort total;
    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort addPoint;

    /// <summary>
    /// The purchase history entries.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public DealLogBody[] body;
}

/// <summary>
/// Main object holding e-money purchase history.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLogBody {
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte status;
    /// <summary>
    /// The brand of card used.
    /// </summary>
    public EMoneyBrand brand;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public String dealNumber;

    /// <summary>
    /// The card ID of the used card.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
    public String cardNumber;

    /// <summary>
    /// Unknown.
    /// </summary>
    public byte state;
    /// <summary>
    /// The transaction timestamp.
    /// </summary>
    public Timestamp time;
    /// <summary>
    /// The amount of units purchased.
    /// </summary>
    public uint amount;
    /// <summary>
    /// The card's balance before the transaction.
    /// </summary>
    public uint beforeBalance;
    /// <summary>
    /// The card's balance after the transaction.
    /// </summary>
    public uint afterBalance;
    /// <summary>
    /// The price paid per unit.
    /// </summary>
    public uint price;
    /// <summary>
    /// The amount of units purchased.
    /// </summary>
    public uint quantity;
}

/// <summary>
/// List of E-Money brands.
/// </summary>
public enum EMoneyBrand : byte {
    /// <summary>
    /// Unknown.
    /// </summary>
    BRAND_UNKNOWN = 0x0,
    /// <summary>
    /// Nanaco.
    /// </summary>
    BRAND_NANACO = 0x1,
    /// <summary>
    /// Rakuten Edy.
    /// </summary>
    BRAND_EDY = 0x2,
    /// <summary>
    /// iD
    /// </summary>
    BRAND_ID = 0x3,
    /// <summary>
    /// Public transport (Suica, Pasmo, ...)
    /// </summary>
    BRAND_TRANSPORT = 0x4,
    /// <summary>
    /// Aeon Waon.
    /// </summary>
    BRAND_WAON = 0x5,
    /// <summary>
    /// Konami Paseli.
    /// </summary>
    BRAND_PASELI = 0x6,
    /// <summary>
    /// Sapica.
    /// </summary>
    BRAND_SAPICA = 0x7,
    /// <summary>
    /// Count of valid e-money brands.
    /// </summary>
    BRAND_NUM = 0x8,
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ReportLog {
    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort total;
    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort addPoint;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public ReportLogBody[] body;
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ReportLogBody {
    /// <summary>
    /// Unknown.
    /// </summary>
    public int status;
    /// <summary>
    /// Unknown.
    /// </summary>
    public Timestamp timestamp;
    /// <summary>
    /// Unknown.
    /// </summary>
    public int count;
    /// <summary>
    /// Unknown.
    /// </summary>
    public int amount;
    /// <summary>
    /// Unknown.
    /// </summary>
    public int alarmCount;
    /// <summary>
    /// Unknown.
    /// </summary>
    public int alarmAmount;
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct SendCounterLog {
    /// <summary>
    /// Unknown.
    /// </summary>
    public fixed uint coin[8];
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CurrentDealInfo {
    /// <summary>
    /// Unknown.
    /// </summary>
    public AdditionalBody additional;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint price;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint quantity;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte state;
    private fixed byte padding[3];
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct AdditionalBody {
    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public String keychipId;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
    public String gameId;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public String userId;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public String itemId;

    private fixed byte padding[5];
}

/// <summary>
/// The value of dip switches. (why is this stored?)
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordDipsw {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// A bitmask of the cabinet's 8 dip switches.
    /// </summary>
    public byte value;
    private fixed byte padding[503];
}

/// <summary>
/// Unused.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordCreditClear {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    private fixed byte padding[504];
}

/// <summary>
/// Record holding AimePay authentication and history data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DataRecordAimePay {
    /// <summary>
    /// The CRC checksum for this record.
    /// </summary>
    public uint crc;
    /// <summary>
    /// The UUID for this record.
    /// </summary>
    public uint uuid;
    /// <summary>
    /// Activation process information
    /// </summary>
    public ActivationInfo activationInfo;
    private fixed byte reserved2[8];
    /// <summary>
    /// History of transactions.
    /// </summary>
    public DealLogAimePay dealLog;
    /// <summary>
    /// Unknown.
    /// </summary>
    public CurrentDealInfoAimePay currentDealInfo;
    private fixed byte padding[384];
}

/// <summary>
/// Object holding AimePay authentication data.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct ActivationInfo {
    /// <summary>
    /// Store Code (same as in PowerOn)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public String storeCode;

    /// <summary>
    /// Store Name (returned from the AimePay authentication, and displayed in the test menu)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 61)]
    public String storeName;

    /// <summary>
    /// Company Code (same as in PowerOn)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public String companyCode;

    /// <summary>
    /// Company Name (returned from the AimePay authentication, and displayed in the test menu)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 121)]
    public String companyName;

    private fixed byte padding[4];
}

/// <summary>
/// Object holding the transaction history list for AimePay.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLogAimePay {
    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort total;
    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort addPoint;
    private fixed byte padding[4];

    /// <summary>
    /// Transaction history list.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public DealLogBodyAimePay[] body;
}

/// <summary>
/// Object holding transaction data for AimePay.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct DealLogBodyAimePay {
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte state;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte status;
    private fixed byte reserved[2];
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint errorCategory;

    /// <summary>
    /// Unknown.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public String errorCode;

    /// <summary>
    /// Time of the transaction.
    /// </summary>
    public Timestamp time;
    /// <summary>
    /// Access code of the card used.
    /// </summary>
    public fixed byte accessCode[10];
    /// <summary>
    /// ID of the item purchased (ex. 2000701000017)
    /// </summary>
    public fixed byte itemId[8];
    private fixed byte reserved2[6];
    /// <summary>
    /// Unknown.
    /// </summary>
    public ulong receiptId;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint quantity;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint amount;
}

/// <summary>
/// Unknown.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public unsafe struct CurrentDealInfoAimePay {
    /// <summary>
    /// Unknown.
    /// </summary>
    public fixed byte accessCode[10];
    private fixed byte reserved[6];
    /// <summary>
    /// Unknown.
    /// </summary>
    public fixed byte itemId[8];
    /// <summary>
    /// Unknown.
    /// </summary>
    public Timestamp time;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint amount;
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint quantity;
    /// <summary>
    /// Unknown.
    /// </summary>
    public byte state;
    private fixed byte padding[7];
}