using System.Collections.Generic;

namespace LongLive.Next.Abstractions.Localization;

public interface INextLocalizationService
{
    bool IsAvailable { get; }

    string Translate(string key);

    string? GetCurrentLanguageDirectory();

    IReadOnlyList<string> GetAvailableLanguageDirectories();
}
