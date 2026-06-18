using System;
using System.Collections.Generic;
using BepInEx.Logging;
using LongLive.BepInEx.Native;
using LongLive.Next.Abstractions.State;
using LongLive.Next.Runtime;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveBootstrapper
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveNativeService _native;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;
    private readonly IReadOnlyList<ILongLiveInstaller> _installers;
    private bool _initialized;
    private bool _runtimeInstallCompleted;
    private bool _sceneHookRegistered;

    public LongLiveBootstrapper(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveNativeService native, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _native = native ?? throw new ArgumentNullException(nameof(native));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _installers = new ILongLiveInstaller[]
        {
            new LongLiveMapTraceInstaller(_logger, _options),
            new LongLiveMapSnapshotInstaller(_logger, _options),
            new LongLiveBattleTraceInstaller(_logger, _options),
            new LongLiveBulkItemUseInstaller(_logger, _options),
            new LongLiveMainMenuEntryInstaller(_logger, _runtime, _options),
            new LongLiveContentInspectionInstaller(_logger, _runtime, _options),
            new LongLiveNativeProbeInstaller(_logger, _native, _options),
            new LongLiveDemoInstaller(_logger, _runtime, _options),
            new LongLiveJsonModDemoInstaller(_logger, _runtime, _options),
        };
    }

    public void Initialize()
    {
        if (_initialized)
        {
            _logger.LogDebug("LongLive host bootstrap already initialized.");
            return;
        }

        _logger.LogInfo("LongLive host bootstrap initializing.");
        _logger.LogInfo($"Next runtime available: {_runtime.IsAvailable}");

        if (_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo("Debug logging is enabled.");
        }

        _initialized = true;

        EnsureSceneHook();
        TryInstallRuntimeFeatures();
    }

    public void Shutdown()
    {
        if (!_initialized)
        {
            return;
        }

        if (_sceneHookRegistered)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _sceneHookRegistered = false;
        }

        _logger.LogInfo("LongLive host bootstrap shutting down.");
        LongLiveBulkItemUseRuntime.OnPluginShutdown();
        _initialized = false;
        _runtimeInstallCompleted = false;
    }

    private void EnsureSceneHook()
    {
        if (_sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        _sceneHookRegistered = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LongLiveBulkItemUseRuntime.OnSceneLoaded(scene);
        LongLiveMapTraceRuntime.OnUnitySceneLoaded(scene, mode);
        LongLiveMapSnapshotRuntime.OnSceneLoaded(scene);

        if (_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo($"LongLive observed scene load: {scene.name}, nextAvailable={_runtime.IsAvailable}");
        }

        TryInstallRuntimeFeatures();
    }

    private void TryInstallRuntimeFeatures()
    {
        if (_runtimeInstallCompleted)
        {
            return;
        }

        if (_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo($"LongLive runtime install check: nextAvailable={_runtime.IsAvailable}");
        }

        if (!_runtime.IsAvailable)
        {
            if (_options.EnableDebugLogging.Value)
            {
                _logger.LogInfo("Next runtime is still unavailable. Runtime-dependent LongLive installers remain deferred.");
            }

            return;
        }

        _logger.LogInfo("Next runtime became available. Running LongLive installers.");
        RunInstallers();
        PublishHostHandshakeState();
        MarkBootstrapCompleted();
        _runtimeInstallCompleted = true;
    }

    private void RunInstallers()
    {
        foreach (var installer in _installers)
        {
            _logger.LogInfo($"Running installer: {installer.Name}");
            installer.Install();
        }
    }

    private void MarkBootstrapCompleted()
    {
        _runtime.SetInt(LongLiveStateKeys.BootstrapCompleted, 1);
        _runtime.SetString(LongLiveStateKeys.LastEventTag, "host-bootstrap");
        _logger.LogInfo("LongLive host bootstrap completed.");
    }

    private void PublishHostHandshakeState()
    {
        if (!_runtime.IsAvailable)
        {
            return;
        }

        if (!LongLivePluginContext.TryGetHostHandshake(out var handshake) || handshake is null)
        {
            _logger.LogWarning("LongLive host handshake was unavailable during state publication.");
            return;
        }

        _runtime.SetInt(LongLiveStateKeys.HostPresent, 1);
        _runtime.SetString(LongLiveStateKeys.HostPluginGuid, handshake.PluginGuid);
        _runtime.SetString(LongLiveStateKeys.HostPluginName, handshake.PluginName);
        _runtime.SetString(LongLiveStateKeys.HostVersion, handshake.PluginVersion);
        _runtime.SetString(LongLiveStateKeys.CurrentLocale, _runtime.Localization.GetCurrentLanguageDirectory() ?? string.Empty);
        _runtime.SetInt(LongLiveStateKeys.HostHandshakeVersion, handshake.HandshakeVersion);
        _runtime.SetString(LongLiveStateKeys.HostCapabilities, string.Join(",", handshake.Capabilities));
        _runtime.SetString(LongLiveStateKeys.HostInstallRoot, handshake.InstallRoot);
        _runtime.SetInt(LongLiveStateKeys.HostNextRuntimeAvailable, handshake.NextRuntimeAvailable ? 1 : 0);
        _runtime.SetString(LongLiveStateKeys.HostPublishedAtUtc, handshake.InitializedAtUtc.ToString("O"));

        if (_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo($"LongLive published host handshake state to Next. version={handshake.PluginVersion}, capabilities={string.Join(",", handshake.Capabilities)}");
        }
    }
}
