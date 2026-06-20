using System;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

internal enum LongLiveBulkUsePromptMarker
{
    CultivationGain,
    DanFangUnlock,
    SeaExplorationGain,
    SkillUnlock,
    StaticSkillUnlock,
    HerbEncyclopediaUnlock,
    HerbOriginUnlock,
    TemporaryItemBuffUnlock,
    FaceCustomizationUnlock,
}

internal static class LongLiveBulkItemUsePromptClassifier
{
    public static void Observe(string? message, ISet<LongLiveBulkUsePromptMarker> markers)
    {
        if (markers == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var normalized = message!.Trim();

        if (normalized.StartsWith("你的修为提升了", StringComparison.Ordinal) || normalized.StartsWith("你的修为降低了", StringComparison.Ordinal))
        {
            markers.Add(LongLiveBulkUsePromptMarker.CultivationGain);
        }

        if (normalized.StartsWith("学会了", StringComparison.Ordinal) && normalized.IndexOf("炼制配方", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.DanFangUnlock);
        }

        if (normalized.StartsWith("学会了", StringComparison.Ordinal) && normalized.IndexOf("神通", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.SkillUnlock);
        }

        if (normalized.StartsWith("学会了", StringComparison.Ordinal) && normalized.IndexOf("功法", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.StaticSkillUnlock);
        }

        if (normalized.IndexOf("草药图鉴", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.HerbEncyclopediaUnlock);
        }

        if (normalized.IndexOf("草药产地", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.HerbOriginUnlock);
        }

        if (normalized.StartsWith("对", StringComparison.Ordinal) && normalized.IndexOf("的探索度提升了", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.SeaExplorationGain);
        }

        if (normalized.IndexOf("临时物品增益", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.TemporaryItemBuffUnlock);
        }

        if (string.Equals(normalized, "你解锁了角色形象调整", StringComparison.Ordinal))
        {
            markers.Add(LongLiveBulkUsePromptMarker.FaceCustomizationUnlock);
        }
    }
}
