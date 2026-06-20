using System;
using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveSceneLocalTopologyCatalog : ILongLiveSceneLocalTopologyCatalog
{
    private readonly Dictionary<string, LongLiveSceneLocalTopologyDescriptor> _topologies = new Dictionary<string, LongLiveSceneLocalTopologyDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveSceneLocalTopologyDescriptor> _topologiesBySceneLogicalId = new Dictionary<string, LongLiveSceneLocalTopologyDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveSceneLocalTopologyDescriptor> _topologiesBySceneName = new Dictionary<string, LongLiveSceneLocalTopologyDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveSceneLocalNodeDescriptor> _nodes = new Dictionary<string, LongLiveSceneLocalNodeDescriptor>(StringComparer.Ordinal);

    public IReadOnlyCollection<LongLiveSceneLocalTopologyDescriptor> Topologies => _topologies.Values;

    public IReadOnlyCollection<LongLiveSceneLocalNodeDescriptor> Nodes => _nodes.Values;

    public int TopologyCount => _topologies.Count;

    public int NodeCount => _nodes.Count;

    public void RegisterTopology(LongLiveSceneLocalTopologyDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.LogicalId))
        {
            throw new ArgumentException("Scene-local topology descriptor must define a logical ID.", nameof(descriptor));
        }

        _topologies[descriptor.LogicalId] = descriptor;

        if (!string.IsNullOrWhiteSpace(descriptor.SceneLogicalId))
        {
            _topologiesBySceneLogicalId[descriptor.SceneLogicalId] = descriptor;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.SceneName))
        {
            _topologiesBySceneName[descriptor.SceneName] = descriptor;
        }
    }

    public void RegisterNode(LongLiveSceneLocalNodeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.LogicalId))
        {
            throw new ArgumentException("Scene-local topology node must define a logical ID.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.TopologyLogicalId))
        {
            throw new ArgumentException("Scene-local topology node must define a topology logical ID.", nameof(descriptor));
        }

        _nodes[descriptor.LogicalId] = descriptor;
    }

    public bool TryGetTopology(string logicalId, out LongLiveSceneLocalTopologyDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            descriptor = null;
            return false;
        }

        return _topologies.TryGetValue(logicalId, out descriptor);
    }

    public bool TryGetTopologyBySceneLogicalId(string sceneLogicalId, out LongLiveSceneLocalTopologyDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(sceneLogicalId))
        {
            descriptor = null;
            return false;
        }

        return _topologiesBySceneLogicalId.TryGetValue(sceneLogicalId, out descriptor);
    }

    public bool TryGetTopologyBySceneName(string sceneName, out LongLiveSceneLocalTopologyDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            descriptor = null;
            return false;
        }

        return _topologiesBySceneName.TryGetValue(sceneName, out descriptor);
    }

    public IReadOnlyList<LongLiveSceneLocalTopologyDescriptor> GetTopologiesForMod(string owningModId)
    {
        return FilterTopologies(descriptor => string.Equals(descriptor.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveSceneLocalNodeDescriptor> GetNodesForTopology(string topologyLogicalId)
    {
        return FilterNodes(descriptor => string.Equals(descriptor.TopologyLogicalId, topologyLogicalId, StringComparison.Ordinal));
    }

    private IReadOnlyList<LongLiveSceneLocalTopologyDescriptor> FilterTopologies(Func<LongLiveSceneLocalTopologyDescriptor, bool> predicate)
    {
        var results = new List<LongLiveSceneLocalTopologyDescriptor>();
        foreach (var descriptor in _topologies.Values)
        {
            if (predicate(descriptor))
            {
                results.Add(descriptor);
            }
        }

        return results;
    }

    private IReadOnlyList<LongLiveSceneLocalNodeDescriptor> FilterNodes(Func<LongLiveSceneLocalNodeDescriptor, bool> predicate)
    {
        var results = new List<LongLiveSceneLocalNodeDescriptor>();
        foreach (var descriptor in _nodes.Values)
        {
            if (predicate(descriptor))
            {
                results.Add(descriptor);
            }
        }

        return results;
    }
}
