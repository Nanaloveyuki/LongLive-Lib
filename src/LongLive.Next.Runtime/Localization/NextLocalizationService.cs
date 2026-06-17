using System.Collections;
using System.Collections.Generic;
using LongLive.Next.Abstractions.Localization;
using LongLive.Next.Runtime.Internal;

namespace LongLive.Next.Runtime.Localization;

public sealed class NextLocalizationService : INextLocalizationService
{
    private readonly NextReflectionBridge _bridge;

    public NextLocalizationService()
        : this(new NextReflectionBridge())
    {
    }

    internal NextLocalizationService(NextReflectionBridge bridge)
    {
        _bridge = bridge;
    }

    public bool IsAvailable => _bridge.IsAvailable;

    public string Translate(string key)
    {
        var mainType = _bridge.ResolveRequiredType("SkySwordKill.Next.Main");
        var nextLanguageType = _bridge.ResolveRequiredType("SkySwordKill.Next.I18N.NextLanguage");

        var mainInstance = _bridge.GetStaticPropertyValue(mainType, "I");
        var currentLanguage = mainInstance is null ? null : _bridge.GetInstancePropertyValue(mainInstance, "CurrentLanguage");

        var translated = _bridge.InvokeStaticMethod(nextLanguageType, "Get", currentLanguage, key);
        return translated as string ?? key;
    }

    public string? GetCurrentLanguageDirectory()
    {
        var mainType = _bridge.ResolveRequiredType("SkySwordKill.Next.Main");
        var mainInstance = _bridge.GetStaticPropertyValue(mainType, "I");
        var currentLanguage = mainInstance is null ? null : _bridge.GetInstancePropertyValue(mainInstance, "CurrentLanguage");
        return currentLanguage is null ? null : _bridge.GetInstancePropertyValue(currentLanguage, "LanguageDir") as string;
    }

    public IReadOnlyList<string> GetAvailableLanguageDirectories()
    {
        var nextLanguageType = _bridge.ResolveRequiredType("SkySwordKill.Next.I18N.NextLanguage");
        var languages = _bridge.GetStaticPropertyValue(nextLanguageType, "Languages");
        var result = new List<string>();
        if (languages is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var dir = _bridge.GetInstancePropertyValue(item, "LanguageDir") as string;
                if (dir is not null && dir.Length > 0)
                {
                    result.Add(dir);
                }
            }
        }

        return result;
    }
}
