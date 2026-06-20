param()

$ErrorActionPreference = 'Stop'

$program = @'
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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

        public static bool TryParseNumericSuffix(string message, out string prefix, out int numericValue)
        {
            string suffix;
            return TryParseNumericToken(message, out prefix, out numericValue, out suffix);
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
}

public static class ParserTestProgram
{
    private static void AssertParse(string input, string expectedPrefix, int expectedValue, string expectedSuffix)
    {
        string prefix;
        int value;
        string suffix;
        if (!LongLive.BepInEx.Plugin.LongLiveNumericMessageParser.TryParseNumericToken(input, out prefix, out value, out suffix))
        {
            throw new Exception("Expected parse success for: " + input);
        }

        if (!string.Equals(prefix, expectedPrefix, StringComparison.Ordinal))
        {
            throw new Exception("Prefix mismatch. input=" + input + ", actual=" + prefix + ", expected=" + expectedPrefix);
        }

        if (value != expectedValue)
        {
            throw new Exception("Value mismatch. input=" + input + ", actual=" + value + ", expected=" + expectedValue);
        }

        if (!string.Equals(suffix, expectedSuffix, StringComparison.Ordinal))
        {
            throw new Exception("Suffix mismatch. input=" + input + ", actual=" + suffix + ", expected=" + expectedSuffix);
        }
    }

    private static void AssertNoParse(string input)
    {
        string prefix;
        int value;
        if (LongLive.BepInEx.Plugin.LongLiveNumericMessageParser.TryParseNumericSuffix(input, out prefix, out value))
        {
            throw new Exception("Expected parse failure for: " + input);
        }
    }

    private static void AssertRebuild(string prefix, int value, string suffix, string expected)
    {
        var rebuilt = LongLive.BepInEx.Plugin.LongLiveNumericMessageParser.RebuildNumericMessage(prefix, value, suffix);
        if (!string.Equals(rebuilt, expected, StringComparison.Ordinal))
        {
            throw new Exception("Rebuild mismatch. actual=" + rebuilt + ", expected=" + expected);
        }
    }

    public static void Main()
    {
        AssertParse("value 12", "value", 12, "");
        AssertParse("fire insight<color=#ff744d>18</color>", "fire insight", 18, "");
        AssertParse("fire insight<color=#ff744d>18</color> pts", "fire insight", 18, "pts");
        AssertParse("damage<color=#fff227>x3</color>, mana<color=#fff227>+12</color>", "damagex3, mana+", 12, "");
        AssertParse("<color=#FF0000>task</color> update 5", "task update", 5, "");
        AssertParse("gain 18 points", "gain", 18, "points");
        AssertRebuild("gain", 18, "points", "gain 18 points");
        AssertRebuild("cultivation gain", 40, "", "cultivation gain 40");
        AssertRebuild("prefix", 40, "", "prefix 40");
        AssertRebuild("prefix", 5, "pts", "prefix 5 pts");
        AssertNoParse("new rumor acquired");
        AssertNoParse("fire insight<color=#ff744d>eighteen</color> pts");

        Console.WriteLine(@"LongLiveNumericMessageParser tests passed.");
    }
}
'@

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-parser-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $programPath = Join-Path $tempDir 'ParserTestProgram.cs'
    [System.IO.File]::WriteAllText($programPath, $program, [System.Text.Encoding]::UTF8)

    $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path $compiler)) {
        $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    }

    if (-not (Test-Path $compiler)) {
        throw 'CSC compiler not found in .NET Framework directories.'
    }

    $outputPath = Join-Path $tempDir 'ParserTestProgram.exe'
    & $compiler /nologo /t:exe /out:$outputPath $programPath
    if ($LASTEXITCODE -ne 0) {
        throw "Parser test compilation failed with exit code $LASTEXITCODE"
    }

    & $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Parser test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
