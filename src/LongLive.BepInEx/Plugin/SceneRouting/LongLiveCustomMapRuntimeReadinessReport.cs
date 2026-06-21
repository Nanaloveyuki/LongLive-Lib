using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeReadinessReport
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public string ActiveSceneLogicalId { get; set; } = string.Empty;

    public int TargetCount { get; set; }

    public int ReadyTargetCount { get; set; }

    public int BlockedTargetCount { get; set; }

    public int HostBackedTargetCount { get; set; }

    public int ActivationPendingTargetCount { get; set; }

    public bool HasActiveTarget { get; set; }

    public string ActiveTargetSceneLogicalId { get; set; } = string.Empty;

    public string ActiveStatusCode { get; set; } = string.Empty;

    public string ActiveDetail { get; set; } = string.Empty;

    public IReadOnlyList<LongLiveCustomMapRuntimeReadinessTarget> Targets { get; set; } = new LongLiveCustomMapRuntimeReadinessTarget[0];
}
