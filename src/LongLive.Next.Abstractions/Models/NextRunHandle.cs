namespace LongLive.Next.Abstractions.Models;

public readonly struct NextRunHandle
{
    public NextRunHandle(string kind, string? id = null, string? tag = null)
    {
        Kind = kind;
        Id = id;
        Tag = tag;
    }

    public string Kind { get; }

    public string? Id { get; }

    public string? Tag { get; }
}
