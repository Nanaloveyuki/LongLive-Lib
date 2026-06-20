using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveEasyBatchUpdateRedirect
{
    public const string RedirectId = "redirect.easybatch.update";

    private readonly ManualLogSource _logger;
    private readonly LongLiveCompatibilityRuntime _compatibilityRuntime;
    private static bool _installed;

    public static bool IsInstalled => _installed;

    public LongLiveEasyBatchUpdateRedirect(ManualLogSource logger, LongLiveCompatibilityRuntime compatibilityRuntime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compatibilityRuntime = compatibilityRuntime ?? throw new ArgumentNullException(nameof(compatibilityRuntime));
    }

    public LongLiveHarmonyRedirectResult Install()
    {
        if (_installed)
        {
            return new LongLiveHarmonyRedirectResult(true, "already-installed", "EasyBatch.Update redirect was already installed.");
        }

        var prefix = AccessTools.Method(typeof(LongLiveEasyBatchUpdateRedirect), nameof(SkipEasyBatchUpdate));
        var result = LongLiveHarmonyRedirectInstaller.InstallPrefixSkip(
            _logger,
            _compatibilityRuntime,
            LongLivePluginMetadata.PluginGuid + ".compat.easybatch.update",
            RedirectId,
            "EasyBatch.Plugin",
            "Update",
            prefix,
            Type.EmptyTypes);

        if (result.Applied)
        {
            _installed = true;
        }

        return result;
    }

    private static bool SkipEasyBatchUpdate()
    {
        return !LongLiveCompatibilityOptionGate.IsEasyBatchEnabled();
    }
}
