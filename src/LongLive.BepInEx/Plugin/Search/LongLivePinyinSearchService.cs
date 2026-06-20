using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LongLive.BepInEx.Plugin;

internal static class LongLivePinyinSearchService
{
    private const int MaxMatchCacheEntries = 4096;
    private const int MaxGeneratedPinyinVariants = 16;
    private const int MaxSingleCharPronunciations = 4;

    private static readonly object SyncRoot = new object();
    private static Dictionary<string, string[]>? _singleCharPinyin;
    private static Dictionary<string, string[]>? _phrases;
    private static Dictionary<string, int[]>? _phraseLengthHints;
    private static Dictionary<string, string>? _matchCache;
    private static bool _resourceLoadFailed;
    private static string? _resourceLoadFailureMessage;
    private static int _maxPhraseLength;

    public static bool IsEnabled => LongLivePlugin.Instance?.Options.EnableTuJianPinyinSearch.Value == true;

    public static bool TryMatches(string? source, string? query, out bool result)
    {
        result = false;

        if (!IsEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            result = true;
            return true;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            result = false;
            return true;
        }

        try
        {
            var normalizedQuery = Normalize(query!);
            var normalizedSource = Normalize(source!);
            if (normalizedSource.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result = true;
                return true;
            }

            if (!TryEnsureLoaded())
            {
                return false;
            }

            var pinyinKey = GetOrBuildPinyinKey(source!);
            result = pinyinKey.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            return true;
        }
        catch (Exception exception)
        {
            LogVerbose("pinyin search failed and will fall back to original matcher: " + exception.GetType().Name + ": " + exception.Message);
            return false;
        }
    }

    public static void OnPluginShutdown()
    {
        lock (SyncRoot)
        {
            _singleCharPinyin = null;
            _phrases = null;
            _phraseLengthHints = null;
            _matchCache = null;
            _resourceLoadFailed = false;
            _resourceLoadFailureMessage = null;
            _maxPhraseLength = 0;
        }
    }

    internal static void LogQueryExpansion(string rawQuery, string[] rawTerms, string[] expandedTerms)
    {
        LogVerbose($"query intercepted: raw={rawQuery}, rawTerms=[{string.Join(", ", rawTerms)}], expandedTerms=[{string.Join(", ", expandedTerms)}]");
    }

    internal static void LogMatchResult(string source, string query, bool matched)
    {
        LogVerbose($"match evaluated: query={query}, matched={matched}, source={source}");
    }

    internal static string[] ExpandQueryTerms(IEnumerable<string>? rawTerms)
    {
        if (rawTerms == null)
        {
            return Array.Empty<string>();
        }

        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawTerm in rawTerms)
        {
            if (string.IsNullOrWhiteSpace(rawTerm))
            {
                continue;
            }

            var start = -1;
            for (var index = 0; index < rawTerm.Length; index++)
            {
                if (IsQuerySeparator(rawTerm[index]))
                {
                    AddExpandedTerm(rawTerm, start, index - start, terms, seen);
                    start = -1;
                    continue;
                }

                if (start < 0)
                {
                    start = index;
                }
            }

            AddExpandedTerm(rawTerm, start, rawTerm.Length - start, terms, seen);
        }

