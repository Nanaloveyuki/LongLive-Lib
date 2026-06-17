using System;

namespace LongLive.Next.Runtime.Internal;

internal static class NextDialogEnvQueryProxyFactory
{
    public static bool IsSupported => false;

    public static object Create(Type dialogEnvQueryInterfaceType, INextDialogEnvQueryProxyHandler handler)
    {
        throw new PlatformNotSupportedException(
            "Next dialog-env-query proxy generation requires System.Reflection.Emit, which is unavailable on the current host runtime.");
    }
}
