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

    public static string BuildDemoMapText(LongLiveTextLocalizer localizer, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        var snapshot = new LongLiveMapDemoRuntimeService(LongLivePluginContext.GetLogger(), runtime).CaptureSnapshot(options);

        return JoinBlocks(
            localizer.Get("panel.demo_map.intro"),
            BuildSection(localizer.Get("panel.section.demo_map"), new[]
            {
                FormatField(localizer.Get("demo_map.registration_enabled"), FormatBoolean(snapshot.RegistrationEnabled)),
                FormatField(localizer.Get("demo_map.registered"), FormatBoolean(snapshot.Registered)),
                FormatField(localizer.Get("demo_map.registered_in_routing"), FormatBoolean(snapshot.RegisteredOwnerModInRouting)),
                FormatField(localizer.Get("demo_map.route_kind"), string.IsNullOrWhiteSpace(snapshot.RouteKind) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrCode(snapshot.RouteKind)),
                FormatField(localizer.Get("demo_map.resolve_status"), string.IsNullOrWhiteSpace(snapshot.ResolveStatus) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrWrappedText(snapshot.ResolveStatus)),
                FormatField(localizer.Get("demo_map.warp_status"), string.IsNullOrWhiteSpace(snapshot.WarpStatus) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrWrappedText(snapshot.WarpStatus)),
                FormatField(localizer.Get("demo_map.overview_node_status"), string.IsNullOrWhiteSpace(snapshot.OverviewNodeStatus) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrWrappedText(snapshot.OverviewNodeStatus)),
                FormatField(localizer.Get("demo_map.custom_page_status"), string.IsNullOrWhiteSpace(snapshot.CustomPageStatus) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrWrappedText(snapshot.CustomPageStatus)),
                FormatField(localizer.Get("diagnostics.map_overview_custom_pages"), string.IsNullOrWhiteSpace(snapshot.CustomPageRuntimeSummary) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrWrappedText(snapshot.CustomPageRuntimeSummary)),
                FormatField(localizer.Get("demo_map.planning_dump_status"), string.IsNullOrWhiteSpace(snapshot.PlanningDumpStatus) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrWrappedText(snapshot.PlanningDumpStatus)),
                FormatField(localizer.Get("demo_map.plan_summary"), string.IsNullOrWhiteSpace(snapshot.PlanSummary) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatWrappedText(snapshot.PlanSummary)),
                FormatField(localizer.Get("demo_map.topology_summary"), string.IsNullOrWhiteSpace(snapshot.TopologySummary) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatWrappedText(snapshot.TopologySummary)),
                FormatField(localizer.Get("demo_map.node_summary"), string.IsNullOrWhiteSpace(snapshot.NodeSummary) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatWrappedText(snapshot.NodeSummary)),
            }),
            BuildSection(localizer.Get("demo_map.identity_section"), new[]
            {
                FormatField(localizer.Get("demo_map.owner_mod"), FormatCode(LongLiveMapDemoConstants.OwningModId)),
                FormatField(localizer.Get("demo_map.page_id"), FormatWrappedCsv(string.Join(", ", new[] { LongLiveMapDemoConstants.PageId, LongLiveMapDemoConstants.SecondPageId }))),
                FormatField(localizer.Get("demo_map.region_id"), FormatWrappedCsv(string.Join(", ", new[] { LongLiveMapDemoConstants.RegionId, LongLiveMapDemoConstants.SecondRegionId }))),
                FormatField(localizer.Get("demo_map.world_node_id"), FormatWrappedCsv(string.Join(", ", new[] { LongLiveMapDemoConstants.WorldNodeId, LongLiveMapDemoConstants.SecondWorldNodeId }))),
                FormatField(localizer.Get("demo_map.outer_scene"), FormatCode(LongLiveMapDemoConstants.OuterSceneId + " (" + LongLiveMapDemoConstants.OuterSceneName + ")")),
                FormatField(localizer.Get("demo_map.custom_scene"), FormatWrappedCsv(string.Join(", ", new[]
                {
                    LongLiveMapDemoConstants.CustomSceneId + " (" + LongLiveMapDemoConstants.CustomSceneName + ")",
                    LongLiveMapDemoConstants.SecondCustomSceneId + " (demo placeholder: routes to " + LongLiveMapDemoConstants.CustomSceneName + ")",
                }))),
                FormatField(localizer.Get("demo_map.topology_id"), FormatWrappedCsv(string.Join(", ", new[] { LongLiveMapDemoConstants.TopologyId, LongLiveMapDemoConstants.SecondTopologyId }))),
            }),
            BuildSection(localizer.Get("demo_map.integration_section"), new[]
            {
                FormatField(localizer.Get("diagnostics.scene_routing_route_count"), FormatNumber(snapshot.RouteCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_projection_count"), FormatNumber(snapshot.ProjectionCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_runtime_scene_count"), FormatNumber(snapshot.RuntimeSceneCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_runtime_bootstrap_count"), FormatNumber(snapshot.RuntimeBootstrapCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_registered_mod_count"), FormatNumber(snapshot.RegisteredModCount)),
                FormatField(localizer.Get("diagnostics.map_overview_active_projection"), FormatBoolean(snapshot.HasActiveProjection)),
                FormatField(localizer.Get("diagnostics.custom_runtime_bootstrap_available"), FormatBoolean(snapshot.HasRuntimeBootstrap)),
                FormatField(localizer.Get("diagnostics.map_overview_bindable_targets"), FormatNumber(snapshot.BindableTargetCount)),
                FormatField(localizer.Get("diagnostics.map_overview_dedicated_shell_targets"), FormatNumber(snapshot.DedicatedShellCount)),
                FormatField(localizer.Get("diagnostics.map_overview_reserved_shell_targets"), FormatNumber(snapshot.ReservedShellCount)),
                FormatField(localizer.Get("diagnostics.map_overview_external_pages"), FormatNumber(snapshot.CustomPageTargetCount)),
                FormatField(localizer.Get("diagnostics.map_overview_host_runtime_targets"), JoinInlineValues(
                    FormatCode("tabs") + "=" + FormatNumber(snapshot.CustomPageTabCount),
                    FormatCode("roots") + "=" + FormatNumber(snapshot.CustomPageRootCount),
                    FormatCode("regions") + "=" + FormatNumber(snapshot.CustomPageRegionOverlayCount),
                    FormatCode("nodes") + "=" + FormatNumber(snapshot.CustomPageRenderedNodeCount))),
                FormatField(localizer.Get("demo_map.hidden_reservations"), FormatNumber(snapshot.HiddenReservationCount)),
                FormatField(localizer.Get("diagnostics.scene_local_topology_count"), FormatNumber(snapshot.TopologyCount)),
                FormatField(localizer.Get("diagnostics.scene_local_node_count"), FormatNumber(snapshot.TopologyNodeCount)),
            }),
            FormatHint(localizer.Get("panel.demo_map.hint")));
    }

    public static string BuildDiagnosticsText(LongLiveTextLocalizer localizer, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        var report = runtime.ContentInspector.Inspect();
        var nativeProbe = LongLivePlugin.Instance?.Native.CurrentProbeResult ?? LongLiveNativeProbeResult.Disabled();
        var snapshotService = new LongLiveMapSnapshotExportService();
        var snapshotExport = snapshotService.ExportCurrentSnapshot();
        var snapshot = snapshotService.CaptureCurrentSnapshot();
        var mapOverviewSnapshot = LongLivePluginContext.GetMapOverviewRuntimeSnapshot();
        var mapOverviewInstallPlan = LongLivePluginContext.GetMapOverviewInstallPlan();
        var mapOverviewExecutionReport = LongLivePluginContext.GetMapOverviewExecutionReport();
        var mapOverviewHostBindingRuntime = LongLivePluginContext.GetMapOverviewHostBindingRuntimeSnapshot();
        var mapOverviewShellAllocationRuntime = LongLivePluginContext.GetMapOverviewShellAllocationRuntimeSnapshot();
        var mapOverviewShellReservationRuntime = LongLivePluginContext.GetMapOverviewShellReservationRuntimeSnapshot();
        var mapOverviewCustomPageRuntime = LongLivePluginContext.GetMapOverviewCustomPageRuntimeSnapshot();
        var customRuntimeSnapshot = LongLivePluginContext.GetCustomMapRuntimeStateSnapshot();
        var customRuntimeActivationPlan = LongLivePluginContext.GetCustomMapRuntimeActivationPlan();
        var customRuntimeActivationRuntime = LongLivePluginContext.GetCustomMapRuntimeActivationRuntimeSnapshot();
        var customRuntimeActivationArtifacts = LongLivePluginContext.GetCustomMapRuntimeActivationArtifactSnapshot();
        var customRuntimeActivationExecutionPlan = LongLivePluginContext.GetCustomMapRuntimeActivationExecutionPlan();
        var customRuntimeActivationReport = LongLivePluginContext.GetCustomMapRuntimeActivationExecutionReport();
        var customRuntimeExecutionReport = LongLivePluginContext.GetCustomMapRuntimeExecutionReport();
        var topologySnapshot = LongLivePluginContext.GetSceneLocalTopologyRuntimeSnapshot();
        var registrationSnapshot = LongLivePluginContext.GetSceneRoutingRegistrationSnapshot();
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
                FormatField(localizer.Get("diagnostics.scene_routing_route_count"), FormatNumber(registrationSnapshot.RouteCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_projection_count"), FormatNumber(registrationSnapshot.RouteProjectionCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_runtime_scene_count"), FormatNumber(registrationSnapshot.CustomRuntimeSceneCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_runtime_bootstrap_count"), FormatNumber(registrationSnapshot.CustomRuntimeBootstrapCount)),
                FormatField(localizer.Get("diagnostics.scene_routing_registered_mod_count"), FormatNumber(registrationSnapshot.OwningModIds.Count)),
                FormatField(localizer.Get("diagnostics.scene_routing_active_scene_registered"), FormatBoolean(registrationSnapshot.HasActiveSceneRegistration)),
                FormatField(localizer.Get("diagnostics.scene_routing_active_scene_logical_id"), registrationSnapshot.HasActiveSceneRegistration ? FormatCode(localizer.GetOrNa(registrationSnapshot.ActiveSceneLogicalId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.scene_routing_active_scene_mod_id"), registrationSnapshot.HasActiveSceneRegistration ? FormatCode(localizer.GetOrNa(registrationSnapshot.ActiveSceneOwningModId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.scene_routing_active_overview_page"), registrationSnapshot.HasActiveSceneRegistration ? FormatCode(localizer.GetOrNa(registrationSnapshot.ActiveOverviewPageId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.scene_routing_active_highlight_region"), registrationSnapshot.HasActiveSceneRegistration ? FormatCode(localizer.GetOrNa(registrationSnapshot.ActiveHighlightRegionId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.scene_routing_route_kinds"), registrationSnapshot.RouteKindCounts.Count > 0 ? BuildCountSummary(registrationSnapshot.RouteKindCounts) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.scene_routing_registered_mods"), registrationSnapshot.OwningModIds.Count > 0 ? BuildModRegistrationSummary(registrationSnapshot) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.map_overview_active_projection"), FormatBoolean(mapOverviewSnapshot.HasActiveProjection)),
                FormatField(localizer.Get("diagnostics.map_overview_active_page"), mapOverviewSnapshot.HasActiveProjection ? FormatCode(localizer.GetOrNa(mapOverviewSnapshot.ActivePageId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.map_overview_active_region"), mapOverviewSnapshot.HasActiveProjection ? FormatCode(localizer.GetOrNa(mapOverviewSnapshot.ActiveRegionId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.map_overview_external_pages"), FormatNumber(mapOverviewInstallPlan.ExternalPageTargetCount)),
                FormatField(localizer.Get("diagnostics.map_overview_external_projections"), FormatNumber(mapOverviewInstallPlan.ExternalProjectionCount)),
                FormatField(localizer.Get("diagnostics.map_overview_bindable_targets"), FormatNumber(mapOverviewHostBindingRuntime.BindableTargetCount)),
                FormatField(localizer.Get("diagnostics.map_overview_missing_anchor_targets"), FormatNumber(mapOverviewHostBindingRuntime.MissingAnchorTargetCount)),
                FormatField(localizer.Get("diagnostics.map_overview_reuse_shell_targets"), FormatNumber(mapOverviewShellAllocationRuntime.ReuseExistingShellCount)),
                FormatField(localizer.Get("diagnostics.map_overview_dedicated_shell_targets"), FormatNumber(mapOverviewShellAllocationRuntime.DedicatedShellCount)),
                FormatField(localizer.Get("diagnostics.map_overview_reserved_shell_targets"), FormatNumber(mapOverviewShellReservationRuntime.CreatedReservationCount)),
                FormatField(localizer.Get("diagnostics.map_overview_preflight"), FormatExecutionSummary(mapOverviewExecutionReport.SuccessCount, mapOverviewExecutionReport.FailureCount)),
                FormatField(localizer.Get("diagnostics.map_overview_host_probe"), BuildMapOverviewProbeSummary(mapOverviewExecutionReport.HostProbe)),
                FormatField(localizer.Get("diagnostics.map_overview_host_runtime_targets"), BuildMapOverviewHostRuntimeTargetSummary(mapOverviewHostBindingRuntime)),
                FormatField(localizer.Get("diagnostics.map_overview_shell_runtime_targets"), BuildMapOverviewShellRuntimeTargetSummary(mapOverviewShellAllocationRuntime)),
                FormatField(localizer.Get("diagnostics.map_overview_shell_reservation_targets"), BuildMapOverviewShellReservationSummary(mapOverviewShellReservationRuntime)),
                FormatField(localizer.Get("diagnostics.map_overview_custom_pages"), BuildMapOverviewCustomPageSummary(mapOverviewCustomPageRuntime)),
                FormatField(localizer.Get("diagnostics.map_overview_host_binding_samples"), BuildMapOverviewBindingSummary(mapOverviewExecutionReport)),
                FormatField(localizer.Get("diagnostics.map_overview_active_page_nodes"), mapOverviewSnapshot.ActivePageNodeNames.Count > 0 ? FormatWrappedCsv(string.Join(", ", mapOverviewSnapshot.ActivePageNodeNames.ToArray())) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.map_overview_active_region_nodes"), mapOverviewSnapshot.ActiveRegionNodeNames.Count > 0 ? FormatWrappedCsv(string.Join(", ", mapOverviewSnapshot.ActiveRegionNodeNames.ToArray())) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.custom_runtime_scene_registered"), FormatBoolean(customRuntimeSnapshot.HasRegisteredScene)),
                FormatField(localizer.Get("diagnostics.custom_runtime_bootstrap_available"), FormatBoolean(customRuntimeSnapshot.HasRuntimeBootstrap)),
                FormatField(localizer.Get("diagnostics.custom_runtime_external_targets"), FormatNumber(customRuntimeActivationPlan.ExternalActivationTargetCount)),
                FormatField(localizer.Get("diagnostics.custom_runtime_has_active_target"), FormatBoolean(customRuntimeActivationPlan.HasActiveTarget)),
                FormatField(localizer.Get("diagnostics.custom_runtime_ready_targets"), FormatNumber(customRuntimeSnapshot.Readiness.ReadyTargetCount)),
                FormatField(localizer.Get("diagnostics.custom_runtime_blocked_targets"), FormatNumber(customRuntimeSnapshot.Readiness.BlockedTargetCount)),
                FormatField(localizer.Get("diagnostics.custom_runtime_activation_pending_targets"), FormatNumber(customRuntimeSnapshot.Readiness.ActivationPendingTargetCount)),
                FormatField(localizer.Get("diagnostics.custom_runtime_activation_state"), string.IsNullOrWhiteSpace(customRuntimeActivationRuntime.ActiveActivationState) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrCode(customRuntimeActivationRuntime.ActiveActivationState)),
                FormatField(localizer.Get("diagnostics.custom_runtime_activation_plan"), FormatExecutionSummary(customRuntimeActivationExecutionPlan.ExecutableStepCount, customRuntimeActivationExecutionPlan.PendingStepCount)),
                FormatField(localizer.Get("diagnostics.custom_runtime_activation_report"), FormatExecutionSummary(customRuntimeActivationReport.SuccessCount, customRuntimeActivationReport.FailureCount)),
                FormatField(localizer.Get("diagnostics.custom_runtime_activation_artifacts"), BuildCustomRuntimeActivationArtifactSummary(customRuntimeActivationArtifacts)),
                FormatField(localizer.Get("diagnostics.custom_runtime_preflight"), FormatExecutionSummary(customRuntimeExecutionReport.SuccessCount, customRuntimeExecutionReport.FailureCount)),
                FormatField(localizer.Get("diagnostics.custom_runtime_active_status"), string.IsNullOrWhiteSpace(customRuntimeSnapshot.Readiness.ActiveStatusCode) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatStatusOrCode(customRuntimeSnapshot.Readiness.ActiveStatusCode)),
                FormatField(localizer.Get("diagnostics.custom_runtime_active_detail"), string.IsNullOrWhiteSpace(customRuntimeSnapshot.Readiness.ActiveDetail) ? FormatMuted(localizer.Get("common.not_reported_yet")) : FormatWrappedText(customRuntimeSnapshot.Readiness.ActiveDetail)),
                FormatField(localizer.Get("diagnostics.custom_runtime_binding_samples"), BuildCustomRuntimeBindingSummary(customRuntimeExecutionReport)),
                FormatField(localizer.Get("diagnostics.custom_runtime_bootstrap_scene"), customRuntimeSnapshot.HasRuntimeBootstrap ? FormatCode(localizer.GetOrNa(customRuntimeSnapshot.RuntimeBootstrapSceneLogicalId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.custom_runtime_bootstrap_entry"), customRuntimeSnapshot.HasRuntimeBootstrap ? FormatCode(localizer.GetOrNa(customRuntimeSnapshot.RuntimeBootstrapEntryNodeLogicalId)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.custom_runtime_bootstrap_return"), customRuntimeSnapshot.HasRuntimeBootstrap ? FormatCode(localizer.GetOrNa(customRuntimeSnapshot.RuntimeBootstrapReturnSceneName)) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.custom_runtime_registered_mods"), customRuntimeSnapshot.RegisteredModIds.Count > 0 ? FormatWrappedCsv(string.Join(", ", customRuntimeSnapshot.RegisteredModIds.ToArray())) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.custom_runtime_bootstrap_samples"), customRuntimeSnapshot.RuntimeBootstrapSamples.Count > 0 ? FormatWrappedCsv(string.Join(", ", customRuntimeSnapshot.RuntimeBootstrapSamples.ToArray())) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.custom_runtime_activation_samples"), BuildCustomRuntimeActivationRuntimeSummary(customRuntimeActivationRuntime)),
                FormatField(localizer.Get("diagnostics.custom_runtime_readiness_samples"), BuildCustomRuntimeReadinessSummary(customRuntimeSnapshot.Readiness)),
                FormatField(localizer.Get("diagnostics.map_snapshot_scenes"), FormatNumber(snapshot.Scenes.Count)),
                FormatField(localizer.Get("diagnostics.map_snapshot_pages"), FormatNumber(snapshot.Pages.Count)),
                FormatField(localizer.Get("diagnostics.map_snapshot_highlights"), FormatNumber(snapshot.HighlightRegions.Count)),
                FormatField(localizer.Get("diagnostics.map_snapshot_nodes"), FormatNumber(snapshot.Nodes.Count)),
                FormatField(localizer.Get("diagnostics.scene_local_topology_count"), FormatNumber(topologySnapshot.TotalTopologyCount)),
                FormatField(localizer.Get("diagnostics.scene_local_node_count"), FormatNumber(topologySnapshot.TotalNodeCount)),
                FormatField(localizer.Get("diagnostics.scene_local_scene_registered"), FormatBoolean(topologySnapshot.HasSceneRegistration)),
                FormatField(localizer.Get("diagnostics.scene_local_active_topology"), topologySnapshot.HasActiveTopology ? FormatCode(topologySnapshot.TopologyLogicalId) : FormatMuted(localizer.Get("common.not_reported_yet"))),
                FormatField(localizer.Get("diagnostics.scene_local_active_nodes"), topologySnapshot.ActiveNodeNames.Count > 0 ? FormatWrappedCsv(string.Join(", ", topologySnapshot.ActiveNodeNames.ToArray())) : FormatMuted(localizer.Get("common.not_reported_yet"))),
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

    private static string BuildCountSummary(IReadOnlyDictionary<string, int> counts)
    {
        return string.Join(",\n  ", counts
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(static pair => FormatCode(pair.Key) + "=" + FormatNumber(pair.Value)));
    }

    private static string BuildModRegistrationSummary(LongLiveSceneRoutingRegistrationSnapshot snapshot)
    {
        var lines = new List<string>();
        foreach (var modId in snapshot.OwningModIds)
        {
            var routeCount = GetCount(snapshot.RouteCountsByModId, modId);
            var pageCount = GetCount(snapshot.PageCountsByModId, modId);
            var regionCount = GetCount(snapshot.RegionCountsByModId, modId);
            var nodeCount = GetCount(snapshot.NodeCountsByModId, modId);
            var projectionCount = GetCount(snapshot.RouteProjectionCountsByModId, modId);
            var runtimeSceneCount = GetCount(snapshot.RuntimeSceneCountsByModId, modId);
            var bootstrapCount = GetCount(snapshot.RuntimeBootstrapCountsByModId, modId);
            var topologyCount = GetCount(snapshot.TopologyCountsByModId, modId);
            lines.Add(
                FormatCode(modId) + ": " +
                JoinInlineValues(
                    FormatCode("routes") + "=" + FormatNumber(routeCount),
                    FormatCode("pages") + "=" + FormatNumber(pageCount),
                    FormatCode("regions") + "=" + FormatNumber(regionCount),
                    FormatCode("nodes") + "=" + FormatNumber(nodeCount),
                    FormatCode("projections") + "=" + FormatNumber(projectionCount),
                    FormatCode("runtimeScenes") + "=" + FormatNumber(runtimeSceneCount),
                    FormatCode("bootstraps") + "=" + FormatNumber(bootstrapCount),
                    FormatCode("topologies") + "=" + FormatNumber(topologyCount)));
        }

        return string.Join(",\n  ", lines);
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string key)
    {
        return counts.TryGetValue(key, out var value) ? value : 0;
    }

    private static string FormatField(string label, string value)
    {
        return $"<b>{label}</b>: {value}";
    }

    private static string FormatExecutionSummary(int successCount, int failureCount)
    {
        return JoinInlineValues(
            FormatCode("success") + "=" + FormatNumber(successCount),
            FormatCode("failure") + "=" + FormatNumber(failureCount));
    }

    private static string BuildMapOverviewProbeSummary(LongLiveMapOverviewHostProbeSnapshot probe)
    {
        if (!string.IsNullOrWhiteSpace(probe.ProbeError))
        {
            return FormatStatus(probe.ProbeError);
        }

        return JoinInlineValues(
            FormatCode("UiMapPanel") + "=" + FormatBoolean(probe.HasUiMapPanel),
            FormatCode("NingZhouAnchor") + "=" + FormatBoolean(probe.HasNingZhouInjectionAnchor),
            FormatCode("SeaAnchor") + "=" + FormatBoolean(probe.HasSeaInjectionAnchor));
    }

    private static string BuildMapOverviewBindingSummary(LongLiveMapOverviewExecutionReport report)
    {
        if (report.Results.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", report.Results.Take(4).Select(static result =>
            FormatCode(result.HostBinding.PageId) + "=" +
            JoinInlineValues(
                FormatCode("root") + "=" + FormatCode(string.IsNullOrWhiteSpace(result.HostBinding.HostRootName) ? "n/a" : result.HostBinding.HostRootName),
                FormatCode("anchor") + "=" + FormatBoolean(result.HostBinding.HasInjectionAnchor),
                FormatCode("nodes") + "=" + FormatNumber(result.HostBinding.NodeChildCount))));
    }

    private static string BuildMapOverviewHostRuntimeTargetSummary(LongLiveMapOverviewHostBindingRuntimeSnapshot snapshot)
    {
        if (snapshot.Targets.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", snapshot.Targets.Take(4).Select(static target =>
            FormatCode(target.PageId) + "=" +
            JoinInlineValues(
                FormatCode("inject") + "=" + FormatBoolean(target.RequiresHostInjection),
                FormatCode("anchor") + "=" + FormatBoolean(target.HasInjectionAnchor),
                FormatCode("root") + "=" + FormatCode(string.IsNullOrWhiteSpace(target.HostRootName) ? "n/a" : target.HostRootName))));
    }

    private static string BuildMapOverviewShellRuntimeTargetSummary(LongLiveMapOverviewShellAllocationRuntimeSnapshot snapshot)
    {
        if (snapshot.Targets.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", snapshot.Targets.Take(4).Select(static target =>
            FormatCode(target.PageId) + "=" +
            JoinInlineValues(
                FormatCode("shell") + "=" + FormatCode(target.ShellKind),
                FormatCode("host") + "=" + FormatCode(target.HostSurface),
                FormatCode("bindable") + "=" + FormatBoolean(target.CanBindInCurrentSession))));
    }

    private static string BuildMapOverviewShellReservationSummary(LongLiveMapOverviewShellReservationRuntimeSnapshot snapshot)
    {
        if (snapshot.Targets.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", snapshot.Targets.Take(4).Select(static target =>
            FormatCode(target.PageId) + "=" +
            JoinInlineValues(
                FormatCode("status") + "=" + FormatStatus(target.StatusCode),
                FormatCode("reserved") + "=" + FormatCode(string.IsNullOrWhiteSpace(target.ReservedObjectName) ? "n/a" : target.ReservedObjectName),
                FormatCode("hidden") + "=" + FormatBoolean(target.ReservationHidden))));
    }

    private static string BuildMapOverviewCustomPageSummary(LongLiveMapOverviewCustomPageRuntimeSnapshot snapshot)
    {
        if (snapshot.CustomPageTargetCount == 0)
        {
            return FormatMuted("n/a");
        }

        var mountedPages = snapshot.MountedPageIds.Count == 0
            ? FormatMuted("n/a")
            : FormatWrappedCsv(string.Join(", ", snapshot.MountedPageIds.ToArray()));
        var activePage = string.IsNullOrWhiteSpace(snapshot.ActivePageId)
            ? FormatMuted("n/a")
            : FormatCode(snapshot.ActivePageId);

        return string.Join(",\n  ", new[]
        {
            JoinInlineValues(
                FormatCode("panel") + "=" + FormatBoolean(snapshot.HasPanelInstance),
                FormatCode("active") + "=" + FormatBoolean(snapshot.IsCustomPageActive),
                FormatCode("page") + "=" + activePage),
            JoinInlineValues(
                FormatCode("targets") + "=" + FormatNumber(snapshot.CustomPageTargetCount),
                FormatCode("tabs") + "=" + FormatNumber(snapshot.MountedTabButtonCount),
                FormatCode("highlights") + "=" + FormatNumber(snapshot.MountedTabHighlightCount),
                FormatCode("roots") + "=" + FormatNumber(snapshot.MountedPageRootCount),
                FormatCode("nodes") + "=" + FormatNumber(snapshot.ActivePageRenderedNodeCount)),
            FormatCode("mounted") + "=" + mountedPages,
        });
    }

    private static string BuildCustomRuntimeBindingSummary(LongLiveCustomMapRuntimeExecutionReport report)
    {
        if (report.Results.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", report.Results.Take(4).Select(static result =>
            FormatCode(result.HostBinding.SceneLogicalId) + "=" +
            JoinInlineValues(
                FormatCode("entry") + "=" + FormatCode(string.IsNullOrWhiteSpace(result.HostBinding.EntryRouteKind) ? "n/a" : result.HostBinding.EntryRouteKind),
                FormatCode("return") + "=" + FormatCode(string.IsNullOrWhiteSpace(result.HostBinding.ReturnRouteKind) ? "n/a" : result.HostBinding.ReturnRouteKind),
                FormatCode("topology") + "=" + FormatNumber(result.HostBinding.TopologyNodeCount))));
    }

    private static string BuildCustomRuntimeReadinessSummary(LongLiveCustomMapRuntimeReadinessReport report)
    {
        if (report.Targets.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", report.Targets.Take(4).Select(static target =>
            FormatCode(target.SceneLogicalId) + "=" +
            JoinInlineValues(
                FormatCode("status") + "=" + FormatStatus(target.StatusCode),
                FormatCode("enter") + "=" + FormatBoolean(target.CanEnterNow),
                FormatCode("host") + "=" + FormatBoolean(target.IsHostBackedScene),
                FormatCode("pending") + "=" + FormatBoolean(target.NeedsCustomActivationImplementation))));
    }

    private static string BuildCustomRuntimeActivationRuntimeSummary(LongLiveCustomMapRuntimeActivationRuntimeSnapshot snapshot)
    {
        if (snapshot.Targets.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", snapshot.Targets.Take(4).Select(static target =>
            FormatCode(target.SceneLogicalId) + "=" +
            JoinInlineValues(
                FormatCode("state") + "=" + FormatStatusOrCode(target.ActivationState),
                FormatCode("status") + "=" + FormatStatus(target.StatusCode),
                FormatCode("enter") + "=" + FormatBoolean(target.CanEnterNow),
                FormatCode("proxy") + "=" + FormatBoolean(target.HasBindableHostProxy),
                FormatCode("bind") + "=" + FormatBoolean(target.CanBindProxyRoute))));
    }

    private static string BuildCustomRuntimeActivationArtifactSummary(LongLiveCustomMapRuntimeActivationArtifactSnapshot snapshot)
    {
        if (snapshot.Artifacts.Count == 0)
        {
            return FormatMuted("n/a");
        }

        return string.Join(",\n  ", snapshot.Artifacts.Take(4).Select(static artifact =>
            FormatCode(artifact.SceneLogicalId) + "=" +
            JoinInlineValues(
                FormatCode("prepared") + "=" + FormatBoolean(artifact.HasPreparedHostSurface),
                FormatCode("bound") + "=" + FormatBoolean(artifact.HasProxyRouteBinding),
                FormatCode("status") + "=" + FormatStatus(artifact.StatusCode))));
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
