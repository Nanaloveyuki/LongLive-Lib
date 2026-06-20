using System;

namespace LongLive.Next.Runtime.Internal;

public static class NextDialogEventProxyFactory
{
    public static Func<Type, INextDialogEventProxyHandler, object>? Factory { get; set; }

    public static bool IsSupported => Factory is not null;

    public static object Create(Type dialogEventInterfaceType, INextDialogEventProxyHandler handler)
    {
        if (Factory is null)
        {
            throw new PlatformNotSupportedException(
                "LongLive.Next command registration currently requires a host-provided dialog-event proxy factory, but none has been configured.");
        }

        return Factory(dialogEventInterfaceType, handler);
    }
}
