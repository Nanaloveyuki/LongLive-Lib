using System;

namespace LongLive.Mods.Exceptions;

public sealed class LongLiveModLoadException : Exception
{
    public LongLiveModLoadException(string message)
        : base(message)
    {
    }

    public LongLiveModLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
