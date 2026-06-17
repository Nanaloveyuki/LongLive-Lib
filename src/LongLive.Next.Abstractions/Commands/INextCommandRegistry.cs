namespace LongLive.Next.Abstractions.Commands;

public interface INextCommandRegistry
{
    bool IsAvailable { get; }

    void Register(string commandName, NextCommandHandler handler);
}
