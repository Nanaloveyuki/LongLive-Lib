using LongLive.Next.Abstractions.Models;

namespace LongLive.Next.Abstractions.Events;

public interface INextEventRunner
{
    bool IsAvailable { get; }

    bool IsRunning { get; }

    NextRunHandle RunEvent(NextEventRequest request);

    NextRunHandle RunScript(NextScriptRequest request);

    void SwitchEvent(string eventId);

    void Cancel();
}
