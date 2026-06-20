param()

$ErrorActionPreference = 'Stop'

$program = @'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public enum PopTipIconType
{
    Up,
    Bag,
    Task,
}

namespace LongLive.BepInEx.Plugin
{
    internal static class LongLiveNumericMessageParser
    {
        private static readonly Regex RichTextTagRegex = new Regex("<[^>]+>", RegexOptions.Compiled);

        public static string RebuildNumericMessage(string prefix, int numericValue, string suffix)
        {
            var resolvedSuffix = suffix ?? string.Empty;
            var insertSpaceBeforeNumber = NeedsSeparatorBetweenPrefixAndNumber(prefix);
            var insertSpaceBeforeSuffix = NeedsSeparatorBetweenNumberAndSuffix(resolvedSuffix);

            return prefix
                + (insertSpaceBeforeNumber ? " " : string.Empty)
                + numericValue.ToString(CultureInfo.InvariantCulture)
                + (insertSpaceBeforeSuffix ? " " : string.Empty)
                + resolvedSuffix;
        }

        public static bool TryParseNumericToken(string message, out string prefix, out int numericValue, out string suffix)
        {
            prefix = string.Empty;
            numericValue = 0;
            suffix = string.Empty;
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var plainText = StripRichText(message);
            return TryParsePlainNumericToken(plainText, out prefix, out numericValue, out suffix);
        }

        private static bool TryParsePlainNumericToken(string text, out string prefix, out int numericValue, out string suffix)
        {
            prefix = string.Empty;
            numericValue = 0;
            suffix = string.Empty;

            var end = text.Length - 1;
            while (end >= 0 && char.IsWhiteSpace(text[end]))
            {
                end--;
            }

            while (end >= 0 && !char.IsDigit(text[end]))
            {
                end--;
            }

            if (end < 0 || !char.IsDigit(text[end]))
            {
                return false;
            }

            var start = end;
            while (start >= 0 && char.IsDigit(text[start]))
            {
                start--;
            }

            if (start >= 0 && text[start] == '-')
            {
                start--;
            }

            var numberText = text.Substring(start + 1, end - start);
            if (!int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
            {
                return false;
            }

            prefix = text.Substring(0, start + 1).TrimEnd();
            suffix = text.Substring(end + 1).Trim();
            return !string.IsNullOrWhiteSpace(prefix);
        }

        private static string StripRichText(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('<') < 0)
            {
                return value;
            }

            var withoutTags = RichTextTagRegex.Replace(value, string.Empty);
            return DecodeCommonEntities(withoutTags);
        }

        private static string DecodeCommonEntities(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('&') < 0)
            {
                return value;
            }

