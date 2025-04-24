namespace Haruka.Arcade.SegaAMFileLib.AMDaemon.V1.SysFile;

class BackupRecordDefinition {
    public Type Structure { get; }
    public uint StartAddress { get; }
    public uint Size { get; }
    public bool HasCrc { get; }
    public bool HasUniqueId { get; }
    public uint UniqueId { get; }

    public BackupRecordDefinition(Type structure, uint startAddress, uint size, bool hasCrc, bool hasUniqueId, uint uniqueId) {
        Structure = structure;
        StartAddress = startAddress;
        Size = size;
        HasCrc = hasCrc;
        HasUniqueId = hasUniqueId;
        UniqueId = uniqueId;
    }
}