using System;
using System.Collections.Generic;
using System.Linq;
using LongLive.BepInEx.Native;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMainMenuPanelContentBuilder
{
    public static string BuildOverviewText(LongLiveTextLocalizer localizer, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        var bridgeStatus = LongLiveBridgeStatusSnapshot.FromRuntime(runtime);
        LongLivePluginContext.TryGetHostHandshake(out var handshake);
        var bridgeReported = bridgeStatus.HasReport;
        return JoinBlocks(
            localizer.Get("panel.overview.intro"),
            BuildSection(localizer.Get("panel.section.runtime"), new[]
            {
                FormatField(localizer.Get("diagnostics.plugin"), $"{LongLivePluginMetadata.PluginName} {LongLivePluginMetadata.PluginVersion}"),
                FormatField(localizer.Get("diagnostics.host_handshake_available"), FormatBoolean(handshake is not null)),
                FormatField(localizer.Get("diagnostics.next_runtime_available"), FormatBoolean(runtime.IsAvailable)),
                FormatField(localizer.Get("diagnostics.content_backend"), FormatStatusOrCode(options.ContentBackend.Value)),
                FormatField(localizer.Get("diagnostics.bridge_status"), FormatStatus(bridgeReported ? bridgeStatus.Status : localizer.Get("common.not_reported_yet"))),
                !bridgeReported ? FormatHint(localizer.Get("diagnostics.bridge_not_reported_explainer")) : string.Empty,
            }),
            BuildSection(localizer.Get("panel.section.compatibility"), new[]
            {
                FormatField("EasyBatch", FormatToggleState(localizer, options.EnableEasyBatchCompatibility.Value)),
                FormatField("WhiteZe Tools", FormatToggleState(localizer, options.EnableWhiteZeCompatibility.Value)),
                FormatField("VTools", FormatToggleState(localizer, options.EnableVToolsCompatibility.Value)),
            }));
    }

    public static string BuildCompatibilityText(LongLiveTextLocalizer localizer, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        var snapshot = LongLivePluginContext.GetCompatibilitySnapshot();
        var detailLines = snapshot.Activations.Count == 0
            ? new[] { FormatMuted(localizer.Get("common.not_reported_yet")) }
            : snapshot.Activations
                .OrderBy(static activation => activation.RedirectId, StringComparer.Ordinal)
                .Select(activation => BuildCompatibilityDetailLine(activation.RedirectId, activation.RedirectEnabled, activation.RedirectApplied, activation.StatusCode, activation.InvocationCount))
                .ToArray();

        return JoinBlocks(
            localizer.Get("compatibility.menu.summary"),
            BuildSection(localizer.Get("panel.section.compatibility"), new[]
            {
                FormatField("EasyBatch", FormatToggleState(localizer, options.EnableEasyBatchCompatibility.Value)),
                FormatField("WhiteZe Tools", FormatToggleState(localizer, options.EnableWhiteZeCompatibility.Value)),
                FormatField("VTools", FormatToggleState(localizer, options.EnableVToolsCompatibility.Value)),
            }),
            BuildSection(localizer.Get("panel.compatibility.details"), detailLines),
            FormatMuted(localizer.Get("panel.compatibility.hint")));
    }

    public static string BuildDiagnosticsText(LongLiveTextLocalizer localizer, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        var report = runtime.ContentInspector.Inspect();
        var nativeProbe = LongLivePlugin.Instance?.Native.CurrentProbeResult ?? LongLiveNativeProbeResult.Disabled();
        var snapshotService = new LongLiveMapSnapshotExportService();
        var snapshotExport = snapshotService.ExportCurrentSnapshot();
        var snapshot = snapshotService.CaptureCurrentSnapshot();
        var compatibility = LongLivePluginContext.GetCompatibilitySnapshot();
        var compatibilityStatus = compatibility.Activations.Count > 0
            ? string.Join(", ", compatibility.Activations.Select(static activation => activation.RedirectId + "=" + activation.StatusCode + "(" + activation.InvocationCount + ")"))
            : localizer.Get("common.not_reported_yet");
        var bridgeStatus = LongLiveBridgeStatusSnapshot.FromRuntime(runtime);
        LongLivePluginContext.TryGetHostHandshake(out var handshake);
        var bridgeReported = bridgeStatus.HasReport;
        var localModsResolved = report.Capabilities.CanResolveLocalModsDirectory;
        var localModsDirectory = localModsResolved ? localizer.GetOrNa(report.LocalModsDirectory) : localizer.Get("common.na");

        return JoinBlocks(
            BuildSection(localizer.Get("panel.section.runtime"), new[]
            {
                FormatField(localizer.Get("diagnostics.plugin"), $"{LongLivePluginMetadata.PluginName} {LongLivePluginMetadata.PluginVersion}"),
                FormatField(localizer.Get("diagnostics.host_handshake_available"), FormatBoolean(handshake is not null)),
                FormatField(localizer.Get("diagnostics.host_handshake_version"), FormatCode(handshake?.HandshakeVersion.ToString() ?? localizer.Get("common.na"))),
                FormatField(localizer.Get("diagnostics.host_install_root"), FormatPath(localizer.GetOrNa(handshake?.InstallRoot))),
                FormatField(localizer.Get("diagnostics.host_capabilities"), FormatWrappedCsv(handshake is null ? localizer.Get("common.na") : string.Join(", ", handshake.Capabilities))),
                FormatField(localizer.Get("diagnostics.next_runtime_available"), FormatBoolean(runtime.IsAvailable)),
                FormatField(localizer.Get("diagnostics.content_inspection_available"), FormatBoolean(report.IsAvailable)),
                FormatField(localizer.Get("diagnostics.local_mods_resolved"), FormatBoolean(localModsResolved)),
                !localModsResolved ? FormatHint(localizer.Get("diagnostics.local_mods_unresolved_explainer")) : string.Empty,
                localModsResolved ? FormatField(localizer.Get("diagnostics.local_mods_directory"), FormatPath(localModsDirectory)) : string.Empty,
                FormatField(localizer.Get("diagnostics.content_backend"), FormatStatusOrCode(options.ContentBackend.Value)),
                options.ContentBackend.Value.Equals("Deferred", StringComparison.OrdinalIgnoreCase) ? FormatHint(localizer.Get("diagnostics.content_backend_deferred_explainer")) : string.Empty,
            }),
            BuildSection(localizer.Get("panel.section.bridge"), new[]
            {
                FormatField(localizer.Get("diagnostics.bridge_reported"), FormatBoolean(bridgeReported)),
                !bridgeReported ? FormatHint(localizer.Get("diagnostics.bridge_not_reported_explainer")) : string.Empty,
                FormatField(localizer.Get("diagnostics.bridge_status"), FormatStatus(bridgeReported ? bridgeStatus.Status : localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_status_detail"), FormatStatusOrWrappedText(bridgeReported ? bridgeStatus.Detail : localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_host_version"), bridgeReported ? FormatCode(localizer.GetOrNa(bridgeStatus.HostVersion)) : FormatStatus(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_host_present"), bridgeReported ? FormatBoolean(bridgeStatus.HostPresent) : FormatStatus(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_host_compatible"), bridgeReported ? FormatBoolean(bridgeStatus.HostCompatible) : FormatStatus(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_host_reason"), FormatStatusOrWrappedText(bridgeReported ? localizer.GetOrNa(bridgeStatus.CompatibilityReason) : localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_host_handshake_version"), bridgeReported ? FormatCode(bridgeStatus.HandshakeVersion.ToString()) : FormatStatus(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_host_capabilities"), bridgeReported ? FormatWrappedCsv(localizer.GetOrNa(bridgeStatus.Capabilities)) : FormatStatus(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.bridge_host_reminder"), bridgeReported ? FormatBoolean(bridgeStatus.ReminderEnabled) : FormatStatus(localizer.Get("common.not_reported_yet"))),
            }),
            BuildSection(localizer.Get("panel.section.compatibility"), new[]
            {
                FormatField(localizer.Get("diagnostics.compatibility_library_count"), FormatNumber(compatibility.Libraries.Count)),
                FormatField(localizer.Get("diagnostics.compatibility_redirect_count"), FormatNumber(compatibility.Redirects.Count)),
                FormatField(localizer.Get("diagnostics.compatibility_activation_count"), FormatNumber(compatibility.Activations.Count)),
                FormatField(localizer.Get("diagnostics.compatibility_toggles"), JoinInlineValues(
                    FormatCode("EasyBatch") + "=" + FormatToggleState(localizer, options.EnableEasyBatchCompatibility.Value),
                    FormatCode("WhiteZe") + "=" + FormatToggleState(localizer, options.EnableWhiteZeCompatibility.Value),
                    FormatCode("VTools") + "=" + FormatToggleState(localizer, options.EnableVToolsCompatibility.Value))),
                FormatField(localizer.Get("diagnostics.compatibility_status"), compatibility.Activations.Count > 0
                    ? BuildCompatibilityStatusSummary(compatibility.Activations.Select(static activation => (activation.RedirectId, activation.StatusCode, activation.InvocationCount)))
                    : FormatStatus(compatibilityStatus)),
            }),
            BuildSection(localizer.Get("panel.section.maps"), new[]
            {
                FormatField(localizer.Get("diagnostics.map_snapshot_scenes"), FormatNumber(snapshot.Scenes.Count)),
                FormatField(localizer.Get("diagnostics.map_snapshot_pages"), FormatNumber(snapshot.Pages.Count)),
                FormatField(localizer.Get("diagnostics.map_snapshot_highlights"), FormatNumber(snapshot.HighlightRegions.Count)),
                FormatField(localizer.Get("diagnostics.map_snapshot_nodes"), FormatNumber(snapshot.Nodes.Count)),
                FormatField(localizer.Get("diagnostics.map_snapshot_export_success"), FormatBoolean(snapshotExport.Success)),
                FormatField(localizer.Get("diagnostics.map_snapshot_export_path"), snapshotExport.Success ? FormatPath(snapshotExport.Path) : FormatCode(localizer.Get("common.na"))),
                FormatField(localizer.Get("diagnostics.map_snapshot_export_summary"), FormatStatusOrWrappedText(snapshotExport.Summary)),
            }),
            BuildSection(localizer.Get("panel.section.native"), new[]
            {
                FormatField(localizer.Get("diagnostics.native_probe_enabled"), FormatBoolean(nativeProbe.Enabled)),
                FormatField(localizer.Get("diagnostics.native_probe_success"), FormatBoolean(nativeProbe.Success)),
                FormatField(localizer.Get("diagnostics.native_probe_summary"), FormatStatusOrWrappedText(nativeProbe.Summary)),
                FormatField(localizer.Get("diagnostics.native_probe_abi"), FormatCode(nativeProbe.AbiVersion?.ToString() ?? localizer.Get("common.na"))),
                FormatField(localizer.Get("diagnostics.native_probe_ready"), FormatCode(nativeProbe.ReadyFlag?.ToString() ?? localizer.Get("common.na"))),
                FormatField(localizer.Get("diagnostics.native_probe_sample_damage"), FormatCode(nativeProbe.TurnDamage?.ToString() ?? localizer.Get("common.na"))),
            }));
    }

    private static string BuildSection(string title, IEnumerable<string> lines)
    {
        return string.Join("\n", new[]
        {
            $"<b><color=#D8E6FF>{title}</color></b>",
            string.Join("\n", lines),
        });
    }

    private static string BuildCompatibilityDetailLine(string redirectId, bool enabled, bool applied, string statusCode, int invocationCount)
    {
        return string.Join("\n", new[]
        {
            $"<b>{FormatCode(redirectId)}</b>",
            $"  {FormatCode("enabled")}: {FormatBoolean(enabled)}",
            $"  {FormatCode("applied")}: {FormatBoolean(applied)}",
            $"  {FormatCode("status")}: {FormatStatus(statusCode)}",
            $"  {FormatCode("calls")}: {FormatNumber(invocationCount)}",
        });
    }

    private static string BuildCompatibilityStatusSummary(IEnumerable<(string RedirectId, string StatusCode, int InvocationCount)> activations)
    {
        return string.Join(",\n  ", activations.Select(static activation =>
            FormatCode(activation.RedirectId) + "=" + FormatStatus(activation.StatusCode) + "(" + FormatNumber(activation.InvocationCount) + ")"));
    }

    private static string FormatField(string label, string value)
    {
        return $"<b>{label}</b>: {value}";
    }

    private static string FormatBoolean(bool value)
    {
        return value
            ? "<color=#79D38A>true</color>"
            : "<color=#E88383>false</color>";
    }

    private static string FormatToggleState(LongLiveTextLocalizer localizer, bool enabled)
    {
        return enabled
            ? $"<color=#79D38A>{localizer.Get("common.enabled")}</color>"
            : $"<color=#E88383>{localizer.Get("common.disabled")}</color>";
    }

    private static string FormatStatus(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FormatMuted("n/a");
        }

        var normalized = value.Trim();
        if (normalized.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return FormatBoolean(true);
        }

        if (normalized.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return FormatBoolean(false);
        }

        if (normalized.IndexOf("尚未上报", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("未上报", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("未启用", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("跳过", StringComparison.Ordinal) >= 0)
        {
            return FormatMuted(normalized);
        }

        if (normalized.IndexOf("错误", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("缺失", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("不兼容", StringComparison.Ordinal) >= 0)
        {
            return $"<color=#E8A16D>{EscapeRichText(normalized)}</color>";
        }

        if (normalized.IndexOf("成功", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("已安装", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("已启用", StringComparison.Ordinal) >= 0)
        {
            return $"<color=#79D38A>{EscapeRichText(normalized)}</color>";
        }

        if (normalized.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("incompatible", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return $"<color=#E8A16D>{EscapeRichText(normalized)}</color>";
        }

        if (normalized.IndexOf("installed", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("enabled", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("present", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return $"<color=#79D38A>{EscapeRichText(normalized)}</color>";
        }

        if (normalized.IndexOf("not reported", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("deferred", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("skipped", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return FormatMuted(normalized);
        }

        return FormatCode(normalized);
    }

    private static string FormatStatusOrCode(string value)
    {
        return LooksLikeStatusToken(value) ? FormatStatus(value) : FormatCode(value);
    }

    private static string FormatStatusOrWrappedText(string value)
    {
        return LooksLikeStatusToken(value) ? FormatStatus(value) : FormatWrappedText(value);
    }

    private static string FormatCode(string value)
    {
        return $"<color=#BBD4FF>{EscapeRichText(value)}</color>";
    }

    private static string FormatNumber(int value)
    {
        return $"<color=#F2D38A>{value}</color>";
    }

    private static string FormatPath(string value)
    {
        return $"<color=#8FD6D2>{EscapeRichText(value)}</color>";
    }

    private static string FormatMuted(string value)
    {
        return $"<color=#9CA8BC>{EscapeRichText(value)}</color>";
    }

    private static string FormatHint(string value)
    {
        return $"<color=#9CA8BC>{EscapeRichText(value)}</color>";
    }

    private static string FormatWrappedText(string value)
    {
        return EscapeRichText(value).Replace("\r\n", "\n").Replace("\n", "\n  ");
    }

    private static string FormatWrappedCsv(string value)
    {
        var normalized = EscapeRichText(value);
        return normalized.Replace(", ", ",\n  ");
    }

    private static string JoinInlineValues(params string[] values)
    {
        return string.Join("  |  ", values);
    }

    private static string JoinBlocks(params string[] blocks)
    {
        return string.Join("\n\n", blocks.Where(static block => !string.IsNullOrWhiteSpace(block)));
    }

    private static bool LooksLikeStatusToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        if (normalized.IndexOf('\n') >= 0 || normalized.IndexOf('\r') >= 0)
        {
            return false;
        }

        return normalized.Length <= 48 &&
               normalized.IndexOf(' ') < 0 &&
               normalized.IndexOf(':') < 0 &&
               normalized.IndexOf('\\') < 0 &&
               normalized.IndexOf('/') < 0;
    }

    private static string EscapeRichText(string value)
    {
        return (value ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
