using System;
using System.Collections.Generic;
using System.Linq;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveHostHandshake
{
    public LongLiveHostHandshake(
        string pluginGuid,
        string pluginName,
        string pluginVersion,
        int handshakeVersion,
        bool nextRuntimeAvailable,
        string installRoot,
        IReadOnlyCollection<string> capabilities,
        DateTimeOffset initializedAtUtc)
    {
        PluginGuid = pluginGuid;
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        HandshakeVersion = handshakeVersion;
        NextRuntimeAvailable = nextRuntimeAvailable;
        InstallRoot = installRoot;
        Capabilities = capabilities;
        InitializedAtUtc = initializedAtUtc;
    }

    public string PluginGuid { get; }

    public string PluginName { get; }

    public string PluginVersion { get; }

    public int HandshakeVersion { get; }

    public bool NextRuntimeAvailable { get; }

    public string InstallRoot { get; }

    public IReadOnlyCollection<string> Capabilities { get; }

    public DateTimeOffset InitializedAtUtc { get; }

    public bool Supports(string capability)
    {
        return Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
    }
}
