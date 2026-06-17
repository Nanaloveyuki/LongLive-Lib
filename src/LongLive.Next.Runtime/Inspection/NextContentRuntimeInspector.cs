using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LongLive.Next.Runtime.Internal;

namespace LongLive.Next.Runtime.Inspection;

public sealed class NextContentRuntimeInspector
{
    private static readonly string[] TargetTypeNames =
    {
        "SkySwordKill.Next.Main",
        "SkySwordKill.Next.Mod.ModManager",
        "SkySwordKill.Next.Mod.MainDataContainer",
        "SkySwordKill.Next.Res.ResourcesManager",
        "SkySwordKill.Next.Patch.JsonDataPatch",
        "SkySwordKill.Next.Patch.ModResourcesLoadSpritePatch",
        "SkySwordKill.Next.Patch.ModResourcesLoadTexturePatch",
    };

    private readonly NextReflectionBridge _bridge;

    public NextContentRuntimeInspector()
        : this(new NextReflectionBridge())
    {
    }

    internal NextContentRuntimeInspector(NextReflectionBridge bridge)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public bool IsAvailable => _bridge.IsAvailable;

    public NextContentRuntimeInspectionReport Inspect()
    {
        var types = new List<NextRuntimeTypeInspection>(TargetTypeNames.Length);
        foreach (var typeName in TargetTypeNames)
        {
            types.Add(InspectType(typeName));
        }

        var localModsDirectory = TryGetLocalModsDirectory();
        var capabilities = BuildCapabilities(types, localModsDirectory);
        var notes = BuildNotes(types);
        return new NextContentRuntimeInspectionReport(
            _bridge.IsAvailable,
            localModsDirectory,
            capabilities,
            types,
            notes);
    }

    private NextRuntimeTypeInspection InspectType(string typeName)
    {
        var type = _bridge.TryResolveType(typeName);
        if (type is null)
        {
            return new NextRuntimeTypeInspection(typeName, false, Array.Empty<string>(), Array.Empty<string>());
        }

        var staticProperties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => $"{property.PropertyType.Name} {property.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var staticMethods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => !method.IsSpecialName)
            .Select(FormatMethodSignature)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return new NextRuntimeTypeInspection(typeName, true, staticProperties, staticMethods);
    }

    private string? TryGetLocalModsDirectory()
    {
        var mainType = _bridge.TryResolveType("SkySwordKill.Next.Main");
        if (mainType is null)
        {
            return null;
        }

        var localModsProperty = mainType.GetProperty("PathLocalModsDir", BindingFlags.Public | BindingFlags.Static);
        var lazyValue = localModsProperty?.GetValue(null);
        if (lazyValue is null)
        {
            return null;
        }

        var valueProperty = lazyValue.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        return valueProperty?.GetValue(lazyValue) as string;
    }

    private static IReadOnlyList<string> BuildNotes(IReadOnlyList<NextRuntimeTypeInspection> types)
    {
        var notes = new List<string>();
        if (types.Any(type => string.Equals(type.TypeName, "SkySwordKill.Next.Patch.JsonDataPatch", StringComparison.Ordinal) && type.IsResolved))
        {
            notes.Add("JsonDataPatch is present in the current AppDomain, so Next mod loading can likely be correlated with game JSON initialization.");
        }

        if (types.Any(type => string.Equals(type.TypeName, "SkySwordKill.Next.Res.ResourcesManager", StringComparison.Ordinal) && type.IsResolved))
        {
            notes.Add("ResourcesManager is present, so asset-oriented backends can probe load and sprite-cache entry points without hard-linking Next at build time.");
        }

        if (types.Any(type => string.Equals(type.TypeName, "SkySwordKill.Next.Mod.ModManager", StringComparison.Ordinal) && type.IsResolved))
        {
            notes.Add("ModManager is present, so a future host backend can inspect mod-load lifecycle methods before attempting runtime injection.");
        }

        return notes;
    }

    private static NextContentRuntimeCapabilities BuildCapabilities(IReadOnlyList<NextRuntimeTypeInspection> types, string? localModsDirectory)
    {
        return new NextContentRuntimeCapabilities(
            hasMainType: IsResolved(types, "SkySwordKill.Next.Main"),
            hasModManager: IsResolved(types, "SkySwordKill.Next.Mod.ModManager"),
            hasMainDataContainer: IsResolved(types, "SkySwordKill.Next.Mod.MainDataContainer"),
            hasResourcesManager: IsResolved(types, "SkySwordKill.Next.Res.ResourcesManager"),
            hasJsonDataPatch: IsResolved(types, "SkySwordKill.Next.Patch.JsonDataPatch"),
            hasModResourcesSpritePatch: IsResolved(types, "SkySwordKill.Next.Patch.ModResourcesLoadSpritePatch"),
            hasModResourcesTexturePatch: IsResolved(types, "SkySwordKill.Next.Patch.ModResourcesLoadTexturePatch"),
            canResolveLocalModsDirectory: !string.IsNullOrWhiteSpace(localModsDirectory));
    }

    private static bool IsResolved(IReadOnlyList<NextRuntimeTypeInspection> types, string typeName)
    {
        foreach (var type in types)
        {
            if (string.Equals(type.TypeName, typeName, StringComparison.Ordinal))
            {
                return type.IsResolved;
            }
        }

        return false;
    }

    private static string FormatMethodSignature(MethodInfo method)
    {
        var parameters = string.Join(", ", method.GetParameters().Select(parameter => $"{parameter.ParameterType.Name} {parameter.Name}"));
        return $"{method.ReturnType.Name} {method.Name}({parameters})";
    }
}
