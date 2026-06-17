namespace LongLive.Next.Runtime.Internal;

internal interface INextDialogEnvQueryProxyHandler
{
    object? Execute(object? nativeContext);
}
