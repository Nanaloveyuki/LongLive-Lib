namespace LongLive.Next.Abstractions.State;

public static class LongLiveStateKeys
{
    public const string CurrentLocale = "longlive.current_locale";

    public const string DebugEnabled = "longlive.debug_enabled";

    public const string HostPresent = "longlive.host.present";

    public const string HostPluginGuid = "longlive.host.plugin_guid";

    public const string HostPluginName = "longlive.host.plugin_name";

    public const string HostVersion = "longlive.host.version";

    public const string HostHandshakeVersion = "longlive.host.handshake_version";

    public const string HostCapabilities = "longlive.host.capabilities";

    public const string HostInstallRoot = "longlive.host.install_root";

    public const string HostNextRuntimeAvailable = "longlive.host.next_runtime_available";

    public const string HostPublishedAtUtc = "longlive.host.published_at_utc";

    public const string BridgeLastStatus = "longlive.bridge.last_status";

    public const string BridgeLastStatusDetail = "longlive.bridge.last_status_detail";

    public const string BridgeLastHostVersion = "longlive.bridge.last_host_version";

    public const string BridgeLastHostPresent = "longlive.bridge.last_host_present";

    public const string BridgeLastMissingHostReminderEnabled = "longlive.bridge.last_missing_host_reminder_enabled";

    public const string BridgeLastHostCompatible = "longlive.bridge.last_host_compatible";

    public const string BridgeLastHostCompatibilityReason = "longlive.bridge.last_host_compatibility_reason";

    public const string BridgeLastHostHandshakeVersion = "longlive.bridge.last_host_handshake_version";

    public const string BridgeLastHostCapabilities = "longlive.bridge.last_host_capabilities";

    public const string BootstrapCompleted = "longlive.bootstrap_completed";

    public const string LastError = "longlive.last_error";

    public const string LastEventTag = "longlive.last_event_tag";
}
