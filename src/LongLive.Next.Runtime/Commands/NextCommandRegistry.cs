using System;
using System.Collections;
using System.Collections.Generic;
using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Runtime.Internal;

namespace LongLive.Next.Runtime.Commands;

public sealed class NextCommandRegistry : INextCommandRegistry
{
    private const string DialogEventInterfaceTypeName = "SkySwordKill.Next.DialogEvent.IDialogEvent";

    private readonly NextReflectionBridge _bridge;

    public NextCommandRegistry()
        : this(new NextReflectionBridge())
    {
    }

    internal NextCommandRegistry(NextReflectionBridge bridge)
    {
        _bridge = bridge;
    }

    public bool IsAvailable => _bridge.IsAvailable && _bridge.TryResolveType(DialogEventInterfaceTypeName) is not null;

    public void Register(string commandName, NextCommandHandler handler)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            throw new ArgumentException("Command name must not be empty.", nameof(commandName));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var dialogEventInterfaceType = _bridge.ResolveRequiredType(DialogEventInterfaceTypeName);
        var adapter = new NextCommandHandlerAdapter(_bridge, commandName, handler);
        var proxy = NextDialogEventProxyFactory.Create(dialogEventInterfaceType, adapter);
        _bridge.InvokeDialogAnalysis("RegisterCommand", commandName, proxy);
    }

    private sealed class NextCommandHandlerAdapter : INextDialogEventProxyHandler
    {
        private readonly NextReflectionBridge _bridge;
        private readonly string _registeredCommandName;
        private readonly NextCommandHandler _handler;

        public NextCommandHandlerAdapter(NextReflectionBridge bridge, string registeredCommandName, NextCommandHandler handler)
        {
            _bridge = bridge;
            _registeredCommandName = registeredCommandName;
            _handler = handler;
        }

        public void Execute(object? nativeCommand, object? nativeEnvironment, Action? callback)
        {
            var context = CreateContext(nativeCommand, nativeEnvironment);
            _handler(context, callback ?? NoOp);
        }

        private NextCommandContext CreateContext(object? nativeCommand, object? nativeEnvironment)
        {
            var commandName = _bridge.GetInstancePropertyValue(nativeCommand, "Command") as string ?? _registeredCommandName;
            var rawCommand = _bridge.GetInstancePropertyValue(nativeCommand, "RawCommand") as string ?? commandName;
            var isEnd = _bridge.GetInstancePropertyValue(nativeCommand, "IsEnd") as bool? ?? false;
            var parameters = ReadParameters(nativeCommand);

            return new NextCommandContext(commandName, parameters, rawCommand, isEnd, nativeCommand, nativeEnvironment);
        }

        private IEnumerable<string> ReadParameters(object? nativeCommand)
        {
            var value = _bridge.GetInstancePropertyValue(nativeCommand, "ParamList");
            if (value is not IEnumerable enumerable)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            foreach (var item in enumerable)
            {
                result.Add(item as string ?? item?.ToString() ?? string.Empty);
            }

            return result;
        }

        private static void NoOp()
        {
        }
    }
}