        return terms.ToArray();
    }

    private static bool IsQuerySeparator(char value)
    {
        return char.IsWhiteSpace(value)
            || value == ','
            || value == ';'
            || value == '/'
            || value == '|'
            || value == '\\'
            || value == '，'
            || value == '；'
            || value == '、';
    }

    private static bool TryEnsureLoaded()
    {
        if (_resourceLoadFailed)
        {
            LogVerbose("pinyin resources remain unavailable; falling back to original matcher. reason=" + _resourceLoadFailureMessage);
            return false;
        }

        if (_singleCharPinyin != null && _phrases != null && _phraseLengthHints != null && _matchCache != null)
        {
            return true;
        }

        lock (SyncRoot)
        {
            if (_resourceLoadFailed)
            {
                LogVerbose("pinyin resources remain unavailable after lock acquisition; falling back to original matcher. reason=" + _resourceLoadFailureMessage);
                return false;
            }

            if (_singleCharPinyin != null && _phrases != null && _phraseLengthHints != null && _matchCache != null)
            {
                return true;
            }

            try
            {
                _singleCharPinyin = LoadSingleCharDictionary();
                _phrases = LoadPhraseDictionary();
                _phraseLengthHints = LoadPhraseLengthHints(_phrases);
                _matchCache = new Dictionary<string, string>(StringComparer.Ordinal);
                _maxPhraseLength = _phrases.Count == 0 ? 0 : _phrases.Keys.Max(static key => key.Length);
                LogVerbose($"pinyin resources loaded: singleChars={_singleCharPinyin.Count}, phrases={_phrases.Count}, phraseHints={_phraseLengthHints.Count}, maxPhraseLength={_maxPhraseLength}");
                return true;
            }
            catch (Exception exception)
            {
                _resourceLoadFailed = true;
                _resourceLoadFailureMessage = exception.GetType().Name + ": " + exception.Message;
                LogVerbose("pinyin resource load failed; falling back to original matcher. " + _resourceLoadFailureMessage);
                return false;
            }
        }
    }

    private static string GetOrBuildPinyinKey(string source)
    {
        if (_matchCache!.TryGetValue(source, out var cached))
        {
            return cached;
        }

        var built = BuildPinyinKey(source);

        if (_matchCache.Count >= MaxMatchCacheEntries)
        {
            _matchCache.Clear();
            LogVerbose($"pinyin match cache cleared after reaching {MaxMatchCacheEntries} entries");
        }

        _matchCache[source] = built;
        return built;
    }

    private static string BuildPinyinKey(string source)
    {
        var normalizedSource = Normalize(source);
        var fullVariants = new List<string> { string.Empty };
        var initialsVariants = new List<string> { string.Empty };
        var index = 0;

        while (index < source.Length)
        {
            var matchedPhrase = TryResolvePhrase(source, index, out var phraseLength);
            if (matchedPhrase != null)
            {
                AppendPinyinTokens(matchedPhrase, fullVariants, initialsVariants);

                index += phraseLength;
                continue;
            }

            var current = source[index].ToString();
            if (_singleCharPinyin!.TryGetValue(current, out var singlePinyin))
            {
                AppendPinyinOptions(singlePinyin, fullVariants, initialsVariants);
            }
            else
            {
                var normalizedChar = Normalize(current);
                if (!string.IsNullOrWhiteSpace(normalizedChar))
                {
                    AppendNormalizedToken(normalizedChar, fullVariants, initialsVariants);
                }
            }

            index++;
        }

        var fullKey = string.Join("|", fullVariants.Distinct(StringComparer.Ordinal));
        var initialsKey = string.Join("|", initialsVariants.Distinct(StringComparer.Ordinal));
        return normalizedSource + "|" + fullKey + "|" + initialsKey;
    }

    private static string[]? TryResolvePhrase(string source, int startIndex, out int phraseLength)
    {
        phraseLength = 0;
        var maxLength = Math.Min(_maxPhraseLength, source.Length - startIndex);

        var current = source[startIndex].ToString();
        var hintedLengths = _phraseLengthHints!.TryGetValue(current, out var resolvedHintedLengths)
            ? resolvedHintedLengths
            : null;

        if (hintedLengths != null)
        {
            foreach (var hintedLength in hintedLengths)
            {
                if (hintedLength > maxLength)
                {
                    continue;
                }

                var slice = source.Substring(startIndex, hintedLength);
                if (_phrases!.TryGetValue(slice, out var syllables))
                {
                    phraseLength = hintedLength;
                    return syllables;
                }
            }
        }

        for (var length = maxLength; length >= 2; length--)
        {
            if (hintedLengths != null && Array.IndexOf(hintedLengths, length) >= 0)
            {
                continue;
            }

            var slice = source.Substring(startIndex, length);
            if (_phrases!.TryGetValue(slice, out var syllables))
            {
                phraseLength = length;
                return syllables;
            }
        }

        return null;
    }

    private static void AppendPinyinTokens(IEnumerable<string> tokens, List<string> fullVariants, List<string> initialsVariants)
    {
        foreach (var token in tokens)
        {
            AppendPinyinOptions(new[] { token }, fullVariants, initialsVariants);
        }
    }

    private static void AppendPinyinOptions(IEnumerable<string> rawTokens, List<string> fullVariants, List<string> initialsVariants)
    {
        var normalizedOptions = rawTokens
            .Select(Normalize)
            .Where(static token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.Ordinal)
            .Take(MaxSingleCharPronunciations)
            .ToArray();
        if (normalizedOptions.Length == 0)
        {
            return;
        }

        ExpandVariants(fullVariants, normalizedOptions);
        ExpandVariants(initialsVariants, normalizedOptions.Select(static option => option[0].ToString()).Distinct(StringComparer.Ordinal).ToArray());
    }

    private static void AppendNormalizedToken(string normalizedToken, List<string> fullVariants, List<string> initialsVariants)
    {
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return;
        }

        ExpandVariants(fullVariants, new[] { normalizedToken });
        ExpandVariants(initialsVariants, new[] { normalizedToken[0].ToString() });
    }

    private static void ExpandVariants(List<string> variants, IReadOnlyList<string> suffixOptions)
    {
        if (suffixOptions.Count == 0 || variants.Count == 0)
        {
            return;
        }

        if (suffixOptions.Count == 1)
        {
            var suffix = suffixOptions[0];
            for (var index = 0; index < variants.Count; index++)
            {
                variants[index] += suffix;
            }

            return;
        }

        var expanded = new List<string>(Math.Min(MaxGeneratedPinyinVariants, variants.Count * suffixOptions.Count));
        foreach (var prefix in variants)
        {
            foreach (var suffix in suffixOptions)
            {
                expanded.Add(prefix + suffix);
                if (expanded.Count >= MaxGeneratedPinyinVariants)
                {
                    variants.Clear();
                    variants.AddRange(expanded.Distinct(StringComparer.Ordinal));
                    return;
                }
            }
        }

        variants.Clear();
        variants.AddRange(expanded.Distinct(StringComparer.Ordinal));
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }

    private static Dictionary<string, string[]> LoadSingleCharDictionary()
    {
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var line in ReadEmbeddedLines("word.txt"))
        {
            if (!TrySplitDictionaryLine(line, out var key, out var valueText))
            {
                continue;
            }

            var values = valueText.Split(',').Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray();
            if (values.Length == 0)
            {
                continue;
            }

            result[key] = values;
        }

        return result;
    }

    private static Dictionary<string, string[]> LoadPhraseDictionary()
    {
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var line in ReadEmbeddedLines("phrases_dict.txt"))
        {
            if (!TrySplitDictionaryLine(line, out var key, out var valueText))
            {
                continue;
            }

            var values = valueText.Split(',').Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray();
            if (values.Length == 0)
            {
                continue;
            }

            result[key] = values;
        }

        return result;
    }

    private static Dictionary<string, int[]> LoadPhraseLengthHints(IReadOnlyDictionary<string, string[]> phrases)
    {
        var lengthsByLeadingChar = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        foreach (var phrase in phrases.Keys)
        {
            if (string.IsNullOrWhiteSpace(phrase))
            {
                continue;
            }

            var length = phrase.Length;
            if (length < 2)
            {
                continue;
            }

            AddPhraseLengthHint(lengthsByLeadingChar, phrase[0].ToString(), length);
        }

        try
        {
            foreach (var line in ReadEmbeddedLines("phrases_map.txt"))
            {
                if (!TrySplitDictionaryLine(line, out var key, out var valueText))
                {
                    continue;
                }

                foreach (var hintedLength in valueText
                    .Where(char.IsDigit)
                    .Select(static ch => (int)char.GetNumericValue(ch))
                    .Where(static length => length >= 2)
                    .Distinct())
                {
                    AddPhraseLengthHint(lengthsByLeadingChar, key, hintedLength);
                }
            }
        }
        catch (InvalidOperationException)
        {
            LogVerbose("phrases_map.txt was unavailable; phrase-length hints were derived from phrases_dict only");
        }

        return lengthsByLeadingChar.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.OrderByDescending(static length => length).ToArray(),
            StringComparer.Ordinal);
    }

    private static void AddPhraseLengthHint(IDictionary<string, HashSet<int>> lengthsByLeadingChar, string key, int hintedLength)
    {
        if (!lengthsByLeadingChar.TryGetValue(key, out var lengths))
        {
            lengths = new HashSet<int>();
            lengthsByLeadingChar[key] = lengths;
        }

        lengths.Add(hintedLength);
    }

    private static void AddExpandedTerm(string rawTerm, int startIndex, int length, ICollection<string> terms, ISet<string> seen)
    {
        if (startIndex < 0 || length <= 0)
        {
            return;
        }

        var term = rawTerm.Substring(startIndex, length).Trim();
        if (string.IsNullOrWhiteSpace(term) || !seen.Add(term))
        {
            return;
        }

        terms.Add(term);
    }

    private static bool TrySplitDictionaryLine(string line, out string key, out string valueText)
    {
        key = string.Empty;
        valueText = string.Empty;

        var separatorIndex = line.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
        {
            return false;
        }

        key = line.Substring(0, separatorIndex).Trim();
        valueText = line.Substring(separatorIndex + 1).Trim();
        return !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(valueText);
    }

    private static IEnumerable<string> ReadEmbeddedLines(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(name =>
            name.EndsWith("Plugin.Search.Resources.mandarin." + fileName, StringComparison.Ordinal));
        if (resourceName == null)
        {
            throw new InvalidOperationException("LongLive pinyin resource not found: " + fileName);
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException("LongLive pinyin resource stream not found: " + resourceName);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line.Trim();
            }
        }
    }

    private static void LogVerbose(string message)
    {
        if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
        {
            LongLivePlugin.LogSource?.LogInfo("[PinyinSearch] " + message);
        }
    }
}
