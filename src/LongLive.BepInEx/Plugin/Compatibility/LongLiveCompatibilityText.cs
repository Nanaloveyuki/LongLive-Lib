using System;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCompatibilityText
{
    public static string Get(string key, string fallback)
    {
        try
        {
            var plugin = LongLivePlugin.Instance;
            if (plugin is null)
            {
                return fallback;
            }

            return new LongLiveTextLocalizer(plugin.Runtime).Get(key);
        }
        catch
        {
            return fallback;
        }
    }

    public static string Format(string key, string fallback, params object[] args)
    {
        return string.Format(Get(key, fallback), args);
    }
}
