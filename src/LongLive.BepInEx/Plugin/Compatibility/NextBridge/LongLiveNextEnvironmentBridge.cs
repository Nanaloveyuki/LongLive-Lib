using System;
using System.Collections;
using System.Reflection;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveNextEnvironmentBridge
{
    public static void WriteWarpResult(object? nativeEnvironment, string resultKey, LongLiveSceneRouteResult result)
    {
        if (nativeEnvironment is null)
        {
            return;
        }

        TrySetMapScene(nativeEnvironment, LongLiveNextRoutingBridge.ResolveCurrentSceneName());
        TrySetTmpArg(nativeEnvironment, resultKey, result.Succeeded ? 1 : 0);
    }

    public static void WriteRoleContext(object? nativeEnvironment, int roleId, string roleName, int roleBindId)
    {
        if (nativeEnvironment is null)
        {
            return;
        }

        TrySetProperty(nativeEnvironment, "roleID", roleId);
        TrySetProperty(nativeEnvironment, "roleName", roleName);
        TrySetProperty(nativeEnvironment, "roleBindID", roleBindId);
        TrySetMapScene(nativeEnvironment, LongLiveNextRoutingBridge.ResolveCurrentSceneName());
    }

    public static void WriteBoolResult(object? nativeEnvironment, string resultKey, bool value)
    {
        if (nativeEnvironment is null)
        {
            return;
        }

        TrySetTmpArg(nativeEnvironment, resultKey, value ? 1 : 0);
    }

    private static void TrySetProperty(object nativeEnvironment, string propertyName, object? value)
    {
        try
        {
            var property = nativeEnvironment.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanWrite == true)
            {
                property.SetValue(nativeEnvironment, value, null);
            }
        }
        catch
        {
        }
    }

    private static void TrySetMapScene(object nativeEnvironment, string sceneName)
    {
        try
        {
            var property = nativeEnvironment.GetType().GetProperty("mapScene", BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanWrite == true)
            {
                property.SetValue(nativeEnvironment, sceneName, null);
            }
        }
        catch
        {
        }
    }

    private static void TrySetTmpArg(object nativeEnvironment, string key, object value)
    {
        try
        {
            var property = nativeEnvironment.GetType().GetProperty("tmpArgs", BindingFlags.Public | BindingFlags.Instance);
            var dictionary = property?.GetValue(nativeEnvironment, null);
            if (dictionary is null)
            {
                return;
            }

            var removeMethod = dictionary.GetType().GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            removeMethod?.Invoke(dictionary, new object[] { key });

            if (dictionary is IDictionary nonGenericDictionary)
            {
                nonGenericDictionary[key] = value;
                return;
            }

            var addMethod = dictionary.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), value.GetType() }, null)
                ?? dictionary.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

            addMethod?.Invoke(dictionary, new[] { (object)key, value });
        }
        catch
        {
        }
    }
}