            var builder = new StringBuilder(value.Length);
            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] == '&')
                {
                    int consumedLength;
                    char decoded;
                    if (TryConsumeEntity(value, index, out consumedLength, out decoded))
                    {
                        builder.Append(decoded);
                        index += consumedLength - 1;
                        continue;
                    }
                }

                builder.Append(value[index]);
            }

            return builder.ToString();
        }

        private static bool TryConsumeEntity(string value, int startIndex, out int consumedLength, out char decoded)
        {
            consumedLength = 0;
            decoded = default(char);

            if (StartsWithAt(value, startIndex, "&lt;"))
            {
                consumedLength = 4;
                decoded = '<';
                return true;
            }

            if (StartsWithAt(value, startIndex, "&gt;"))
            {
                consumedLength = 4;
                decoded = '>';
                return true;
            }

            if (StartsWithAt(value, startIndex, "&amp;"))
            {
                consumedLength = 5;
                decoded = '&';
                return true;
            }

            if (StartsWithAt(value, startIndex, "&quot;"))
            {
                consumedLength = 6;
                decoded = '"';
                return true;
            }

            if (StartsWithAt(value, startIndex, "&#39;"))
            {
                consumedLength = 5;
                decoded = '\'';
                return true;
            }

            if (StartsWithAt(value, startIndex, "&nbsp;"))
            {
                consumedLength = 6;
                decoded = ' ';
                return true;
            }

            return false;
        }

        private static bool StartsWithAt(string value, int startIndex, string token)
        {
            if (startIndex < 0 || token.Length == 0)
            {
                return false;
            }

            if (value.Length - startIndex < token.Length)
            {
                return false;
            }

            return string.Compare(value, startIndex, token, 0, token.Length, StringComparison.Ordinal) == 0;
        }

        private static bool NeedsSeparatorBetweenPrefixAndNumber(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            var last = prefix[prefix.Length - 1];
            return char.IsLetterOrDigit(last) && last < 128;
        }

        private static bool NeedsSeparatorBetweenNumberAndSuffix(string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
            {
                return false;
            }

            var first = suffix[0];
            return char.IsLetterOrDigit(first) && first < 128;
        }
    }

    internal static class LongLivePopTipAggregation
    {
        public static bool Record(IList<LongLivePopTipAggregationEntry> entries, string message, PopTipIconType iconType, string sound, float now, float aggregationWindowSeconds)
        {
            string prefix;
            int numericValue;
            string suffix;
            if (LongLiveNumericMessageParser.TryParseNumericToken(message, out prefix, out numericValue, out suffix))
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
        private LongLivePopTipAggregationEntry(string rawMessage, string prefix, int numericValue, string suffix, bool hasNumericSuffix, PopTipIconType iconType, string sound, float now)
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

        public string RawMessage { get; private set; }
        public string Prefix { get; private set; }
        public int NumericValue { get; set; }
        public string Suffix { get; private set; }
        public bool HasNumericSuffix { get; private set; }
        public PopTipIconType IconType { get; private set; }
        public string Sound { get; private set; }
        public int Count { get; set; }
        public float LastSeenAt { get; set; }

        public static LongLivePopTipAggregationEntry CreateNumeric(string prefix, int numericValue, string suffix, PopTipIconType iconType, string sound, float now)
        {
            var normalizedSuffix = suffix ?? string.Empty;
            return new LongLivePopTipAggregationEntry(prefix + numericValue.ToString(CultureInfo.InvariantCulture) + normalizedSuffix, prefix, numericValue, normalizedSuffix, true, iconType, sound, now);
        }

        public static LongLivePopTipAggregationEntry CreateLiteral(string rawMessage, PopTipIconType iconType, string sound, float now)
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
}

public static class PopTipAggregationTestProgram
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    public static void Main()
    {
        var entries = new List<LongLive.BepInEx.Plugin.LongLivePopTipAggregationEntry>();

        var merged = LongLive.BepInEx.Plugin.LongLivePopTipAggregation.Record(entries, "cultivation gain 18", PopTipIconType.Up, null, 0f, 0.5f);
        Assert(!merged, "First numeric entry should create a new aggregation entry.");
        merged = LongLive.BepInEx.Plugin.LongLivePopTipAggregation.Record(entries, "cultivation gain 22", PopTipIconType.Up, null, 0.2f, 0.5f);
        Assert(merged, "Second numeric entry should merge.");
        Assert(entries.Count == 1, "Numeric merge should keep one entry.");
        Assert(entries[0].BuildMessage() == "cultivation gain 40", "Merged numeric value mismatch.");

        merged = LongLive.BepInEx.Plugin.LongLivePopTipAggregation.Record(entries, "cultivation gain 5 pts", PopTipIconType.Up, null, 0.3f, 0.5f);
        Assert(!merged, "Different numeric suffix should not merge with plain numeric entry.");
        Assert(entries.Count == 2, "Suffix-specific numeric entry should be independent.");
        Assert(entries[1].BuildMessage() == "cultivation gain 5 pts", "Suffix-specific message mismatch.");

        merged = LongLive.BepInEx.Plugin.LongLivePopTipAggregation.Record(entries, "new rumor acquired", PopTipIconType.Task, null, 0.4f, 0.5f);
        Assert(!merged, "First literal entry should create a new entry.");
        merged = LongLive.BepInEx.Plugin.LongLivePopTipAggregation.Record(entries, "new rumor acquired", PopTipIconType.Task, null, 0.45f, 0.5f);
        Assert(merged, "Second literal entry should merge.");
        Assert(entries[2].BuildMessage() == "new rumor acquired x2", "Literal merge count mismatch.");

        merged = LongLive.BepInEx.Plugin.LongLivePopTipAggregation.Record(entries, "new rumor acquired", PopTipIconType.Task, null, 1.2f, 0.5f);
        Assert(!merged, "Literal entry outside aggregation window should create a new entry.");
        Assert(entries.Count == 4, "Late literal entry should create another bucket.");

        Console.WriteLine("LongLivePopTipAggregation tests passed.");
    }
}
'@

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-poptip-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $programPath = Join-Path $tempDir 'PopTipAggregationTestProgram.cs'
    [System.IO.File]::WriteAllText($programPath, $program, [System.Text.Encoding]::UTF8)

    $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path $compiler)) {
        $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    }

    if (-not (Test-Path $compiler)) {
        throw 'CSC compiler not found in .NET Framework directories.'
    }

    $outputPath = Join-Path $tempDir 'PopTipAggregationTestProgram.exe'
    & $compiler /nologo /t:exe /out:$outputPath $programPath
    if ($LASTEXITCODE -ne 0) {
        throw "Pop-tip aggregation test compilation failed with exit code $LASTEXITCODE"
    }

    & $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Pop-tip aggregation test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
