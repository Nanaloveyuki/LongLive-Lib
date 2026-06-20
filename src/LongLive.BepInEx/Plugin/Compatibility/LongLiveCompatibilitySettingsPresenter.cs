using System;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCompatibilitySettingsPresenter
{
    public static string BuildToggleStatusSummary(LongLiveTextLocalizer localizer, LongLiveHostOptions options)
    {
        return string.Join(", ", new[]
        {
            string.Format(localizer.Get("compatibility.menu.easybatch"), options.EnableEasyBatchCompatibility.Value ? localizer.Get("common.enabled") : localizer.Get("common.disabled")),
            string.Format(localizer.Get("compatibility.menu.whiteze"), options.EnableWhiteZeCompatibility.Value ? localizer.Get("common.enabled") : localizer.Get("common.disabled")),
            string.Format(localizer.Get("compatibility.menu.vtools"), options.EnableVToolsCompatibility.Value ? localizer.Get("common.enabled") : localizer.Get("common.disabled")),
        });
    }

    public static string BuildSummary(LongLiveTextLocalizer localizer, LongLiveHostOptions options)
    {
        return string.Join("\n", new[]
        {
            localizer.Get("compatibility.menu.summary"),
            string.Empty,
            BuildToggleStatusSummary(localizer, options),
            string.Empty,
            localizer.Get("compatibility.menu.toggle_hint"),
        });
    }

    public static void CycleEasyBatch(LongLiveHostOptions options)
    {
        ToggleOption(options.EnableEasyBatchCompatibility);
    }

    public static void CycleWhiteZe(LongLiveHostOptions options)
    {
        ToggleOption(options.EnableWhiteZeCompatibility);
    }

    public static void CycleVTools(LongLiveHostOptions options)
    {
        ToggleOption(options.EnableVToolsCompatibility);
    }

    private static void ToggleOption(global::BepInEx.Configuration.ConfigEntry<bool> option)
    {
        option.Value = !option.Value;
        option.ConfigFile.Save();
    }
}
