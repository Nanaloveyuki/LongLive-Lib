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
        foreach (var candidate in GetCandidatePaths(pluginDirectory))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "native", "target", "debug", "longlive_native_core.dll");
    }

    private static string[] GetCandidatePaths(string pluginDirectory)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return new[]
        {
            Path.Combine(pluginDirectory, "longlive_native_core.dll"),
            Path.GetFullPath(Path.Combine(pluginDirectory, "..", "longlive_native_core.dll")),
            Path.Combine(repoRoot, "artifacts", "workshop", "LongLive.Lib", "longlive_native_core.dll"),
            Path.Combine(repoRoot, "artifacts", "workshop", "LongLive.Lib", "plugins", "longlive_native_core.dll"),
            Path.Combine(repoRoot, "native", "target", "debug", "longlive_native_core.dll"),
            Path.Combine(repoRoot, "native", "target", "release", "longlive_native_core.dll")
        };
    }
}
