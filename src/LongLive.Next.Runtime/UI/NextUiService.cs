using System;
using LongLive.Next.Abstractions.UI;
using LongLive.Next.Runtime.Internal;

namespace LongLive.Next.Runtime.UI;

public sealed class NextUiService : INextUiService
{
    private readonly NextReflectionBridge _bridge;

    public NextUiService()
        : this(new NextReflectionBridge())
    {
    }

    internal NextUiService(NextReflectionBridge bridge)
    {
        _bridge = bridge;
    }

    public bool IsAvailable => _bridge.IsAvailable;

    public void OpenLuaWindow(string packageName, string componentName, string scriptPath, bool modal = true,
        Action? onClosed = null)
    {
        _bridge.InvokeHelper("OpenGUI", packageName, componentName, scriptPath, modal, onClosed);
    }

    public void CloseAll(bool force = false)
    {
        var fguiManager = _bridge.GetStaticPropertyValue("SkySwordKill.Next.Main", "FGUI");
        if (fguiManager is null)
        {
            throw NextReflectionBridge.CreateUnavailableException();
        }

        var methodName = force ? "RemoveAllWindowForce" : "RemoveAllWindow";
        _bridge.InvokeInstanceMethod(fguiManager, methodName);
    }
}
