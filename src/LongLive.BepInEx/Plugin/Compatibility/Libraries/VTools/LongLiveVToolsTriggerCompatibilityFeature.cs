using System;
using BepInEx.Logging;
using HarmonyLib;
using LongLive.Mods.Compatibility;
using LongLive.Next.Runtime;
using UnityEngine;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveVToolsTriggerCompatibilityFeature : ILongLiveCompatibilityFeature
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveCompatibilityRuntime _compatibilityRuntime;
    private readonly NextRuntimeFacade _runtime;

    public LongLiveVToolsTriggerCompatibilityFeature(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveCompatibilityRuntime compatibilityRuntime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _compatibilityRuntime = compatibilityRuntime ?? throw new ArgumentNullException(nameof(compatibilityRuntime));
    }

    public LongLiveCompatibilityLibraryDescriptor Library => new LongLiveCompatibilityLibraryDescriptor
    {
        LibraryId = "vtools",
        DisplayName = "VTools",
        RelationshipMode = LongLiveCompatibilityRelationshipMode.AdapterCompatible,
        CapabilityFamily = "dialog-trigger",
        DetectionTypeName = "Ventulus.VNext.DialogTrigger.AllMapMove",
        Notes = "Trigger bridge for common VTools dialog-trigger semantics around map movement, nearby NPC refresh, and jie-suan completion.",
    };

    public LongLiveCompatibilityRedirectDescriptor Redirect => new LongLiveCompatibilityRedirectDescriptor
    {
        RedirectId = LongLiveVToolsTriggerBridge.RedirectId,
        SourceLibraryId = "vtools",
        CapabilityFamily = "dialog-trigger",
        TargetSurface = typeof(LongLiveVToolsTriggerBridge).FullName ?? nameof(LongLiveVToolsTriggerBridge),
        DetectionTypeName = "SkySwordKill.Next.Main",
        DetectionMethodName = "DialogAnalysis.TryTrigger",
        EnabledByDefault = true,
        Notes = "Provides host-owned VTools-style dialog triggers without depending on the original VTools trigger patch stack.",
    };

    public LongLiveCompatibilityActivationRecord Install()
    {
        var sourceDetected = _compatibilityRuntime.IsTypeAvailable(Library.DetectionTypeName);
        var redirectEnabled = LongLiveCompatibilityOptionGate.IsVToolsEnabled();

        var applied = false;
        if (redirectEnabled && _runtime.IsAvailable)
        {
            applied = LongLiveVToolsTriggerBridge.Install(_logger, _compatibilityRuntime);
        }

        return LongLiveCompatibilityActivationFactory.Create(
            Redirect.RedirectId,
            Redirect.SourceLibraryId,
            sourceDetected,
            redirectEnabled,
            applied,
            applied
                ? (sourceDetected ? "trigger-bridge-installed-source-present" : "trigger-bridge-installed-source-missing")
                : (redirectEnabled ? "next-unavailable" : "disabled"),
            applied
                ? (sourceDetected
                    ? LongLiveCompatibilityText.Get("compatibility.vtools.trigger_bridge_present", "LongLive trigger bridge for VTools-style map and NPC triggers is active, and the reference VTools trigger surface is present.")
                    : LongLiveCompatibilityText.Get("compatibility.vtools.trigger_bridge_missing", "LongLive trigger bridge for VTools-style map and NPC triggers is active, but the reference VTools trigger surface was not detected in the current host."))
                : (redirectEnabled
                    ? LongLiveCompatibilityText.Get("compatibility.vtools.trigger_bridge_next_unavailable", "VTools-style trigger bridge was skipped because Next runtime trigger entry points are unavailable.")
                    : LongLiveCompatibilityText.Get("compatibility.vtools.trigger_bridge_disabled", "VTools-style trigger bridge is disabled by compatibility settings.")));
    }
}

internal static class LongLiveVToolsTriggerBridge
{
    public const string RedirectId = "adapter.vtools.dialog-trigger";

    private static bool _installed;
    private static ManualLogSource? _logger;
    private static LongLiveCompatibilityRuntime? _compatibilityRuntime;
    private static bool _nearNpcRefreshed;
    private static string _lastNearNpcSignature = string.Empty;
    private static bool _jieSuanHookRegistered;
    private static Action<MessageData>? _jieSuanCallback;
    private static Type? _dialogEnvironmentType;
    private static Type? _dialogAnalysisType;

    public static bool IsInstalled => _installed;

    public static bool Install(ManualLogSource logger, LongLiveCompatibilityRuntime compatibilityRuntime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compatibilityRuntime = compatibilityRuntime ?? throw new ArgumentNullException(nameof(compatibilityRuntime));

        if (_installed)
        {
            return true;
        }

        RegisterJieSuanHook();
        _installed = true;
        return true;
    }

    private static void RegisterJieSuanHook()
    {
        if (_jieSuanHookRegistered)
        {
            return;
        }

        _jieSuanCallback = OnJieSuanComplete;
        MessageMag.Instance.Register("MSG_Npc_JieSuan_COMPLETE", _jieSuanCallback);
        _jieSuanHookRegistered = true;
    }

    private static void OnJieSuanComplete(MessageData _)
    {
        if (!LongLiveCompatibilityOptionGate.IsVToolsEnabled())
        {
            return;
        }

        var plugin = LongLivePlugin.Instance;
        if (plugin == null)
        {
            return;
        }

        plugin.StartCoroutine(DispatchJieSuanComplete());
    }

    private static System.Collections.IEnumerator DispatchJieSuanComplete()
    {
        yield return null;
        TryTrigger(new[] { "结算完成", "OnJieSuanComplete" }, null, true, "vtools.jiesuan-complete");
    }

