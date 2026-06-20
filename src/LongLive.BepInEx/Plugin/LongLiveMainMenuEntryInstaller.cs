using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using LongLive.BepInEx.Native;
using LongLive.Mods.Maps;
using LongLive.Next.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMainMenuEntryInstaller : ILongLiveInstaller
{
    private static LongLiveMainMenuEntryInstaller? _instance;

    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;

    public LongLiveMainMenuEntryInstaller(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMainMenuEntryInstaller";

    public void Install()
    {
        _instance = this;
        LongLiveUiController.Initialize(_logger, _runtime, _options);
        _logger.LogInfo("LongLive main-menu entry installer armed with Harmony main-menu patch.");
        TryInstallEntryImmediately();
    }

    private void TryInstallEntryImmediately()
    {
        var mainUi = MainUIMag.inst;
        if (mainUi is null)
        {
            if (_options.EnableDebugLogging.Value)
            {
                _logger.LogInfo("LongLive immediate main-menu install skipped because MainUIMag.inst is null.");
            }

            return;
        }

        try
        {
            mainUi.OpenMain();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"LongLive immediate main-menu OpenMain refresh failed: {ex.GetType().Name}: {ex.Message}");
        }

        InstallEntry(mainUi);
    }

    private void InstallEntry(MainUIMag mainUi)
    {
        if (mainUi?.新主界面 is null)
        {
            return;
        }

        var sourceButton = mainUi.新主界面.transform.Find("Panel/btn/神仙斗法")?.gameObject;
        if (sourceButton is null)
        {
            if (_options.EnableDebugLogging.Value)
            {
                _logger.LogInfo("LongLive main-menu source button was not found via Panel/btn/神仙斗法.");
            }

            return;
        }

        var parent = sourceButton.transform.parent;
        var existingEntry = parent.Find("LongLiveEntry")?.gameObject;
        if (existingEntry is not null)
        {
            existingEntry.transform.localPosition = ResolveEntryLocalPosition(parent, sourceButton.transform.localPosition);
            existingEntry.transform.SetAsLastSibling();
            existingEntry.SetActive(true);

            if (_options.EnableDebugLogging.Value)
            {
                _logger.LogInfo($"LongLive main-menu entry already existed and was refreshed in place, localPosition={existingEntry.transform.localPosition}");
            }

            return;
        }

        var entry = UnityEngine.Object.Instantiate(sourceButton, parent, true);
        entry.name = "LongLiveEntry";
        entry.transform.localScale = sourceButton.transform.localScale;
        entry.transform.localRotation = sourceButton.transform.localRotation;
        entry.transform.localPosition = ResolveEntryLocalPosition(parent, sourceButton.transform.localPosition);
        entry.transform.SetAsLastSibling();
        entry.SetActive(true);

        ApplyButtonSprites(entry);
        BindClick(entry);

        if (_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo($"LongLive main-menu entry installed through OpenMain patch, localPosition={entry.transform.localPosition}");
        }
    }

    private void ApplyButtonSprites(GameObject entry)
    {
        var image = entry.GetComponent<Image>();
        var button = entry.GetComponent<FpBtn>();
        if (image is null || button is null)
        {
            _logger.LogInfo("LongLive main-menu entry sprite application skipped because Image or FpBtn was missing.");
            return;
        }

        var spriteSet = ResolveSpriteSet();
        if (spriteSet is null)
        {
            _logger.LogInfo("LongLive button sprite set could not be resolved from either Next assets or LongLiveAssets.");
            return;
        }

        image.sprite = spriteSet.Normal;
        image.color = Color.white;
        button.targetImage = image;
        button.nomalSprite = spriteSet.Normal;
        button.mouseDownSprite = spriteSet.Pressed;
        button.mouseEnterSprite = spriteSet.Hover;

        if (_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo($"LongLive button sprite set applied from: {spriteSet.SourceDescription}");
        }
    }

    private ButtonSpriteSet? ResolveSpriteSet()
    {
        var longLiveAssetRoot = Path.Combine(Path.GetDirectoryName(typeof(LongLivePlugin).Assembly.Location) ?? string.Empty, "LongLiveAssets", "Next");
        return TryLoadSpriteSet(
            longLiveAssetRoot,
            "logo_default.png",
            "logo_press.png",
            "logo_selector.png",
            $"LongLive assets ({longLiveAssetRoot})");

    }

    private ButtonSpriteSet? TryLoadSpriteSet(string assetRoot, string normalName, string pressedName, string hoverName, string sourceDescription)
    {
        var normalPath = Path.Combine(assetRoot, normalName);
        var pressedPath = Path.Combine(assetRoot, pressedName);
        var hoverPath = Path.Combine(assetRoot, hoverName);

        if (_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo($"LongLive probing sprite-set: {sourceDescription}");
            _logger.LogInfo($"LongLive sprite candidate normal exists={File.Exists(normalPath)} path={normalPath}");
            _logger.LogInfo($"LongLive sprite candidate pressed exists={File.Exists(pressedPath)} path={pressedPath}");
            _logger.LogInfo($"LongLive sprite candidate hover exists={File.Exists(hoverPath)} path={hoverPath}");
        }

        var normalSprite = LoadSprite(normalPath);
        var pressedSprite = LoadSprite(pressedPath);
        var hoverSprite = LoadSprite(hoverPath);
        if (normalSprite is null || pressedSprite is null || hoverSprite is null)
        {
            if (_options.EnableDebugLogging.Value)
            {
                _logger.LogInfo($"LongLive sprite-set probe failed: {sourceDescription}");
            }

            return null;
        }

        return new ButtonSpriteSet(normalSprite, pressedSprite, hoverSprite, sourceDescription);
    }

    private void BindClick(GameObject entry)
    {
        var button = entry.GetComponent<FpBtn>();
        if (button is null)
        {
            return;
        }

        button.mouseUpEvent = new UnityEvent();
        button.mouseUpEvent.AddListener(() =>
        {
            if (_options.EnableDebugLogging.Value)
            {
                _logger.LogInfo("LongLive main-menu entry click received.");
            }

            LongLiveUiController.TryTogglePanel("main-menu-entry");
        });
    }

    private static Vector3 ResolveEntryLocalPosition(Transform parent, Vector3 sourceLocalPosition)
    {
        var nextButton = parent.Find("Next");
        if (nextButton is not null)
        {
            return nextButton.localPosition + new Vector3(0f, 90f, 0f);
        }

        return sourceLocalPosition + new Vector3(0f, 180f, 0f);
    }

    private static Sprite? LoadSprite(string filePath)
    {
        if (!File.Exists(filePath))
        {
            LongLivePlugin.LogSource?.LogInfo($"LongLive sprite file missing: {filePath}");
            return null;
        }

        var bytes = File.ReadAllBytes(filePath);
        var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
        };

        if (!texture.LoadImage(bytes))
        {
            LongLivePlugin.LogSource?.LogInfo($"LongLive sprite file failed Texture2D.LoadImage: {filePath}");
            UnityEngine.Object.Destroy(texture);
            return null;
        }

        LongLivePlugin.LogSource?.LogInfo($"LongLive sprite file loaded successfully: {filePath}, size={texture.width}x{texture.height}");

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void ShowDiagnostics()
    {
        if (LongLiveUiController.TryTogglePanel("legacy-showdiagnostics"))
        {
            return;
        }

        var localizer = new LongLiveTextLocalizer(_runtime);

        if (!TryShowEntryMenu(localizer))
        {
            ShowDiagnosticsReport(localizer);
        }
    }

    private bool TryShowPanel(LongLiveTextLocalizer localizer)
    {
        return LongLiveUiController.TryTogglePanel("main-menu-panel");
    }

    private bool TryShowEntryMenu(LongLiveTextLocalizer localizer)
    {
        var dialogType = FindType("SkySwordKill.Next.FGUI.Dialog.WindowConfirmDialog");
        if (dialogType is null)
        {
            return false;
        }

        var createDialog = dialogType.GetMethod(
            "CreateDialog",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string), typeof(bool), typeof(Action), typeof(Action) },
            modifiers: null);

        if (createDialog is null)
        {
            return false;
        }

        createDialog.Invoke(null, new object?[]
        {
            localizer.Get("entry.menu.title"),
            localizer.Get("entry.menu.body"),
            true,
            (Action)(() => ShowCompatibilitySettings(localizer)),
            (Action)(() => ShowDiagnosticsReport(localizer)),
        });

        return true;
    }

    private void ShowDiagnosticsReport(LongLiveTextLocalizer localizer)
    {
        var report = _runtime.ContentInspector.Inspect();
        var nativeProbe = LongLivePlugin.Instance?.Native.CurrentProbeResult ?? LongLiveNativeProbeResult.Disabled();
        var snapshotService = new LongLiveMapSnapshotExportService();
        var snapshotExport = snapshotService.ExportCurrentSnapshot();
        var snapshot = snapshotService.CaptureCurrentSnapshot();
        var compatibility = LongLivePluginContext.GetCompatibilitySnapshot();
        var compatibilityStatus = compatibility.Activations.Count > 0
            ? string.Join(", ", compatibility.Activations.Select(static activation => activation.RedirectId + "=" + activation.StatusCode + "(" + activation.InvocationCount + ")"))
            : localizer.Get("common.not_reported_yet");
        var compatibilityToggles = LongLiveCompatibilitySettingsPresenter.BuildToggleStatusSummary(localizer, _options);
        var bridgeStatus = LongLiveBridgeStatusSnapshot.FromRuntime(_runtime);
        LongLivePluginContext.TryGetHostHandshake(out var handshake);
        var lines = new[]
        {
            $"{localizer.Get("diagnostics.plugin")}: {LongLivePluginMetadata.PluginName} {LongLivePluginMetadata.PluginVersion}",
            $"{localizer.Get("diagnostics.host_handshake_available")}: {handshake is not null}",
            $"{localizer.Get("diagnostics.host_handshake_version")}: {handshake?.HandshakeVersion.ToString() ?? localizer.Get("common.na")}",
            $"{localizer.Get("diagnostics.host_install_root")}: {localizer.GetOrNa(handshake?.InstallRoot)}",
            $"{localizer.Get("diagnostics.host_capabilities")}: {(handshake is null ? localizer.Get("common.na") : string.Join(", ", handshake.Capabilities))}",
            $"{localizer.Get("diagnostics.next_runtime_available")}: {_runtime.IsAvailable}",
            $"{localizer.Get("diagnostics.content_inspection_available")}: {report.IsAvailable}",
            $"{localizer.Get("diagnostics.local_mods_resolved")}: {report.Capabilities.CanResolveLocalModsDirectory}",
            $"{localizer.Get("diagnostics.content_backend")}: {_options.ContentBackend.Value}",
            $"{localizer.Get("diagnostics.bridge_reported")}: {bridgeStatus.HasReport}",
            $"{localizer.Get("diagnostics.bridge_status")}: {(bridgeStatus.HasReport ? bridgeStatus.Status : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_status_detail")}: {(bridgeStatus.HasReport ? bridgeStatus.Detail : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_host_version")}: {(bridgeStatus.HasReport ? localizer.GetOrNa(bridgeStatus.HostVersion) : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_host_present")}: {(bridgeStatus.HasReport ? bridgeStatus.HostPresent.ToString() : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_host_compatible")}: {(bridgeStatus.HasReport ? bridgeStatus.HostCompatible.ToString() : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_host_reason")}: {(bridgeStatus.HasReport ? localizer.GetOrNa(bridgeStatus.CompatibilityReason) : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_host_handshake_version")}: {(bridgeStatus.HasReport ? bridgeStatus.HandshakeVersion.ToString() : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_host_capabilities")}: {(bridgeStatus.HasReport ? localizer.GetOrNa(bridgeStatus.Capabilities) : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.bridge_host_reminder")}: {(bridgeStatus.HasReport ? bridgeStatus.ReminderEnabled.ToString() : localizer.Get("common.not_reported_yet"))}",
            $"{localizer.Get("diagnostics.compatibility_library_count")}: {compatibility.Libraries.Count}",
            $"{localizer.Get("diagnostics.compatibility_redirect_count")}: {compatibility.Redirects.Count}",
            $"{localizer.Get("diagnostics.compatibility_activation_count")}: {compatibility.Activations.Count}",
            $"{localizer.Get("diagnostics.compatibility_status")}: {compatibilityStatus}",
            $"{localizer.Get("diagnostics.compatibility_toggles")}: {compatibilityToggles}",
            $"{localizer.Get("diagnostics.map_snapshot_scenes")}: {snapshot.Scenes.Count}",
            $"{localizer.Get("diagnostics.map_snapshot_pages")}: {snapshot.Pages.Count}",
            $"{localizer.Get("diagnostics.map_snapshot_highlights")}: {snapshot.HighlightRegions.Count}",
            $"{localizer.Get("diagnostics.map_snapshot_nodes")}: {snapshot.Nodes.Count}",
            $"{localizer.Get("diagnostics.map_snapshot_export_success")}: {snapshotExport.Success}",
            $"{localizer.Get("diagnostics.map_snapshot_export_path")}: {(snapshotExport.Success ? snapshotExport.Path : localizer.Get("common.na"))}",
            $"{localizer.Get("diagnostics.map_snapshot_export_summary")}: {snapshotExport.Summary}",
            $"{localizer.Get("diagnostics.native_probe_enabled")}: {nativeProbe.Enabled}",
            $"{localizer.Get("diagnostics.native_probe_success")}: {nativeProbe.Success}",
            $"{localizer.Get("diagnostics.native_probe_summary")}: {nativeProbe.Summary}",
            $"{localizer.Get("diagnostics.native_probe_abi")}: {nativeProbe.AbiVersion?.ToString() ?? localizer.Get("common.na")}",
            $"{localizer.Get("diagnostics.native_probe_ready")}: {nativeProbe.ReadyFlag?.ToString() ?? localizer.Get("common.na")}",
            $"{localizer.Get("diagnostics.native_probe_sample_damage")}: {nativeProbe.TurnDamage?.ToString() ?? localizer.Get("common.na")}",
        };

        var body = string.Join("\n", lines);
        if (!TryShowNextConfirmWindow(body))
        {
            _logger.LogInfo(body);
        }
    }

    private void ShowCompatibilitySettings(LongLiveTextLocalizer localizer, Action<LongLiveTextLocalizer>? onComplete = null)
    {
        if (!TryShowCompatibilityDialogStep(localizer, 0, onComplete))
        {
            var title = localizer.Get("compatibility.menu.title");
            var body = LongLiveCompatibilitySettingsPresenter.BuildSummary(localizer, _options);
            _logger.LogInfo(title + "\n" + body);
            onComplete?.Invoke(localizer);
        }
    }

    private bool TryShowCompatibilityDialogStep(LongLiveTextLocalizer localizer, int stepIndex, Action<LongLiveTextLocalizer>? onComplete)
    {
        var dialogType = FindType("SkySwordKill.Next.FGUI.Dialog.WindowConfirmDialog");
        if (dialogType is null)
        {
            return false;
        }

        var createDialog = dialogType.GetMethod(
            "CreateDialog",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string), typeof(bool), typeof(Action), typeof(Action) },
            modifiers: null);

        if (createDialog is null)
        {
            return false;
        }

        var title = localizer.Get("compatibility.menu.title");
        var body = LongLiveCompatibilitySettingsPresenter.BuildSummary(localizer, _options) + "\n\n" + BuildCompatibilityDialogStepHint(localizer, stepIndex);

        createDialog.Invoke(null, new object?[]
        {
            title,
            body,
            true,
            (Action)(() => HandleCompatibilityDialogConfirm(localizer, stepIndex, onComplete)),
            (Action)(() => HandleCompatibilityDialogCancel(localizer, stepIndex, onComplete)),
        });

        return true;
    }

    private void HandleCompatibilityDialogConfirm(LongLiveTextLocalizer localizer, int stepIndex, Action<LongLiveTextLocalizer>? onComplete)
    {
        switch (stepIndex)
        {
            case 0:
                LongLiveCompatibilitySettingsPresenter.CycleEasyBatch(_options);
                TryShowCompatibilityDialogStep(localizer, 1, onComplete);
                return;
            case 1:
                LongLiveCompatibilitySettingsPresenter.CycleWhiteZe(_options);
                TryShowCompatibilityDialogStep(localizer, 2, onComplete);
                return;
            case 2:
                LongLiveCompatibilitySettingsPresenter.CycleVTools(_options);
                onComplete?.Invoke(localizer);
                return;
            default:
                onComplete?.Invoke(localizer);
                return;
        }
    }

    private void HandleCompatibilityDialogCancel(LongLiveTextLocalizer localizer, int stepIndex, Action<LongLiveTextLocalizer>? onComplete)
    {
        switch (stepIndex)
        {
            case 0:
                TryShowCompatibilityDialogStep(localizer, 1, onComplete);
                return;
            case 1:
                TryShowCompatibilityDialogStep(localizer, 2, onComplete);
                return;
            default:
                onComplete?.Invoke(localizer);
                return;
        }
    }

    private static string BuildCompatibilityDialogStepHint(LongLiveTextLocalizer localizer, int stepIndex)
    {
        return stepIndex switch
        {
            0 => localizer.Get("compatibility.menu.step_easybatch"),
            1 => localizer.Get("compatibility.menu.step_whiteze"),
            2 => localizer.Get("compatibility.menu.step_vtools"),
            _ => localizer.Get("compatibility.menu.step_done"),
        };
    }

    private bool TryShowNextConfirmWindow(string body)
    {
        var dialogType = FindType("SkySwordKill.Next.FGUI.Dialog.WindowConfirmDialog");
        if (dialogType is null)
        {
            return false;
        }

        var createDialog = dialogType.GetMethod(
            "CreateDialog",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string), typeof(bool), typeof(Action), typeof(Action) },
            modifiers: null);

        if (createDialog is null)
        {
            return false;
        }

        var localizer = new LongLiveTextLocalizer(_runtime);
        createDialog.Invoke(null, new object?[] { localizer.Get("diagnostics.title"), body, false, null, null });
        return true;
    }

    private static Type? FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName, false, false);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    [HarmonyPatch(typeof(MainUIMag), "OpenMain")]
    private static class MainUIMagOpenMainPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MainUIMag __instance)
        {
            _instance?.InstallEntry(__instance);
        }
    }

    private sealed class ButtonSpriteSet
    {
        public ButtonSpriteSet(Sprite normal, Sprite pressed, Sprite hover, string sourceDescription)
        {
            Normal = normal;
            Pressed = pressed;
            Hover = hover;
            SourceDescription = sourceDescription;
        }

        public Sprite Normal { get; }

        public Sprite Pressed { get; }

        public Sprite Hover { get; }

        public string SourceDescription { get; }
    }
}
