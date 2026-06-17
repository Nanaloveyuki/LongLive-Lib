namespace LongLive.Mods.Installation;

public static class LongLiveContentInstallReasonCode
{
    public const string None = "none";
    public const string DeferredBackend = "deferred_backend";
    public const string NextRuntimeUnavailable = "next_runtime_unavailable";
    public const string NextLifecycleUnavailable = "next_lifecycle_unavailable";
    public const string NextResourcesUnavailable = "next_resources_unavailable";
    public const string NextResourcePatchUnavailable = "next_resource_patch_unavailable";
    public const string AssetSourceMissing = "asset_source_missing";
    public const string NextPreflightDeferred = "next_preflight_deferred";
}
