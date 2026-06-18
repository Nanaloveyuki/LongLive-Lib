using System;
using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapAllocationRegistry
{
    private readonly LongLiveMapHostAllocationRanges _ranges;
    private readonly Dictionary<string, int> _mapTypeAllocations = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _highlightAllocations = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _nodeAllocations = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _outsideScenePosAllocations = new Dictionary<string, int>(StringComparer.Ordinal);

    private int _nextMapType;
    private int _nextHighlightId;
    private int _nextNodeIndex;
    private int _nextOutsideScenePos;

    public LongLiveMapAllocationRegistry(LongLiveMapHostAllocationRanges? ranges = null)
    {
        _ranges = ranges ?? new LongLiveMapHostAllocationRanges();
        _nextMapType = _ranges.MapTypeStart;
        _nextHighlightId = _ranges.HighlightIdStart;
        _nextNodeIndex = _ranges.NodeIndexStart;
        _nextOutsideScenePos = _ranges.OutsideScenePosStart;
    }

    public int AllocateMapType(string logicalId)
    {
        return Allocate(_mapTypeAllocations, logicalId, ref _nextMapType);
    }

    public int AllocateHighlightId(string logicalId)
    {
        return Allocate(_highlightAllocations, logicalId, ref _nextHighlightId);
    }

    public int AllocateNodeIndex(string logicalId)
    {
        return Allocate(_nodeAllocations, logicalId, ref _nextNodeIndex);
    }

    public int AllocateOutsideScenePos(string logicalId)
    {
        return Allocate(_outsideScenePosAllocations, logicalId, ref _nextOutsideScenePos);
    }

    public LongLiveMapRegistrationReport BuildReport(IReadOnlyDictionary<string, string> ownershipByLogicalId)
    {
        var report = new LongLiveMapRegistrationReport();
        AddEntries(report.AddMapTypeAllocation, _mapTypeAllocations, ownershipByLogicalId);
        AddEntries(report.AddHighlightAllocation, _highlightAllocations, ownershipByLogicalId);
        AddEntries(report.AddNodeAllocation, _nodeAllocations, ownershipByLogicalId);
        AddEntries(report.AddOutsideScenePosAllocation, _outsideScenePosAllocations, ownershipByLogicalId);
        return report;
    }

    private static int Allocate(Dictionary<string, int> allocations, string logicalId, ref int nextValue)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            throw new ArgumentException("Logical ID must not be empty.", nameof(logicalId));
        }

        if (allocations.TryGetValue(logicalId, out var existing))
        {
            return existing;
        }

        var allocated = nextValue;
        allocations.Add(logicalId, allocated);
        nextValue++;
        return allocated;
    }

    private static void AddEntries(
        Action<LongLiveMapAllocationEntry> add,
        IReadOnlyDictionary<string, int> allocations,
        IReadOnlyDictionary<string, string> ownershipByLogicalId)
    {
        foreach (var pair in allocations)
        {
            ownershipByLogicalId.TryGetValue(pair.Key, out var modId);
            add(new LongLiveMapAllocationEntry(pair.Key, modId ?? string.Empty, pair.Value));
        }
    }
}
