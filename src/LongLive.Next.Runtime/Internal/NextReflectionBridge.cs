using System;
using System.Linq;
using System.Reflection;
using LongLive.Next.Abstractions.Exceptions;

namespace LongLive.Next.Runtime.Internal;

internal sealed class NextReflectionBridge
{
    private const string HelperTypeName = "SkySwordKill.Next.Helper";
    private const string DialogAnalysisTypeName = "SkySwordKill.Next.DialogSystem.DialogAnalysis";

    private readonly Lazy<Type?> _helperType;
    private readonly Lazy<Type?> _dialogAnalysisType;

    public NextReflectionBridge()
    {
        _helperType = new Lazy<Type?>(() => ResolveTypeCore(HelperTypeName));
        _dialogAnalysisType = new Lazy<Type?>(() => ResolveTypeCore(DialogAnalysisTypeName));
    }

    public bool IsAvailable => _helperType.Value is not null && _dialogAnalysisType.Value is not null;

    public Type ResolveRequiredType(string fullName)
    {
        return ResolveTypeCore(fullName) ?? throw CreateUnavailableException();
    }

    public Type? TryResolveType(string fullName)
    {
        return ResolveTypeCore(fullName);
    }

    public object? InvokeHelper(string methodName, params object?[]? arguments)
    {
        return InvokeRequiredMethod(_helperType.Value, methodName, arguments);
    }

    public object? InvokeDialogAnalysis(string methodName, params object?[]? arguments)
    {
        return InvokeRequiredMethod(_dialogAnalysisType.Value, methodName, arguments);
    }

    public object? InvokeDialogAnalysisNonPublic(string methodName, params object?[]? arguments)
    {
        return InvokeRequiredMethod(_dialogAnalysisType.Value, methodName, BindingFlags.NonPublic | BindingFlags.Static, arguments);
    }

    public T GetDialogAnalysisProperty<T>(string propertyName, T fallback = default!)
    {
        var type = _dialogAnalysisType.Value;
        if (type is null)
        {
            throw CreateUnavailableException();
        }

        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        if (property?.GetValue(null) is T value)
        {
            return value;
        }

        return fallback;
    }

    public object? GetStaticPropertyValue(string typeName, string propertyName)
    {
        var type = ResolveRequiredType(typeName);
        return GetStaticPropertyValue(type, propertyName);
    }

    public object? GetStaticPropertyValue(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        return property?.GetValue(null);
    }

    public object? GetInstancePropertyValue(object? instance, string propertyName)
    {
        if (instance is null)
        {
            return null;
        }

        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(instance);
    }

    public object? InvokeStaticMethod(Type type, string methodName, params object?[]? arguments)
    {
        return InvokeRequiredMethod(type, methodName, arguments);
    }

    public object? InvokeInstanceMethod(object instance, string methodName, params object?[]? arguments)
    {
        var candidateMethods = instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == methodName)
            .ToArray();

        var args = arguments ?? Array.Empty<object?>();
        foreach (var method in candidateMethods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != args.Length)
            {
                continue;
            }

            if (!CanAccept(parameters, args))
            {
                continue;
            }

            return method.Invoke(instance, args);
        }

        throw new MissingMethodException(instance.GetType().FullName, methodName);
    }

    private static Type? ResolveTypeCore(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName, throwOnError: false, ignoreCase: false);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    private static object? InvokeRequiredMethod(Type? type, string methodName, object?[]? arguments)
    {
        return InvokeRequiredMethod(type, methodName, BindingFlags.Public | BindingFlags.Static, arguments);
    }

    private static object? InvokeRequiredMethod(Type? type, string methodName, BindingFlags flags, object?[]? arguments)
    {
        if (type is null)
        {
            throw CreateUnavailableException();
        }

        var candidateMethods = type
            .GetMethods(flags)
            .Where(method => method.Name == methodName)
            .ToArray();

        var args = arguments ?? Array.Empty<object?>();
        foreach (var method in candidateMethods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != args.Length)
            {
                continue;
            }

            if (!CanAccept(parameters, args))
            {
                continue;
            }

            return method.Invoke(null, args);
        }

        throw new MissingMethodException(type.FullName, methodName);
    }

    private static bool CanAccept(ParameterInfo[] parameters, object?[] arguments)
    {
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameterType = parameters[index].ParameterType;
            var argument = arguments[index];

            if (argument is null)
            {
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                {
                    return false;
                }

                continue;
            }

            if (!parameterType.IsInstanceOfType(argument))
            {
                return false;
            }
        }

        return true;
    }

    internal static NextIntegrationException CreateUnavailableException()
    {
        return new NextIntegrationException(
            "Next runtime types are not available in the current AppDomain. Ensure the host is running with Next loaded before using LongLive.Next.Runtime.");
    }
}
