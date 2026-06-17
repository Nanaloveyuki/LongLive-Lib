using System;

namespace LongLive.Next.Abstractions.Models;

public sealed class NextScriptRequest
{
    public NextScriptRequest(string script, Action? onCompleted = null, string? tag = null)
    {
        Script = script;
        OnCompleted = onCompleted;
        Tag = tag;
    }

    public string Script { get; }

    public Action? OnCompleted { get; }

    public string? Tag { get; }
}
