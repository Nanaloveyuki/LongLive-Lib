using System;

namespace LongLive.Next.Abstractions.Exceptions;

public sealed class NextIntegrationException : InvalidOperationException
{
    public NextIntegrationException(string message)
        : base(message)
    {
    }
}
