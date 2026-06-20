using System;
using System.Reflection;
using BepInEx.Logging;
using LongLive.Mods.Compatibility;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCompatibilityRuntime
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveCompatibilityRegistry _registry;

    public LongLiveCompatibilityRuntime(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registry = new LongLiveCompatibilityRegistry();
    }

    public ILongLiveCompatibilityRegistry Registry => _registry;

    public LongLiveCompatibilitySnapshot CaptureSnapshot()
    {
        return _registry.CaptureSnapshot();
    }

    public void RefreshDynamicState()
    {
        RefreshEasyBatchState();
        RefreshWhiteZeState();
        RefreshVToolsState();
    }

    public bool IsTypeAvailable(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        return ResolveType(typeName) is not null;
    }

    public Type? ResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        return Type.GetType(typeName, false)
            ?? FindLoadedType(typeName);
    }

    public MethodInfo? ResolveMethod(string typeName, string methodName)
    {
        var type = ResolveType(typeName);
        if (type is null || string.IsNullOrWhiteSpace(methodName))
        {
            return null;
        }

        return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
    }

    public void RecordActivation(LongLiveCompatibilityActivationRecord record)
    {
        _registry.RecordActivation(record);

        _logger.LogInfo(
            $"LongLive compatibility activation recorded. redirect={record.RedirectId}, source={record.SourceLibraryId}, detected={record.SourceDetected}, enabled={record.RedirectEnabled}, applied={record.RedirectApplied}, status={record.StatusCode}");
    }

    public void RecordInvocation(string redirectId, string statusCode, string detail)
    {
        if (string.IsNullOrWhiteSpace(redirectId))
        {
            throw new ArgumentException("Compatibility invocation record must define a redirect ID.", nameof(redirectId));
        }

        var record = _registry.GetActivationOrCreate(redirectId);
        record.InvocationCount += 1;
        record.LastInvocationStatusCode = statusCode ?? string.Empty;
        record.LastInvocationDetail = detail ?? string.Empty;

        _logger.LogDebug(
            $"LongLive compatibility invocation recorded. redirect={redirectId}, calls={record.InvocationCount}, status={record.LastInvocationStatusCode}, detail={record.LastInvocationDetail}");
    }

    private static Type? FindLoadedType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(typeName, false);
                if (type is not null)
                {
                    return type;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private void RefreshEasyBatchState()
    {
        var record = _registry.GetActivationOrCreate(LongLiveEasyBatchUpdateRedirect.RedirectId);
        var enabled = LongLiveCompatibilityOptionGate.IsEasyBatchEnabled();
        record.RedirectEnabled = enabled;
        record.RedirectApplied = enabled && LongLiveEasyBatchUpdateRedirect.IsInstalled;
    }

    private void RefreshWhiteZeState()
    {
        var record = _registry.GetActivationOrCreate(LongLiveWhiteZeRoutingAdapter.RedirectId);
        var enabled = LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled();
        record.RedirectEnabled = enabled;
        record.RedirectApplied = enabled && LongLiveWhiteZeRoutingAdapter.IsRegistered;
    }

    private void RefreshVToolsState()
    {
        var record = _registry.GetActivationOrCreate(LongLiveVToolsRoutingAdapter.RedirectId);
        var enabled = LongLiveCompatibilityOptionGate.IsVToolsEnabled();
        record.RedirectEnabled = enabled;
        record.RedirectApplied = enabled && LongLiveVToolsRoutingAdapter.IsRegistered;
    }
}
