using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationArtifactSnapshot
{
    public int ArtifactCount { get; set; }

    public int PreparedSurfaceCount { get; set; }

    public int BoundProxyRouteCount { get; set; }

    public IReadOnlyList<LongLiveCustomMapRuntimeActivationArtifact> Artifacts { get; set; } = new LongLiveCustomMapRuntimeActivationArtifact[0];
}
