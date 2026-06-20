using System;

namespace LongLive.Next.Runtime.Internal;

public interface INextDialogEventProxyHandler
{
    void Execute(object? nativeCommand, object? nativeEnvironment, Action? callback);
}
