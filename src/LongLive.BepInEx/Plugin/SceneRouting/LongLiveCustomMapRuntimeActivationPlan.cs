using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationPlan
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public string ActiveSceneLogicalId { get; set; } = string.Empty;

    public string ActiveOwningModId { get; set; } = string.Empty;

    public bool HasActiveTarget { get; set; }

    public bool HasActiveBootstrapMatch { get; set; }

    public int RuntimeSceneCount { get; set; }

    public int BootstrapCount { get; set; }

    public int ActivationTargetCount { get; set; }

    public int ExternalActivationTargetCount { get; set; }

    public int TopologyCount { get; set; }

    public int TopologyNodeCount { get; set; }

    public IReadOnlyList<LongLiveCustomMapRuntimeActivationTarget> Targets { get; set; } = new LongLiveCustomMapRuntimeActivationTarget[0];
}
