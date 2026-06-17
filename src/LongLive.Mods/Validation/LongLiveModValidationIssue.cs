namespace LongLive.Mods.Validation;

public sealed class LongLiveModValidationIssue
{
    public LongLiveModValidationIssue(LongLiveModValidationSeverity severity, string code, string message)
    {
        Severity = severity;
        Code = code;
        Message = message;
    }

    public LongLiveModValidationSeverity Severity { get; }

    public string Code { get; }

    public string Message { get; }
}
