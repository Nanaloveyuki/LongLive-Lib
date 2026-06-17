using BepInEx.Logging;
using LongLive.Next.Abstractions.State;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveDemoInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;

    public LongLiveDemoInstaller(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        _logger = logger;
        _runtime = runtime;
        _options = options;
    }

    public string Name => "LongLiveDemoInstaller";

    public void Install()
    {
        if (_options.EnableDemoCommandRegistration.Value)
        {
            _runtime.CommandRegistry.Register(
                "LongLiveEcho",
                (context, complete) =>
                {
                    var text = context.GetString(0, string.Empty);
                    if (_options.EnableDebugLogging.Value)
                    {
                        _logger.LogInfo($"LongLiveEcho invoked with: {text}");
                    }

                    _runtime.SetString(LongLiveStateKeys.LastError, text);
                    complete();
                });

            _logger.LogInfo("Registered demo command: LongLiveEcho");
        }

        if (_options.EnableDemoQueryRegistration.Value)
        {
            _runtime.QueryRegistry.Register(
                "LongLiveDebugEnabled",
                _ => _runtime.GetInt(LongLiveStateKeys.DebugEnabled, 0));

            _logger.LogInfo("Registered demo query: LongLiveDebugEnabled");
        }
    }
}