    private static bool TryTrigger(string[] triggerNames, object? env, bool allowMultiRun, string statusCode)
    {
        if (!LongLiveCompatibilityOptionGate.IsVToolsEnabled())
        {
            return false;
        }

        try
        {
            var environment = env ?? CreateDialogEnvironment();
            var dialogAnalysisType = ResolveDialogAnalysisType();
            var method = AccessTools.Method(dialogAnalysisType, "TryTrigger", new[] { typeof(string[]), environment.GetType(), typeof(bool) });
            if (method == null)
            {
                throw new MissingMethodException(dialogAnalysisType.FullName, "TryTrigger");
            }

            var rawResult = method.Invoke(null, new[] { triggerNames, environment, (object)allowMultiRun });
            var result = rawResult is bool boolResult && boolResult;
            _compatibilityRuntime?.RecordInvocation(RedirectId, statusCode, "result=" + result);
            return result;
        }
        catch (Exception ex)
        {
            _compatibilityRuntime?.RecordInvocation(RedirectId, statusCode + ".error", ex.GetType().Name + ": " + ex.Message);
            _logger?.LogWarning($"LongLive VTools trigger bridge failed: trigger={string.Join("/", triggerNames)}, error={ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private static object CreateDialogEnvironment()
    {
        var type = ResolveDialogEnvironmentType();
        return Activator.CreateInstance(type) ?? throw new InvalidOperationException("Failed to create DialogEnvironment instance.");
    }

    private static Type ResolveDialogEnvironmentType()
    {
        return _dialogEnvironmentType ??= ResolveRequiredType("SkySwordKill.Next.DialogSystem.DialogEnvironment");
    }

    private static Type ResolveDialogAnalysisType()
    {
        return _dialogAnalysisType ??= ResolveRequiredType("SkySwordKill.Next.DialogSystem.DialogAnalysis");
    }

    private static Type ResolveRequiredType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName, false);
            if (type != null)
            {
                return type;
            }
        }

        throw new TypeLoadException("Unable to resolve required Next runtime type: " + typeName);
    }

    private static void AddCustomData(object dialogEnvironment, string key, object value)
    {
        var customDataProperty = dialogEnvironment.GetType().GetProperty("customData");
        var customData = customDataProperty?.GetValue(dialogEnvironment, null);
        if (customData is System.Collections.IDictionary dictionary)
        {
            dictionary[key] = value;
            return;
        }

        throw new InvalidOperationException("DialogEnvironment.customData is unavailable.");
    }

    private static void TryStopMapGetWay()
    {
        try
        {
            var type = ResolveRequiredType("GetWay.MapGetWay");
            var instProperty = type.GetProperty("Inst");
            var instance = instProperty?.GetValue(null, null);
            if (instance == null)
            {
                return;
            }

            var isStopProperty = type.GetProperty("IsStop");
            if (isStopProperty?.CanWrite == true)
            {
                isStopProperty.SetValue(instance, true, null);
                return;
            }

            var isStopField = type.GetField("IsStop");
            isStopField?.SetValue(instance, true);
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(MapComponent), "NewMovaAvatar")]
    private static class MapComponentNewMovaAvatarPatch
    {
        private static bool Prefix()
        {
            if (!LongLiveCompatibilityOptionGate.IsVToolsEnabled())
            {
                return true;
            }

            if (AllMapManage.instance != null && AllMapManage.instance.isPlayMove)
            {
                return true;
            }

            var blocked = TryTrigger(new[] { "大地图移动前", "BeforeAllMapMove" }, null, false, "vtools.before-allmap-move");
            if (blocked)
            {
                TryStopMapGetWay();
            }

            return !blocked;
        }
    }

    [HarmonyPatch(typeof(MapInstComport), "AvatarMoveToThis")]
    private static class MapInstComportAvatarMoveToThisPatch
    {
        private static bool Prefix()
        {
            if (!LongLiveCompatibilityOptionGate.IsVToolsEnabled())
            {
                return true;
            }

            var blocked = TryTrigger(new[] { "副本移动前", "BeforeFubenMove" }, null, false, "vtools.before-fuben-move");
            return !blocked;
        }
    }

    [HarmonyPatch(typeof(UINPCJiaoHu), "RefreshNowMapNPC")]
    private static class UINpcJiaoHuRefreshNowMapNpcPatch
    {
        private static void Postfix()
        {
            if (LongLiveCompatibilityOptionGate.IsVToolsEnabled())
            {
                _nearNpcRefreshed = true;
            }
        }
    }

    [HarmonyPatch(typeof(UINPCLeftList), "RefreshNPC")]
    private static class UINpcLeftListRefreshNpcPatch
    {
        private static void Postfix()
        {
            if (!LongLiveCompatibilityOptionGate.IsVToolsEnabled() || !_nearNpcRefreshed)
            {
                return;
            }

            _nearNpcRefreshed = false;
            var jiaoHu = UINPCJiaoHu.Inst;
            if (jiaoHu == null)
            {
                return;
            }

            var list = new System.Collections.Generic.List<int>();
            list.AddRange(jiaoHu.TNPCIDList);
            list.AddRange(jiaoHu.NPCIDList);
            list.AddRange(jiaoHu.SeaNPCIDList);
            if (list.Count == 0)
            {
                return;
            }

            list.Sort();
            var signature = string.Join(",", list.ToArray());
            if (string.Equals(signature, _lastNearNpcSignature, StringComparison.Ordinal))
            {
                return;
            }

            _lastNearNpcSignature = signature;
            var env = CreateDialogEnvironment();
            AddCustomData(env, "NearNpcList", list);
            TryTrigger(new[] { "附近的人", "OnNearNpc" }, env, true, "vtools.on-near-npc");
        }
    }
}
