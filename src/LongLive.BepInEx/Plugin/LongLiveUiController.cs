using System;
using BepInEx.Logging;
using LongLive.Next.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveUiController
{
    private static ManualLogSource? _logger;
    private static NextRuntimeFacade? _runtime;
    private static LongLiveHostOptions? _options;
    private static LongLiveUiControllerBehaviour? _behaviour;
    private static LongLiveMainMenuPanel? _panel;

    public static void Initialize(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (_behaviour != null)
        {
            LogDebug("LongLive UI controller initialization skipped because the behaviour is already alive.");
            return;
        }

        var hostObject = new GameObject("LongLiveUiController");
        UnityEngine.Object.DontDestroyOnLoad(hostObject);
        _behaviour = hostObject.AddComponent<LongLiveUiControllerBehaviour>();
        LogDebug($"LongLive UI controller initialized on persistent host object. scene={SceneManager.GetActiveScene().name}");
    }

    public static bool TryTogglePanel(string source = "manual")
    {
        if (_runtime is null || _options is null)
        {
            LogDebug($"LongLive UI toggle skipped because the controller is not initialized. source={source}");
            return false;
        }

        if (_panel != null && !_panel.IsUsable)
        {
            LogDebug($"LongLive UI panel cache was stale and will be recreated. source={source}");
            _panel = null;
        }

        var uiRoot = ResolveUiRoot();
        if (uiRoot == null)
        {
            LogDebug($"LongLive UI toggle skipped because no UI root was found. source={source}");

            return false;
        }

        var localizer = new LongLiveTextLocalizer(_runtime);
        _panel ??= new LongLiveMainMenuPanel(_logger!, _runtime, _options, localizer);
        LogDebug($"LongLive UI toggle requested. source={source}, root={DescribeTransform(uiRoot)}, visible={_panel.IsVisible}");

        if (_panel.IsVisible)
        {
            _panel.Hide();
            LogDebug($"LongLive UI panel hidden. source={source}");
            return true;
        }

        var shown = _panel.TryShow(uiRoot);
        LogDebug($"LongLive UI panel show result. source={source}, shown={shown}");
        return shown;
    }

    public static void RefreshPanelIfVisible()
    {
        if (_panel?.IsVisible != true)
        {
            return;
        }

        TryTogglePanel("refresh-hide");
        TryTogglePanel("refresh-show");
    }

    public static bool IsInputCaptureActive => _panel?.IsVisible == true;

    public static void OnSceneLoaded(Scene scene)
    {
        if (_panel != null && !_panel.IsUsable)
        {
            LogDebug($"LongLive UI panel cache cleared after scene load because the previous root was destroyed. scene={scene.name}");
            _panel = null;
        }

        LogDebug($"LongLive UI controller observed scene load. scene={scene.name}, panelVisible={_panel?.IsVisible == true}");
    }

    private static Transform? ResolveUiRoot()
    {
        var mainUi = MainUIMag.inst;
        if (mainUi?.新主界面 != null && mainUi.新主界面.activeInHierarchy)
        {
            LogDebug($"LongLive UI root resolved from MainUIMag. root={DescribeTransform(mainUi.新主界面.transform)}");
            return mainUi.新主界面.transform;
        }

        if (NewUICanvas.Inst?.Canvas != null)
        {
            LogDebug($"LongLive UI root resolved from NewUICanvas. root={DescribeTransform(NewUICanvas.Inst.Canvas.transform)}");
            return NewUICanvas.Inst.Canvas.transform;
        }

        try
        {
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled)
                {
                    continue;
                }

                if (canvas.transform.parent == null)
                {
                    LogDebug($"LongLive UI root resolved from fallback canvas. root={DescribeTransform(canvas.transform)}");
                    return canvas.transform;
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"LongLive UI root fallback scan failed. error={ex.GetType().Name}: {ex.Message}");
        }

        return null;
    }

    private static void LogDebug(string message)
    {
        if (_options?.EnableDebugLogging.Value == true)
        {
            _logger?.LogInfo(message);
        }
    }

    private static string DescribeTransform(Transform? transform)
    {
        if (transform == null)
        {
            return "null";
        }

        return transform.name + ", scene=" + transform.gameObject.scene.name;
    }

    private sealed class LongLiveUiControllerBehaviour : MonoBehaviour
    {
        private bool _firstUpdateLogged;

        private void Update()
        {
            if (!_firstUpdateLogged)
            {
                _firstUpdateLogged = true;
                LogDebug($"LongLive UI controller update loop started. scene={SceneManager.GetActiveScene().name}");
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                LogDebug($"LongLive UI controller detected F2. scene={SceneManager.GetActiveScene().name}");
                TryTogglePanel("f2");
            }
        }
    }
}
