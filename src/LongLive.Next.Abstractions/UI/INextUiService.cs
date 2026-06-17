using System;

namespace LongLive.Next.Abstractions.UI;

public interface INextUiService
{
    bool IsAvailable { get; }

    void OpenLuaWindow(string packageName, string componentName, string scriptPath, bool modal = true,
        Action? onClosed = null);

    void CloseAll(bool force = false);
}
