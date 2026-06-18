using LongLive.Mods.Models;
using LongLive.Mods.Maps;
using LongLive.Mods.Parsing;
using LongLive.Mods.Validation;

namespace LongLive.Mods;

public sealed class LongLiveModToolkit
{
    public LongLiveModToolkit()
    {
        Loader = new LongLiveModLoader();
        Validator = new LongLiveModValidator();
        MapRegistryPlanner = new LongLiveMapRegistryPlanner();
    }

    public LongLiveModLoader Loader { get; }

    public LongLiveModValidator Validator { get; }

    public LongLiveMapRegistryPlanner MapRegistryPlanner { get; }

    public LongLiveModLoadReport LoadAndValidate(string modDirectoryPath)
    {
        var package = Loader.LoadFromDirectory(modDirectoryPath);
        var validation = Validator.Validate(package);
        return new LongLiveModLoadReport(package, validation);
    }

    public LongLiveMapRegistryPlan PlanSampleSeaIslandMap(string modId = "longlive.sample")
    {
        var draft = LongLiveMapRegistrySamples.CreateSeaIslandSample(modId);
        return MapRegistryPlanner.CreatePlan(draft);
    }
}
