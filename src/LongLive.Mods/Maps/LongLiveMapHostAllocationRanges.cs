namespace LongLive.Mods.Maps;

public sealed class LongLiveMapHostAllocationRanges
{
    public LongLiveMapHostAllocationRanges(
        int mapTypeStart = 1000,
        int highlightIdStart = 1000,
        int nodeIndexStart = 100000,
        int outsideScenePosStart = 100000)
    {
        MapTypeStart = mapTypeStart;
        HighlightIdStart = highlightIdStart;
        NodeIndexStart = nodeIndexStart;
        OutsideScenePosStart = outsideScenePosStart;
    }

    public int MapTypeStart { get; }

    public int HighlightIdStart { get; }

    public int NodeIndexStart { get; }

    public int OutsideScenePosStart { get; }
}
