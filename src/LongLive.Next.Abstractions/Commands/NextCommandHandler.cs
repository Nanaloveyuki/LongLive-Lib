using System;

namespace LongLive.Next.Abstractions.Commands;

public delegate void NextCommandHandler(NextCommandContext context, Action complete);
