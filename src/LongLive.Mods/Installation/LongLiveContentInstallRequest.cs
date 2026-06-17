using System;

namespace LongLive.Mods.Installation;

public sealed class LongLiveContentInstallRequest<TContent>
    where TContent : class
{
    public LongLiveContentInstallRequest(LongLiveContentInstallContext context, TContent content)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public LongLiveContentInstallContext Context { get; }

    public TContent Content { get; }
}
