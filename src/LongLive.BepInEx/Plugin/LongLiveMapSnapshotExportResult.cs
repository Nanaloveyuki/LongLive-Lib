namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapSnapshotExportResult
{
    public LongLiveMapSnapshotExportResult(bool success, string path, string summary)
    {
        Success = success;
        Path = path;
        Summary = summary;
    }

    public bool Success { get; }

    public string Path { get; }

    public string Summary { get; }
}
