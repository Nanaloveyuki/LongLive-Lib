namespace LongLive.Mods.Maps;

public readonly struct LongLiveMapPoint
{
    public LongLiveMapPoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}
