namespace LongLive.Mods.Compatibility;

public interface ILongLiveCompatibilityRegistry
{
    void RegisterLibrary(LongLiveCompatibilityLibraryDescriptor descriptor);

    void RegisterRedirect(LongLiveCompatibilityRedirectDescriptor descriptor);

    LongLiveCompatibilitySnapshot CaptureSnapshot();
}
