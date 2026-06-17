using System.Collections.Generic;

namespace LongLive.Mods.Installation;

public sealed class LongLiveInstalledModReport
{
    private readonly List<string> _installedCommands = new List<string>();
    private readonly List<string> _installedQueries = new List<string>();
    private readonly List<LongLiveContentInstallEntry> _contentEntries = new List<LongLiveContentInstallEntry>();
    private readonly List<string> _skippedEntries = new List<string>();

    public IReadOnlyList<string> InstalledCommands => _installedCommands;

    public IReadOnlyList<string> InstalledQueries => _installedQueries;

    public IReadOnlyList<LongLiveContentInstallEntry> ContentEntries => _contentEntries;

    public IReadOnlyList<string> SkippedEntries => _skippedEntries;

    public void AddInstalledCommand(string commandId)
    {
        _installedCommands.Add(commandId);
    }

    public void AddInstalledQuery(string queryId)
    {
        _installedQueries.Add(queryId);
    }

    public void AddContentEntry(LongLiveContentInstallEntry entry)
    {
        _contentEntries.Add(entry);
    }

    public void AddSkippedEntry(string message)
    {
        _skippedEntries.Add(message);
    }
}
