using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapRegistryValidationResult
{
    private readonly List<LongLiveMapRegistryValidationIssue> _issues = new List<LongLiveMapRegistryValidationIssue>();

    public bool IsValid
    {
        get
        {
            foreach (var issue in _issues)
            {
                if (issue.IsError)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public IReadOnlyList<LongLiveMapRegistryValidationIssue> Issues => _issues;

    public void AddError(string code, string message)
    {
        _issues.Add(new LongLiveMapRegistryValidationIssue(true, code, message));
    }

    public void AddWarning(string code, string message)
    {
        _issues.Add(new LongLiveMapRegistryValidationIssue(false, code, message));
    }
}
