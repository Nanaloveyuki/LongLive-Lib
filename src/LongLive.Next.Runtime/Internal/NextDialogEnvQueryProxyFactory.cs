using System;

namespace LongLive.Next.Runtime.Internal;

public static class NextDialogEnvQueryProxyFactory
{
    public static Func<Type, INextDialogEnvQueryProxyHandler, object>? Factory { get; set; }

    public static bool IsSupported => Factory is not null;

    public static object Create(Type dialogEnvQueryInterfaceType, INextDialogEnvQueryProxyHandler handler)
    {
        if (Factory is null)
        {
            throw new PlatformNotSupportedException(
                "LongLive.Next query registration currently requires a host-provided dialog-env-query proxy factory, but none has been configured.");
        }

        return Factory(dialogEnvQueryInterfaceType, handler);
    }
}
