using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public static class LongLivePluginContext
{
    public static bool IsInitialized => LongLivePlugin.Instance is not null;

    public static NextRuntimeFacade GetRuntime()
    {
        var plugin = LongLivePlugin.Instance;
        if (plugin is null)
        {
            throw new InvalidOperationException("LongLivePlugin has not been initialized yet.");
        }

        return plugin.Runtime;
    }

    public static ManualLogSource GetLogger()
    {
        var logger = LongLivePlugin.LogSource;
        if (logger is null)
        {
            throw new InvalidOperationException("LongLivePlugin logger is not available yet.");
        }

        return logger;
    }

    public static LongLiveHostOptions GetOptions()
    {
        var plugin = LongLivePlugin.Instance;
        if (plugin is null)
        {
            throw new InvalidOperationException("LongLivePlugin has not been initialized yet.");
        }

        return plugin.Options;
    }
}
