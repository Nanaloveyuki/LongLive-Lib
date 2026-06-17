using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Abstractions.Queries;

namespace LongLive.Mods.Installation;

public interface ILongLiveModCapabilityRegistry
{
    bool TryGetCommandHandler(string handlerId, out NextCommandHandler handler);

    bool TryGetQueryHandler(string handlerId, out NextQueryHandler handler);
}
