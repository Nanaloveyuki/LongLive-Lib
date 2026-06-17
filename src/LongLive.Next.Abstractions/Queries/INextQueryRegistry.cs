namespace LongLive.Next.Abstractions.Queries;

public interface INextQueryRegistry
{
    bool IsAvailable { get; }

    void Register(string methodName, NextQueryHandler handler);
}
