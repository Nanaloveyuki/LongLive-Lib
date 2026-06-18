using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveWorldNodeDescriptor
{
    public string LogicalId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string PageId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public LongLiveMapPoint Position { get; set; }

    public LongLiveMapKind MapKind { get; set; } = LongLiveMapKind.Unknown;

    public string TargetSceneLogicalId { get; set; } = string.Empty;

    public string TargetSceneName { get; set; } = string.Empty;

    public int? NodeGroup { get; set; }

    public List<string> ConnectedNodeIds { get; set; } = new List<string>();

    public int? HostNodeIndex { get; set; }
}
