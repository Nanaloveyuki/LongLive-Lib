using System;
using System.Linq;
using System.Reflection;

namespace LongLive.BepInEx.Plugin.Configuration;

internal static class LongLiveConfigurationManagerTagFactory
{
    private static readonly string[] CandidateTypeNames =
    {
        "ConfigurationManagerAttributes",
        "BepInEx.ConfigurationManager.ConfigurationManagerAttributes",
        "BepInEx.ConfigurationManagerAttributes",
    };

    private static readonly Lazy<Type?> AttributesType = new(ResolveAttributesType);

    public static object[] Create(string displayName, string category, int order)
    {
        var attributesType = AttributesType.Value;
        if (attributesType == null)
        {
            return Array.Empty<object>();
        }

        var instance = Activator.CreateInstance(attributesType);
        if (instance == null)
        {
            return Array.Empty<object>();
        }

        SetMember(instance, "DisplayName", displayName);
        SetMember(instance, "Category", category);
        SetMember(instance, "Order", order);
        return new[] { instance };
    }

    private static Type? ResolveAttributesType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var candidateTypeName in CandidateTypeNames)
            {
                var resolved = assembly.GetType(candidateTypeName, false);
                if (resolved != null)
                {
                    return resolved;
                }
            }
        }

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(static type => string.Equals(type.Name, "ConfigurationManagerAttributes", StringComparison.Ordinal));
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static type => type != null).Cast<Type>().ToArray();
        }
    }

    private static void SetMember(object instance, string memberName, object value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (property?.CanWrite == true)
        {
            property.SetValue(instance, value, null);
            return;
        }

        var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
        field?.SetValue(instance, value);
    }
}
