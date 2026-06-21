namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneRoutingPlanningBundle
{
    public LongLiveSceneRoutingRegistrationSnapshot Registration { get; set; } = new LongLiveSceneRoutingRegistrationSnapshot();

    public LongLiveMapOverviewInstallPlan MapOverviewInstallPlan { get; set; } = new LongLiveMapOverviewInstallPlan();

    public LongLiveMapOverviewShellAllocationPlan MapOverviewShellAllocationPlan { get; set; } = new LongLiveMapOverviewShellAllocationPlan();

    public LongLiveCustomMapRuntimeActivationPlan CustomMapRuntimeActivationPlan { get; set; } = new LongLiveCustomMapRuntimeActivationPlan();

    public LongLiveMapOverviewExecutionPlan MapOverviewExecutionPlan { get; set; } = new LongLiveMapOverviewExecutionPlan();

    public LongLiveCustomMapRuntimeExecutionPlan CustomMapRuntimeExecutionPlan { get; set; } = new LongLiveCustomMapRuntimeExecutionPlan();

    public LongLiveCustomMapRuntimeReadinessReport CustomMapRuntimeReadinessReport { get; set; } = new LongLiveCustomMapRuntimeReadinessReport();

    public LongLiveCustomMapRuntimeActivationRuntimeSnapshot CustomMapRuntimeActivationRuntime { get; set; } = new LongLiveCustomMapRuntimeActivationRuntimeSnapshot();

    public LongLiveCustomMapRuntimeActivationArtifactSnapshot CustomMapRuntimeActivationArtifacts { get; set; } = new LongLiveCustomMapRuntimeActivationArtifactSnapshot();

    public LongLiveCustomMapRuntimeActivationExecutionPlan CustomMapRuntimeActivationExecutionPlan { get; set; } = new LongLiveCustomMapRuntimeActivationExecutionPlan();

    public LongLiveMapOverviewExecutionReport MapOverviewExecutionReport { get; set; } = new LongLiveMapOverviewExecutionReport();

    public LongLiveMapOverviewHostBindingRuntimeSnapshot MapOverviewHostBindingRuntime { get; set; } = new LongLiveMapOverviewHostBindingRuntimeSnapshot();

    public LongLiveMapOverviewShellAllocationRuntimeSnapshot MapOverviewShellAllocationRuntime { get; set; } = new LongLiveMapOverviewShellAllocationRuntimeSnapshot();

    public LongLiveMapOverviewShellReservationRuntimeSnapshot MapOverviewShellReservationRuntime { get; set; } = new LongLiveMapOverviewShellReservationRuntimeSnapshot();

    public LongLiveCustomMapRuntimeExecutionReport CustomMapRuntimeExecutionReport { get; set; } = new LongLiveCustomMapRuntimeExecutionReport();

    public LongLiveCustomMapRuntimeActivationExecutionReport CustomMapRuntimeActivationExecutionReport { get; set; } = new LongLiveCustomMapRuntimeActivationExecutionReport();
}
