using LongLive.Next.Abstractions.State;
using LongLive.Next.Runtime.Internal;

namespace LongLive.Next.Runtime.State;

public sealed class NextStateStore : INextStateStore
{
    private readonly NextReflectionBridge _bridge;

    public NextStateStore()
        : this(new NextReflectionBridge())
    {
    }

    internal NextStateStore(NextReflectionBridge bridge)
    {
        _bridge = bridge;
    }

    public bool IsAvailable => _bridge.IsAvailable;

    public int GetInt(string key, int defaultValue = 0)
    {
        var value = _bridge.InvokeDialogAnalysis("GetInt", key);
        return value is int intValue ? intValue : defaultValue;
    }

    public void SetInt(string key, int value)
    {
        _bridge.InvokeDialogAnalysis("SetInt", key, value);
    }

    public string GetString(string key, string defaultValue = "")
    {
        var value = _bridge.InvokeDialogAnalysis("GetStr", key);
        return value as string ?? defaultValue;
    }

    public void SetString(string key, string value)
    {
        _bridge.InvokeDialogAnalysis("SetStr", key, value);
    }

    public int GetInt(string group, string key, int defaultValue = 0)
    {
        var value = _bridge.InvokeDialogAnalysis("GetInt", group, key);
        return value is int intValue ? intValue : defaultValue;
    }

    public void SetInt(string group, string key, int value)
    {
        _bridge.InvokeDialogAnalysis("SetInt", group, key, value);
    }

    public string GetString(string group, string key, string defaultValue = "")
    {
        var value = _bridge.InvokeDialogAnalysis("GetStr", group, key);
        return value as string ?? defaultValue;
    }

    public void SetString(string group, string key, string value)
    {
        _bridge.InvokeDialogAnalysis("SetStr", group, key, value);
    }
}
