namespace LongLive.Mods.Maps;

public sealed class LongLiveMapRegistryPlan
{
    public LongLiveMapRegistryPlan(
        LongLiveMapRegistryDraft draft,
        LongLiveMapRegistryValidationResult validation,
        LongLiveMapRegistrationReport allocations)
    {
        Draft = draft;
        Validation = validation;
        Allocations = allocations;
    }

    public LongLiveMapRegistryDraft Draft { get; }

    public LongLiveMapRegistryValidationResult Validation { get; }

    public LongLiveMapRegistrationReport Allocations { get; }
}
