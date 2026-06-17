using System.Collections.Generic;

namespace LongLive.Next.Runtime.Inspection;

public sealed class NextRuntimeTypeInspection
{
    public NextRuntimeTypeInspection(
        string typeName,
        bool isResolved,
        IReadOnlyList<string> staticProperties,
        IReadOnlyList<string> staticMethods)
    {
        TypeName = typeName;
        IsResolved = isResolved;
        StaticProperties = staticProperties;
        StaticMethods = staticMethods;
    }

    public string TypeName { get; }

    public bool IsResolved { get; }

    public IReadOnlyList<string> StaticProperties { get; }

    public IReadOnlyList<string> StaticMethods { get; }
}
