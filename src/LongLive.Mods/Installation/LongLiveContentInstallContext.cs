using System;
using LongLive.Mods.Models;

namespace LongLive.Mods.Installation;

public sealed class LongLiveContentInstallContext
{
    public LongLiveContentInstallContext(LongLiveModPackage package)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
    }

    public LongLiveModPackage Package { get; }

    public string RootDirectory => Package.RootDirectory;

    public LongLiveModManifest Manifest => Package.Manifest;
}
