using System;
using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Abstractions.Queries;
using LongLive.Next.Abstractions.State;

namespace LongLive.Mods.Installation;

public sealed class LongLiveBuiltinCapabilityRegistry : ILongLiveModCapabilityRegistry
{
    private readonly INextStateStore _stateStore;

    public LongLiveBuiltinCapabilityRegistry(INextStateStore stateStore)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public bool TryGetCommandHandler(string handlerId, out NextCommandHandler handler)
    {
        switch (handlerId)
        {
            case "longlive.echo":
                handler = (context, complete) =>
                {
                    var text = context.GetString(0, string.Empty);
                    _stateStore.SetString(LongLiveStateKeys.LastError, text);
                    complete();
                };
                return true;
            default:
                handler = null!;
                return false;
        }
    }

    public bool TryGetQueryHandler(string handlerId, out NextQueryHandler handler)
    {
        switch (handlerId)
        {
            case "longlive.state.int":
                handler = context =>
                {
                    var key = context.GetString(0, string.Empty);
                    var defaultValue = context.GetInt(1, 0);
                    return _stateStore.GetInt(key, defaultValue);
                };
                return true;
            default:
                handler = null!;
                return false;
        }
    }
}
