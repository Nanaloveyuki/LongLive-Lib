using BepInEx.Configuration;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveHostOptions
{
    public LongLiveHostOptions(
        ConfigEntry<bool> enableDebugLogging,
        ConfigEntry<bool> enableDemoCommandRegistration,
        ConfigEntry<bool> enableDemoQueryRegistration,
        ConfigEntry<bool> enableJsonModDemoInstall,
        ConfigEntry<string> jsonModDemoPath,
        ConfigEntry<string> contentBackend)
    {
        EnableDebugLogging = enableDebugLogging;
        EnableDemoCommandRegistration = enableDemoCommandRegistration;
        EnableDemoQueryRegistration = enableDemoQueryRegistration;
        EnableJsonModDemoInstall = enableJsonModDemoInstall;
        JsonModDemoPath = jsonModDemoPath;
        ContentBackend = contentBackend;
    }

    public ConfigEntry<bool> EnableDebugLogging { get; }

    public ConfigEntry<bool> EnableDemoCommandRegistration { get; }

    public ConfigEntry<bool> EnableDemoQueryRegistration { get; }

    public ConfigEntry<bool> EnableJsonModDemoInstall { get; }

    public ConfigEntry<string> JsonModDemoPath { get; }

    public ConfigEntry<string> ContentBackend { get; }
}
