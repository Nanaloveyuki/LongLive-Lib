using System;
using System.IO;
using System.Text.Json;
using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapSnapshotExportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    private readonly LongLiveMapSnapshotAdapter _adapter;

    public LongLiveMapSnapshotExportService(LongLiveMapSnapshotAdapter? adapter = null)
    {
        _adapter = adapter ?? new LongLiveMapSnapshotAdapter();
    }

    public LongLiveMapRegistryDraft CaptureCurrentSnapshot()
    {
        return _adapter.CaptureCurrentSnapshot();
    }

    public LongLiveMapSnapshotExportResult ExportCurrentSnapshot(string? outputDirectory = null)
    {
        try
        {
            var snapshot = CaptureCurrentSnapshot();
            var targetDirectory = ResolveOutputDirectory(outputDirectory);
            Directory.CreateDirectory(targetDirectory);

            var fileName = "map-snapshot-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
            var targetPath = Path.Combine(targetDirectory, fileName);
            var json = JsonSerializer.Serialize(snapshot, SerializerOptions);
            File.WriteAllText(targetPath, json);

            return new LongLiveMapSnapshotExportResult(true, targetPath, "success");
        }
        catch (Exception exception)
        {
            return new LongLiveMapSnapshotExportResult(false, string.Empty, exception.Message);
        }
    }

    private static string ResolveOutputDirectory(string? outputDirectory)
    {
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            return outputDirectory!;
        }

        var pluginAssemblyPath = typeof(LongLivePlugin).Assembly.Location;
        var pluginDirectory = Path.GetDirectoryName(pluginAssemblyPath);
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            pluginDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            pluginDirectory = ".";
        }

        return Path.Combine(pluginDirectory, "LongLiveExports");
    }
}
