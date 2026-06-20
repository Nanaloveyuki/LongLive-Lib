using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

public interface ILongLiveThirdPartyMapDraftAdapter
{
    string AdapterId { get; }

    string SourceModId { get; }

    bool CanBuildDraft();

    LongLiveMapRegistryDraft BuildDraft();
}
