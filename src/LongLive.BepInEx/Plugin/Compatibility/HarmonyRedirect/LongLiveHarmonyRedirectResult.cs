namespace LongLive.BepInEx.Plugin;

public readonly struct LongLiveHarmonyRedirectResult
{
    public LongLiveHarmonyRedirectResult(bool applied, string statusCode, string detail)
    {
        Applied = applied;
        StatusCode = statusCode ?? string.Empty;
        Detail = detail ?? string.Empty;
    }

    public bool Applied { get; }

    public string StatusCode { get; }

    public string Detail { get; }
}
