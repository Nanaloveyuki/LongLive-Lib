namespace LongLive.Mods.Maps;

public sealed class LongLiveMapRegistryValidationIssue
{
    public LongLiveMapRegistryValidationIssue(bool isError, string code, string message)
    {
        IsError = isError;
        Code = code;
        Message = message;
    }

    public bool IsError { get; }

    public string Code { get; }

    public string Message { get; }
}
