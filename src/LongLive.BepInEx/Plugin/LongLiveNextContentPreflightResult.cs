namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveNextContentPreflightResult
{
    public LongLiveNextContentPreflightResult(bool canContinue, string reasonCode, string message)
    {
        CanContinue = canContinue;
        ReasonCode = reasonCode;
        Message = message;
    }

    public bool CanContinue { get; }

    public string ReasonCode { get; }

    public string Message { get; }
}
