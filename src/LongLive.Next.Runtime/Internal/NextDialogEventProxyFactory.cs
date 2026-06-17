using System;

namespace LongLive.Next.Runtime.Internal;

internal static class NextDialogEventProxyFactory
{
    public static bool IsSupported => false;

    public static object Create(Type dialogEventInterfaceType, INextDialogEventProxyHandler handler)
    {
        throw new PlatformNotSupportedException(
            "Next dialog-event proxy generation requires System.Reflection.Emit, which is unavailable on the current host runtime.");
    }
}
