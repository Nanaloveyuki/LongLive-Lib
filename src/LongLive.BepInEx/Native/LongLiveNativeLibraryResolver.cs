using System;
using System.IO;

namespace LongLive.BepInEx.Native;

internal static class LongLiveNativeLibraryResolver
{
    public static string ResolveLibraryPath(string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var pluginDirectory = Path.GetDirectoryName(typeof(Plugin.LongLivePlugin).Assembly.Location) ?? AppContext.BaseDirectory;
        var pluginLocalPath = Path.Combine(pluginDirectory, "longlive_native_core.dll");
        if (File.Exists(pluginLocalPath))
        {
            return pluginLocalPath;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "native", "target", "debug", "longlive_native_core.dll");
    }
}
