namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRouteResult
{
    private LongLiveSceneRouteResult(
        bool succeeded,
        string requestedSceneName,
        LongLiveSceneRouteKind requestedSceneKind,
        int? requestedEntryIndex,
        int? appliedEntryIndex,
        string failureCode,
        string detail)
    {
        Succeeded = succeeded;
        RequestedSceneName = requestedSceneName;
        RequestedSceneKind = requestedSceneKind;
        RequestedEntryIndex = requestedEntryIndex;
        AppliedEntryIndex = appliedEntryIndex;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool Succeeded { get; }

    public string RequestedSceneName { get; }

    public LongLiveSceneRouteKind RequestedSceneKind { get; }

    public int? RequestedEntryIndex { get; }

    public int? AppliedEntryIndex { get; }

    public string FailureCode { get; }

    public string Detail { get; }

    public static LongLiveSceneRouteResult Success(
        string requestedSceneName,
        LongLiveSceneRouteKind requestedSceneKind,
        int? requestedEntryIndex,
        int? appliedEntryIndex,
        string detail)
    {
        return new LongLiveSceneRouteResult(
            true,
            requestedSceneName,
            requestedSceneKind,
            requestedEntryIndex,
            appliedEntryIndex,
            string.Empty,
            detail);
    }

    public static LongLiveSceneRouteResult Failure(
        string requestedSceneName,
        LongLiveSceneRouteKind requestedSceneKind,
        int? requestedEntryIndex,
        string failureCode,
        string detail)
    {
        return new LongLiveSceneRouteResult(
            false,
            requestedSceneName,
            requestedSceneKind,
            requestedEntryIndex,
            null,
            failureCode,
            detail);
    }
}
