using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Abstractions.Events;
using LongLive.Next.Abstractions.Localization;
using LongLive.Next.Abstractions.Queries;
using LongLive.Next.Abstractions.State;
using LongLive.Next.Abstractions.UI;
using LongLive.Next.Runtime.Commands;
using LongLive.Next.Runtime.Events;
using LongLive.Next.Runtime.Internal;
using LongLive.Next.Runtime.Inspection;
using LongLive.Next.Runtime.Localization;
using LongLive.Next.Runtime.Queries;
using LongLive.Next.Runtime.State;
using LongLive.Next.Runtime.UI;

namespace LongLive.Next.Runtime;

public sealed class NextRuntimeFacade
{
    private readonly NextReflectionBridge _bridge;

    public NextRuntimeFacade()
        : this(new NextReflectionBridge())
    {
    }

    internal NextRuntimeFacade(NextReflectionBridge bridge)
    {
        _bridge = bridge;
        EventRunner = new NextEventRunner(_bridge);
        StateStore = new NextStateStore(_bridge);
        CommandRegistry = new NextCommandRegistry(_bridge);
        QueryRegistry = new NextQueryRegistry(_bridge);
        Ui = new NextUiService(_bridge);
        Localization = new NextLocalizationService(_bridge);
        ContentInspector = new NextContentRuntimeInspector(_bridge);
    }

    public bool IsAvailable => _bridge.IsAvailable;

    public INextEventRunner EventRunner { get; }

    public INextStateStore StateStore { get; }

    public INextCommandRegistry CommandRegistry { get; }

    public INextQueryRegistry QueryRegistry { get; }

    public INextUiService Ui { get; }

    public INextLocalizationService Localization { get; }

    public NextContentRuntimeInspector ContentInspector { get; }
}
