using LongLive.Mods.Models;
using LongLive.Mods.Validation;

namespace LongLive.Mods;

public sealed class LongLiveModLoadReport
{
    public LongLiveModLoadReport(LongLiveModPackage package, LongLiveModValidationResult validation)
    {
        Package = package;
        Validation = validation;
    }

    public LongLiveModPackage Package { get; }

    public LongLiveModValidationResult Validation { get; }
}
