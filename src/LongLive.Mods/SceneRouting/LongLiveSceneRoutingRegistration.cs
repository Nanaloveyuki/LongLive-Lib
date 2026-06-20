using System;

namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRoutingRegistration
{
    private readonly ILongLiveSceneRoutingRegistrationSink _sink;

    public LongLiveSceneRoutingRegistration(ILongLiveSceneRoutingRegistrationSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public void Register(ILongLiveSceneRouteRegistrationSource source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        _sink.RegisterSource(source);
    }
}
