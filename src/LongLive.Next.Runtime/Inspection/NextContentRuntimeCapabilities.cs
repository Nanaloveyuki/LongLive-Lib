namespace LongLive.Next.Runtime.Inspection;

public sealed class NextContentRuntimeCapabilities
{
    public NextContentRuntimeCapabilities(
        bool hasMainType,
        bool hasModManager,
        bool hasMainDataContainer,
        bool hasResourcesManager,
        bool hasJsonDataPatch,
        bool hasModResourcesSpritePatch,
        bool hasModResourcesTexturePatch,
        bool canResolveLocalModsDirectory)
    {
        HasMainType = hasMainType;
        HasModManager = hasModManager;
        HasMainDataContainer = hasMainDataContainer;
        HasResourcesManager = hasResourcesManager;
        HasJsonDataPatch = hasJsonDataPatch;
        HasModResourcesSpritePatch = hasModResourcesSpritePatch;
        HasModResourcesTexturePatch = hasModResourcesTexturePatch;
        CanResolveLocalModsDirectory = canResolveLocalModsDirectory;
    }

    public bool HasMainType { get; }

    public bool HasModManager { get; }

    public bool HasMainDataContainer { get; }

    public bool HasResourcesManager { get; }

    public bool HasJsonDataPatch { get; }

    public bool HasModResourcesSpritePatch { get; }

    public bool HasModResourcesTexturePatch { get; }

    public bool CanResolveLocalModsDirectory { get; }
}
