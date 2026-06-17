using LongLive.Next.Abstractions.Events;
using LongLive.Next.Abstractions.Models;
using LongLive.Next.Runtime.Internal;

namespace LongLive.Next.Runtime.Events;

public sealed class NextEventRunner : INextEventRunner
{
    private readonly NextReflectionBridge _bridge;

    public NextEventRunner()
        : this(new NextReflectionBridge())
    {
    }

    internal NextEventRunner(NextReflectionBridge bridge)
    {
        _bridge = bridge;
    }

    public bool IsAvailable => _bridge.IsAvailable;

    public bool IsRunning => _bridge.IsAvailable && _bridge.GetDialogAnalysisProperty("IsRunningEvent", false);

    public NextRunHandle RunEvent(NextEventRequest request)
    {
        _bridge.InvokeHelper("StartEvent", request.EventId, request.OnCompleted);
        return new NextRunHandle("event", request.EventId, request.Tag);
    }

    public NextRunHandle RunScript(NextScriptRequest request)
    {
        _bridge.InvokeHelper("RunScript", request.Script, request.OnCompleted);
        return new NextRunHandle("script", null, request.Tag);
    }

    public void SwitchEvent(string eventId)
    {
        _bridge.InvokeDialogAnalysis("SwitchDialogEvent", eventId, null);
    }

    public void Cancel()
    {
        _bridge.InvokeDialogAnalysis("CancelEvent");
    }
}
