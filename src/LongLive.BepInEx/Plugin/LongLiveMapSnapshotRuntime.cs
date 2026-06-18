using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapSnapshotRuntime
{
    private static bool _autoExportCompleted;

    public static void OnSceneLoaded(Scene scene)
    {
        var plugin = LongLivePlugin.Instance;
        var logger = LongLivePlugin.LogSource;
        if (plugin is null || logger is null)
        {
            return;
        }

        var options = plugin.Options;
        if (!options.EnableAutoExportMapSnapshot.Value)
        {
            return;
        }

        if (!options.EnableDebugLogging.Value)
        {
            logger.LogInfo("LongLive map snapshot auto-export requested, but debug logging is disabled. Scene-load export skipped.");
            return;
        }

        if (_autoExportCompleted)
        {
            return;
        }

        var service = new LongLiveMapSnapshotExportService();
        var snapshot = service.CaptureCurrentSnapshot();
        logger.LogInfo(
            $"LongLive map snapshot scene-load check: scene={scene.name}, scenes={snapshot.Scenes.Count}, pages={snapshot.Pages.Count}, highlights={snapshot.HighlightRegions.Count}, nodes={snapshot.Nodes.Count}");

        if (snapshot.Scenes.Count <= 0 && snapshot.Nodes.Count <= 0)
        {
            logger.LogInfo("LongLive map snapshot scene-load export deferred because the captured snapshot is still empty.");
            return;
        }

        var result = service.ExportCurrentSnapshot();
        if (result.Success)
        {
            logger.LogInfo($"LongLive map snapshot exported on scene load: {result.Path}");
            _autoExportCompleted = true;
        }
        else
        {
            logger.LogInfo($"LongLive map snapshot scene-load export failed: {result.Summary}");
        }
    }
}
