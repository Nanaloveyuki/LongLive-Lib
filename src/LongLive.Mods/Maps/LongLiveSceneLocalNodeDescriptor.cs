using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveSceneLocalNodeDescriptor
{
    public string LogicalId { get; set; } = string.Empty;

    public string TopologyLogicalId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public LongLiveMapPoint Position { get; set; }

    public bool IsCity { get; set; }

    public bool IsHidden { get; set; }

    public List<string> ConnectedNodeIds { get; set; } = new List<string>();

    public List<int> StaticAvatarIds { get; set; } = new List<int>();
}
