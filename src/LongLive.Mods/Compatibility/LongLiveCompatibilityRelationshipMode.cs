namespace LongLive.Mods.Compatibility;

public enum LongLiveCompatibilityRelationshipMode
{
    ReferenceOnly = 0,
    CapabilityCompatible = 1,
    AdapterCompatible = 2,
    ChokePointRedirect = 3,
    FoundationDependency = 4,
    BridgeDependency = 5,
}
