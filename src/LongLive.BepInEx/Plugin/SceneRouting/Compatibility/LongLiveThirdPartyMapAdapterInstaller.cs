using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveThirdPartyMapAdapterInstaller : ILongLiveInstaller
{
    private static readonly ILongLiveThirdPartyMapDraftAdapter[] Adapters =
    {
        new LongLiveLingJieMapDraftAdapter(),
        new LongLiveJToolsMapDraftAdapter(),
    };

    private readonly ManualLogSource _logger;

    public LongLiveThirdPartyMapAdapterInstaller(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "third-party-map-adapter-installer";

    public void Install()
    {
        var registrationService = new LongLiveThirdPartyMapDraftRegistrationService(_logger);
        registrationService.TryRegisterPending(Adapters);
    }

    public static void RetryPending(ManualLogSource logger)
    {
        var registrationService = new LongLiveThirdPartyMapDraftRegistrationService(logger);
        registrationService.TryRegisterPending(Adapters);
    }
}
