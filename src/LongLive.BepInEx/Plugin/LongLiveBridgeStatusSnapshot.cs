using System;
using LongLive.Next.Abstractions.State;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveBridgeStatusSnapshot
{
    private LongLiveBridgeStatusSnapshot(
        bool hasReport,
        string status,
        string detail,
        string hostVersion,
        bool hostPresent,
        bool reminderEnabled,
        bool hostCompatible,
        string compatibilityReason,
        int handshakeVersion,
        string capabilities)
    {
        HasReport = hasReport;
        Status = status;
        Detail = detail;
        HostVersion = hostVersion;
        HostPresent = hostPresent;
        ReminderEnabled = reminderEnabled;
        HostCompatible = hostCompatible;
        CompatibilityReason = compatibilityReason;
        HandshakeVersion = handshakeVersion;
        Capabilities = capabilities;
    }

    public bool HasReport { get; }

    public string Status { get; }

    public string Detail { get; }

    public string HostVersion { get; }

    public bool HostPresent { get; }

    public bool ReminderEnabled { get; }

    public bool HostCompatible { get; }

    public string CompatibilityReason { get; }

    public int HandshakeVersion { get; }

    public string Capabilities { get; }

    public static LongLiveBridgeStatusSnapshot FromRuntime(NextRuntimeFacade runtime)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        var status = runtime.GetString(LongLiveStateKeys.BridgeLastStatus, string.Empty);
        if (string.IsNullOrWhiteSpace(status))
        {
            return new LongLiveBridgeStatusSnapshot(false, string.Empty, string.Empty, string.Empty, false, true, false, string.Empty, 0, string.Empty);
        }

        return new LongLiveBridgeStatusSnapshot(
            true,
            status,
            runtime.GetString(LongLiveStateKeys.BridgeLastStatusDetail, string.Empty),
            runtime.GetString(LongLiveStateKeys.BridgeLastHostVersion, string.Empty),
            runtime.GetInt(LongLiveStateKeys.BridgeLastHostPresent, 0) == 1,
            runtime.GetInt(LongLiveStateKeys.BridgeLastMissingHostReminderEnabled, 1) == 1,
            runtime.GetInt(LongLiveStateKeys.BridgeLastHostCompatible, 0) == 1,
            runtime.GetString(LongLiveStateKeys.BridgeLastHostCompatibilityReason, string.Empty),
            runtime.GetInt(LongLiveStateKeys.BridgeLastHostHandshakeVersion, 0),
            runtime.GetString(LongLiveStateKeys.BridgeLastHostCapabilities, string.Empty));
    }
}
