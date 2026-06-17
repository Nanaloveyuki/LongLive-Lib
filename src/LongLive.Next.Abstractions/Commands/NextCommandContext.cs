using System;
using System.Collections.Generic;
using System.Globalization;

namespace LongLive.Next.Abstractions.Commands;

public sealed class NextCommandContext
{
    private readonly string[] _parameters;

    public NextCommandContext(
        string commandName,
        IEnumerable<string>? parameters,
        string rawCommand,
        bool isEnd,
        object? nativeCommand,
        object? nativeEnvironment)
    {
        CommandName = commandName ?? string.Empty;
        RawCommand = rawCommand ?? string.Empty;
        IsEnd = isEnd;
        NativeCommand = nativeCommand;
        NativeEnvironment = nativeEnvironment;
        _parameters = parameters is null ? Array.Empty<string>() : CreateArray(parameters);
    }

    public string CommandName { get; }

    public string RawCommand { get; }

    public bool IsEnd { get; }

    public IReadOnlyList<string> Parameters => _parameters;

    public object? NativeCommand { get; }

    public object? NativeEnvironment { get; }

    public int GetInt(int index, int defaultValue = 0)
    {
        var value = TryGetString(index);
        if (value is null)
        {
            return defaultValue;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public float GetFloat(int index, float defaultValue = 0)
    {
        var value = TryGetString(index);
        if (value is null)
        {
            return defaultValue;
        }

        return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public bool GetBool(int index, bool defaultValue = false)
    {
        var value = TryGetString(index);
        if (value is null)
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var boolResult))
        {
            return boolResult;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult)
            ? intResult != 0
            : defaultValue;
    }

    public string GetString(int index, string defaultValue = "")
    {
        return index >= 0 && index < _parameters.Length ? _parameters[index] : defaultValue;
    }

    public string? TryGetString(int index)
    {
        return index >= 0 && index < _parameters.Length ? _parameters[index] : null;
    }

    private static string[] CreateArray(IEnumerable<string> parameters)
    {
        var result = new List<string>();
        foreach (var parameter in parameters)
        {
            result.Add(parameter ?? string.Empty);
        }

        return result.ToArray();
    }
}
