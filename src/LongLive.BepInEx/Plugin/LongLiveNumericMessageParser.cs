using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveNumericMessageParser
{
    private static readonly Regex RichTextTagRegex = new Regex("<[^>]+>", RegexOptions.Compiled);

    public static string RebuildNumericMessage(string prefix, int numericValue, string? suffix)
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

    public static bool TryParseNumericToken(string? message, out string prefix, out int numericValue, out string suffix)
    {
        prefix = string.Empty;
        numericValue = 0;
        suffix = string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var text = message!;
        var plainText = StripRichText(text);

        return TryParsePlainNumericToken(plainText, out prefix, out numericValue, out suffix);
    }

    public static bool TryParseNumericSuffix(string? message, out string prefix, out int numericValue)
    {
        return TryParseNumericToken(message, out prefix, out numericValue, out _);
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
                if (TryConsumeEntity(value, index, out var consumedLength, out var decoded))
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
        decoded = default;

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
