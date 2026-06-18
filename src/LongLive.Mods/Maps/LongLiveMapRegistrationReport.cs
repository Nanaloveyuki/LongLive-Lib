using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapRegistrationReport
{
    private readonly List<LongLiveMapAllocationEntry> _mapTypeAllocations = new List<LongLiveMapAllocationEntry>();
    private readonly List<LongLiveMapAllocationEntry> _highlightAllocations = new List<LongLiveMapAllocationEntry>();
    private readonly List<LongLiveMapAllocationEntry> _nodeAllocations = new List<LongLiveMapAllocationEntry>();
    private readonly List<LongLiveMapAllocationEntry> _outsideScenePosAllocations = new List<LongLiveMapAllocationEntry>();

    public IReadOnlyList<LongLiveMapAllocationEntry> MapTypeAllocations => _mapTypeAllocations;

    public IReadOnlyList<LongLiveMapAllocationEntry> HighlightAllocations => _highlightAllocations;

    public IReadOnlyList<LongLiveMapAllocationEntry> NodeAllocations => _nodeAllocations;

    public IReadOnlyList<LongLiveMapAllocationEntry> OutsideScenePosAllocations => _outsideScenePosAllocations;

    public void AddMapTypeAllocation(LongLiveMapAllocationEntry entry)
    {
        _mapTypeAllocations.Add(entry);
    }

    public void AddHighlightAllocation(LongLiveMapAllocationEntry entry)
    {
        _highlightAllocations.Add(entry);
    }

    public void AddNodeAllocation(LongLiveMapAllocationEntry entry)
    {
        _nodeAllocations.Add(entry);
    }

    public void AddOutsideScenePosAllocation(LongLiveMapAllocationEntry entry)
    {
        _outsideScenePosAllocations.Add(entry);
    }
}
