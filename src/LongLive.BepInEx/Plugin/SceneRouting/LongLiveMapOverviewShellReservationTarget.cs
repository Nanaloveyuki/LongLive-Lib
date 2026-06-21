namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewShellReservationTarget
{
    public string PageId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string HostSurface { get; set; } = string.Empty;

    public string ReservedObjectName { get; set; } = string.Empty;

    public string SourceRootName { get; set; } = string.Empty;

    public bool ReservationCreated { get; set; }

    public bool ReservationReusable { get; set; }

    public bool ReservationHidden { get; set; }

    public string StatusCode { get; set; } = string.Empty;
}
