using System;
using LongLive.Next.Abstractions.Models;

namespace LongLive.Next.Runtime;

public static class NextRuntimeExtensions
{
    public static NextRunHandle RunEvent(this NextRuntimeFacade runtime, string eventId, Action? onCompleted = null,
        string? tag = null)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        return runtime.EventRunner.RunEvent(new NextEventRequest(eventId, onCompleted, tag));
    }

    public static NextRunHandle RunScript(this NextRuntimeFacade runtime, string script, Action? onCompleted = null,
        string? tag = null)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        return runtime.EventRunner.RunScript(new NextScriptRequest(script, onCompleted, tag));
    }

    public static int GetInt(this NextRuntimeFacade runtime, string key, int defaultValue = 0)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        return runtime.StateStore.GetInt(key, defaultValue);
    }

    public static void SetInt(this NextRuntimeFacade runtime, string key, int value)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        runtime.StateStore.SetInt(key, value);
    }

    public static string GetString(this NextRuntimeFacade runtime, string key, string defaultValue = "")
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        return runtime.StateStore.GetString(key, defaultValue);
    }

    public static void SetString(this NextRuntimeFacade runtime, string key, string value)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        runtime.StateStore.SetString(key, value);
    }
}
