using System;

namespace LongLive.Next.Abstractions.Models;

public sealed class NextEventRequest
{
    public NextEventRequest(string eventId, Action? onCompleted = null, string? tag = null)
    {
        EventId = eventId;
        OnCompleted = onCompleted;
        Tag = tag;
    }

    public string EventId { get; }

    public Action? OnCompleted { get; }

    public string? Tag { get; }
}
