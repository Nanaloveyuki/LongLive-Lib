using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewShellReservationRuntimeSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public int RequestedReservationCount { get; set; }

    public int CreatedReservationCount { get; set; }

    public int ReusableReservationCount { get; set; }

    public int HiddenReservationCount { get; set; }

    public IReadOnlyList<LongLiveMapOverviewShellReservationTarget> Targets { get; set; } = new LongLiveMapOverviewShellReservationTarget[0];
}
