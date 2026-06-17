using BepInEx.Configuration;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveHostOptions
{
    public LongLiveHostOptions(
        ConfigEntry<bool> enableDebugLogging,
        ConfigEntry<bool> enableContentRuntimeInspection,
        ConfigEntry<bool> enableNativeProbe,
        ConfigEntry<string> nativeLibraryPath,
        ConfigEntry<bool> enableDemoCommandRegistration,
        ConfigEntry<bool> enableDemoQueryRegistration,
        ConfigEntry<bool> enableJsonModDemoInstall,
        ConfigEntry<string> jsonModDemoPath,
        ConfigEntry<string> contentBackend)
    {
        EnableDebugLogging = enableDebugLogging;
        EnableContentRuntimeInspection = enableContentRuntimeInspection;
        EnableNativeProbe = enableNativeProbe;
        NativeLibraryPath = nativeLibraryPath;
        EnableDemoCommandRegistration = enableDemoCommandRegistration;
        EnableDemoQueryRegistration = enableDemoQueryRegistration;
        EnableJsonModDemoInstall = enableJsonModDemoInstall;
        JsonModDemoPath = jsonModDemoPath;
        ContentBackend = contentBackend;
    }

    public ConfigEntry<bool> EnableDebugLogging { get; }

    public ConfigEntry<bool> EnableContentRuntimeInspection { get; }

    public ConfigEntry<bool> EnableNativeProbe { get; }

    public ConfigEntry<string> NativeLibraryPath { get; }

    public ConfigEntry<bool> EnableDemoCommandRegistration { get; }

    public ConfigEntry<bool> EnableDemoQueryRegistration { get; }

    public ConfigEntry<bool> EnableJsonModDemoInstall { get; }

    public ConfigEntry<string> JsonModDemoPath { get; }

    public ConfigEntry<string> ContentBackend { get; }
}
