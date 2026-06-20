namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCompatibilityOptionGate
{
    public static bool IsEasyBatchEnabled()
    {
        return LongLivePlugin.Instance?.Options.EnableEasyBatchCompatibility.Value ?? true;
    }

    public static bool IsWhiteZeToolsEnabled()
    {
        return LongLivePlugin.Instance?.Options.EnableWhiteZeCompatibility.Value ?? true;
    }

    public static bool IsVToolsEnabled()
    {
        return LongLivePlugin.Instance?.Options.EnableVToolsCompatibility.Value ?? true;
    }
}
