namespace LongLive.Mods.Models;

public sealed class LongLiveLocaleResource
{
    public LongLiveLocaleResource(string relativePath, string content)
    {
        RelativePath = relativePath;
        Content = content;
    }

    public string RelativePath { get; }

    public string Content { get; }
}
