using System;
using BepInEx.Configuration;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin.Configuration;

internal sealed class LongLiveLocalizedConfigBinder
{
    private readonly ConfigFile _config;
    private readonly LongLiveTextLocalizer _localizer;

    public LongLiveLocalizedConfigBinder(ConfigFile config, NextRuntimeFacade runtime)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _localizer = new LongLiveTextLocalizer(runtime ?? throw new ArgumentNullException(nameof(runtime)));
    }

    public ConfigEntry<T> Bind<T>(
        string section,
        string key,
        T defaultValue,
        string categoryKey,
        string displayNameKey,
        string descriptionKey,
        int order,
        AcceptableValueBase? acceptableValues = null)
    {
        var description = new ConfigDescription(
            _localizer.Get(descriptionKey),
            acceptableValues,
            LongLiveConfigurationManagerTagFactory.Create(
                _localizer.Get(displayNameKey),
                _localizer.Get(categoryKey),
                order));

        return _config.Bind(new ConfigDefinition(section, key), defaultValue, description);
    }
}
