using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LongLive.BepInEx.Plugin;

internal static class LongLivePopTipAggregation
{
    public static bool Record(
        IList<LongLivePopTipAggregationEntry> entries,
        string message,
        PopTipIconType iconType,
        string? sound,
        float now,
        float aggregationWindowSeconds)
    {
        if (LongLiveNumericMessageParser.TryParseNumericToken(message, out var prefix, out var numericValue, out var suffix))
        {
            var numericEntry = entries.FirstOrDefault(candidate =>
                candidate.IconType == iconType &&
                string.Equals(candidate.Sound, sound, StringComparison.Ordinal) &&
                candidate.HasNumericSuffix &&
                string.Equals(candidate.Prefix, prefix, StringComparison.Ordinal) &&
                string.Equals(candidate.Suffix, suffix, StringComparison.Ordinal) &&
                now - candidate.LastSeenAt <= aggregationWindowSeconds);

            if (numericEntry != null)
            {
                numericEntry.NumericValue += numericValue;
                numericEntry.Count++;
                numericEntry.LastSeenAt = now;
                return true;
            }

            entries.Add(LongLivePopTipAggregationEntry.CreateNumeric(prefix, numericValue, suffix, iconType, sound, now));
            return false;
        }

        var exactEntry = entries.FirstOrDefault(candidate =>
            candidate.IconType == iconType &&
            string.Equals(candidate.Sound, sound, StringComparison.Ordinal) &&
            !candidate.HasNumericSuffix &&
            string.Equals(candidate.RawMessage, message, StringComparison.Ordinal) &&
            now - candidate.LastSeenAt <= aggregationWindowSeconds);

        if (exactEntry != null)
        {
            exactEntry.Count++;
            exactEntry.LastSeenAt = now;
            return true;
        }

        entries.Add(LongLivePopTipAggregationEntry.CreateLiteral(message, iconType, sound, now));
        return false;
    }
}

internal sealed class LongLivePopTipAggregationEntry
{
    private LongLivePopTipAggregationEntry(string rawMessage, string? prefix, int numericValue, string? suffix, bool hasNumericSuffix, PopTipIconType iconType, string? sound, float now)
    {
        RawMessage = rawMessage;
        Prefix = prefix;
        NumericValue = numericValue;
        Suffix = suffix;
        HasNumericSuffix = hasNumericSuffix;
        IconType = iconType;
        Sound = sound;
        Count = 1;
        LastSeenAt = now;
    }

    public string RawMessage { get; }

    public string? Prefix { get; }

    public int NumericValue { get; set; }

    public string? Suffix { get; }

    public bool HasNumericSuffix { get; }

    public PopTipIconType IconType { get; }

    public string? Sound { get; }

    public int Count { get; set; }

    public float LastSeenAt { get; set; }

    public static LongLivePopTipAggregationEntry CreateNumeric(string prefix, int numericValue, string? suffix, PopTipIconType iconType, string? sound, float now)
    {
        var normalizedSuffix = suffix ?? string.Empty;
        return new LongLivePopTipAggregationEntry(prefix + numericValue.ToString(CultureInfo.InvariantCulture) + normalizedSuffix, prefix, numericValue, normalizedSuffix, true, iconType, sound, now);
    }

    public static LongLivePopTipAggregationEntry CreateLiteral(string rawMessage, PopTipIconType iconType, string? sound, float now)
    {
        return new LongLivePopTipAggregationEntry(rawMessage, null, 0, null, false, iconType, sound, now);
    }

    public string BuildMessage()
    {
        if (HasNumericSuffix && Prefix != null)
        {
            return LongLiveNumericMessageParser.RebuildNumericMessage(Prefix, NumericValue, Suffix);
        }

        if (Count > 1)
        {
            return RawMessage + " x" + Count.ToString(CultureInfo.InvariantCulture);
        }

        return RawMessage;
    }
}
