using System;
using System.Collections;
using System.Collections.Generic;
using LongLive.Next.Abstractions.Queries;
using LongLive.Next.Runtime.Internal;

namespace LongLive.Next.Runtime.Queries;

public sealed class NextQueryRegistry : INextQueryRegistry
{
    private const string DialogEnvQueryInterfaceTypeName = "SkySwordKill.Next.DialogSystem.IDialogEnvQuery";

    private readonly NextReflectionBridge _bridge;

    public NextQueryRegistry()
        : this(new NextReflectionBridge())
    {
    }

    internal NextQueryRegistry(NextReflectionBridge bridge)
    {
        _bridge = bridge;
    }

    public bool IsAvailable => _bridge.IsAvailable && _bridge.TryResolveType(DialogEnvQueryInterfaceTypeName) is not null;

    public void Register(string methodName, NextQueryHandler handler)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            throw new ArgumentException("Method name must not be empty.", nameof(methodName));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var dialogEnvQueryInterfaceType = _bridge.ResolveRequiredType(DialogEnvQueryInterfaceTypeName);
        var adapter = new NextQueryHandlerAdapter(_bridge, handler);
        var proxy = NextDialogEnvQueryProxyFactory.Create(dialogEnvQueryInterfaceType, adapter);
        _bridge.InvokeDialogAnalysisNonPublic("RegisterEnvQuery", methodName, proxy);
    }

    private sealed class NextQueryHandlerAdapter : INextDialogEnvQueryProxyHandler
    {
        private readonly NextReflectionBridge _bridge;
        private readonly NextQueryHandler _handler;

        public NextQueryHandlerAdapter(NextReflectionBridge bridge, NextQueryHandler handler)
        {
            _bridge = bridge;
            _handler = handler;
        }

        public object? Execute(object? nativeContext)
        {
            var context = CreateContext(nativeContext);
            return _handler(context);
        }

        private NextQueryContext CreateContext(object? nativeContext)
        {
            var nativeEnvironment = _bridge.GetInstancePropertyValue(nativeContext, "Env");
            var arguments = ReadArguments(nativeContext);
            return new NextQueryContext(arguments, nativeContext, nativeEnvironment);
        }

        private IEnumerable<object?> ReadArguments(object? nativeContext)
        {
            var value = _bridge.GetInstancePropertyValue(nativeContext, "Args");
            if (value is not IEnumerable enumerable)
            {
                return Array.Empty<object?>();
            }

            var result = new List<object?>();
            foreach (var item in enumerable)
            {
                result.Add(item);
            }

            return result;
        }
    }
}
