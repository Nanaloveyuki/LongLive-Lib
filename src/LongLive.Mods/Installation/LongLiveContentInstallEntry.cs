using System;

namespace LongLive.Mods.Installation;

public sealed class LongLiveContentInstallEntry
{
    public LongLiveContentInstallEntry(
        string contentType,
        string contentId,
        LongLiveContentInstallStatus status,
        string message)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type must not be empty.", nameof(contentType));
        }

        if (string.IsNullOrWhiteSpace(contentId))
        {
            throw new ArgumentException("Content id must not be empty.", nameof(contentId));
        }

        ContentType = contentType;
        ContentId = contentId;
        Status = status;
        Message = message ?? string.Empty;
    }

    public string ContentType { get; }

    public string ContentId { get; }

    public LongLiveContentInstallStatus Status { get; }

    public string Message { get; }
}
