using System.Collections.Generic;

namespace LongLive.Next.Runtime.Inspection;

public sealed class NextContentRuntimeInspectionReport
{
    public NextContentRuntimeInspectionReport(
        bool isAvailable,
        string? localModsDirectory,
        NextContentRuntimeCapabilities capabilities,
        IReadOnlyList<NextRuntimeTypeInspection> types,
        IReadOnlyList<string> notes)
    {
        IsAvailable = isAvailable;
        LocalModsDirectory = localModsDirectory;
        Capabilities = capabilities;
        Types = types;
        Notes = notes;
    }

    public bool IsAvailable { get; }

    public string? LocalModsDirectory { get; }

    public NextContentRuntimeCapabilities Capabilities { get; }

    public IReadOnlyList<NextRuntimeTypeInspection> Types { get; }

    public IReadOnlyList<string> Notes { get; }
}
