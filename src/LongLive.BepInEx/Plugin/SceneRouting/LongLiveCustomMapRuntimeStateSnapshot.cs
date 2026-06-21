using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeStateSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public bool HasRegisteredScene { get; set; }

    public string ActiveSceneLogicalId { get; set; } = string.Empty;

    public string ActiveOwningModId { get; set; } = string.Empty;

    public bool HasRuntimeBootstrap { get; set; }

    public string RuntimeBootstrapSceneLogicalId { get; set; } = string.Empty;

    public string RuntimeBootstrapEntryNodeLogicalId { get; set; } = string.Empty;

    public string RuntimeBootstrapReturnSceneLogicalId { get; set; } = string.Empty;

    public string RuntimeBootstrapReturnSceneName { get; set; } = string.Empty;

    public int TotalSceneCount { get; set; }

    public int TotalBootstrapCount { get; set; }

    public int TotalTopologyCount { get; set; }

    public int TotalTopologyNodeCount { get; set; }

    public List<string> RegisteredModIds { get; set; } = new List<string>();

    public List<string> RuntimeBootstrapSamples { get; set; } = new List<string>();

    public int ExternalActivationTargetCount { get; set; }

    public LongLiveCustomMapRuntimeActivationPlan ActivationPlan { get; set; } = new LongLiveCustomMapRuntimeActivationPlan();

    public LongLiveCustomMapRuntimeReadinessReport Readiness { get; set; } = new LongLiveCustomMapRuntimeReadinessReport();
}
