namespace LongLive.BepInEx.Plugin;

public interface ILongLiveInstaller
{
    string Name { get; }

    void Install();
}
