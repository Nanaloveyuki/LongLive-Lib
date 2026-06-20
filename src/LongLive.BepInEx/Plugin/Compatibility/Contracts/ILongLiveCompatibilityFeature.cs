using LongLive.Mods.Compatibility;

namespace LongLive.BepInEx.Plugin;

public interface ILongLiveCompatibilityFeature
{
    LongLiveCompatibilityLibraryDescriptor Library { get; }

    LongLiveCompatibilityRedirectDescriptor Redirect { get; }

    LongLiveCompatibilityActivationRecord Install();
}
