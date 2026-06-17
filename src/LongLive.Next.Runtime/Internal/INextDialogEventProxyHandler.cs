using System;

namespace LongLive.Next.Runtime.Internal;

internal interface INextDialogEventProxyHandler
{
    void Execute(object? nativeCommand, object? nativeEnvironment, Action? callback);
}
