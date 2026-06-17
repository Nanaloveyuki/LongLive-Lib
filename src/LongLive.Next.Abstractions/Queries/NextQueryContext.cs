using System;
using System.Collections.Generic;
using System.Globalization;

namespace LongLive.Next.Abstractions.Queries;

public sealed class NextQueryContext
{
    private readonly object?[] _arguments;

    public NextQueryContext(IEnumerable<object?>? arguments, object? nativeContext, object? nativeEnvironment)
    {
        NativeContext = nativeContext;
        NativeEnvironment = nativeEnvironment;
        _arguments = arguments is null ? Array.Empty<object?>() : CreateArray(arguments);
    }

    public IReadOnlyList<object?> Arguments => _arguments;

    public object? NativeContext { get; }

    public object? NativeEnvironment { get; }

    public object? GetArgument(int index, object? defaultValue = null)
    {
        return index >= 0 && index < _arguments.Length ? _arguments[index] : defaultValue;
    }

    public T? GetArgument<T>(int index)
    {
        var value = GetArgument(index);
        return value is T typedValue ? typedValue : default;
    }

    public int GetInt(int index, int defaultValue = 0)
    {
        var value = GetArgument(index);
        if (value is null)
        {
            return defaultValue;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        if (value is IConvertible convertible)
        {
            try
            {
                return convertible.ToInt32(CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
            }
            catch (InvalidCastException)
            {
            }
            catch (OverflowException)
            {
            }
        }

        return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public float GetFloat(int index, float defaultValue = 0)
    {
        var value = GetArgument(index);
        if (value is null)
        {
            return defaultValue;
        }

        if (value is float floatValue)
        {
            return floatValue;
        }

        if (value is IConvertible convertible)
        {
            try
            {
                return convertible.ToSingle(CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
            }
            catch (InvalidCastException)
            {
            }
            catch (OverflowException)
            {
            }
        }

        return float.TryParse(value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public bool GetBool(int index, bool defaultValue = false)
    {
        var value = GetArgument(index);
        if (value is null)
        {
            return defaultValue;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(value.ToString(), out var parsedBool))
        {
            return parsedBool;
        }

        if (value is IConvertible convertible)
        {
            try
            {
                return convertible.ToInt32(CultureInfo.InvariantCulture) != 0;
            }
            catch (FormatException)
            {
            }
            catch (InvalidCastException)
            {
            }
            catch (OverflowException)
            {
            }
        }

        return defaultValue;
    }

    public string GetString(int index, string defaultValue = "")
    {
        var value = GetArgument(index);
        return value?.ToString() ?? defaultValue;
    }

    private static object?[] CreateArray(IEnumerable<object?> arguments)
    {
        var result = new List<object?>();
        foreach (var argument in arguments)
        {
            result.Add(argument);
        }

        return result.ToArray();
    }
}
