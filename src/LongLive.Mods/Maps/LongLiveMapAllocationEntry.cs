namespace LongLive.Mods.Maps;

public sealed class LongLiveMapAllocationEntry
{
    public LongLiveMapAllocationEntry(string logicalId, string owningModId, int hostValue)
    {
        LogicalId = logicalId;
        OwningModId = owningModId;
        HostValue = hostValue;
    }

    public string LogicalId { get; }

    public string OwningModId { get; }

    public int HostValue { get; }
}
