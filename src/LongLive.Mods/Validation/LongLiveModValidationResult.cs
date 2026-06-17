using System.Collections.Generic;

namespace LongLive.Mods.Validation;

public sealed class LongLiveModValidationResult
{
    private readonly List<LongLiveModValidationIssue> _issues = new List<LongLiveModValidationIssue>();

    public bool IsValid
    {
        get
        {
            foreach (var issue in _issues)
            {
                if (issue.Severity == LongLiveModValidationSeverity.Error)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public IReadOnlyList<LongLiveModValidationIssue> Issues => _issues;

    public void AddError(string code, string message)
    {
        _issues.Add(new LongLiveModValidationIssue(LongLiveModValidationSeverity.Error, code, message));
    }

    public void AddWarning(string code, string message)
    {
        _issues.Add(new LongLiveModValidationIssue(LongLiveModValidationSeverity.Warning, code, message));
    }
}
