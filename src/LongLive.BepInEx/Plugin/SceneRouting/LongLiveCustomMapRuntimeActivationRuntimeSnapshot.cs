using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationRuntimeSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public int TargetCount { get; set; }

    public int ActivatedTargetCount { get; set; }

    public int HostBackedTargetCount { get; set; }

    public int PendingImplementationTargetCount { get; set; }

    public int BlockedTargetCount { get; set; }

    public bool HasActiveTarget { get; set; }

    public string ActiveTargetSceneLogicalId { get; set; } = string.Empty;

    public string ActiveActivationState { get; set; } = string.Empty;

    public string ActiveStatusCode { get; set; } = string.Empty;

    public IReadOnlyList<LongLiveCustomMapRuntimeActivationRuntimeTarget> Targets { get; set; } = new LongLiveCustomMapRuntimeActivationRuntimeTarget[0];
}
