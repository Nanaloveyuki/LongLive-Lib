using System;
using System.Collections.Generic;
using BepInEx.Logging;
using LongLive.Next.Abstractions.State;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveBootstrapper
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;
    private readonly IReadOnlyList<ILongLiveInstaller> _installers;
    private bool _initialized;

    public LongLiveBootstrapper(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _installers = new ILongLiveInstaller[]
        {
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

        if (!_runtime.IsAvailable)
        {
            _logger.LogWarning("Next runtime types are not available in the current AppDomain. Deferred integration is expected until the host loads Next.");
            return;
        }

        RunInstallers();
        MarkBootstrapCompleted();
        _initialized = true;
    }

    public void Shutdown()
    {
        if (!_initialized)
        {
            return;
        }

        _logger.LogInfo("LongLive host bootstrap shutting down.");
        _initialized = false;
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
}
