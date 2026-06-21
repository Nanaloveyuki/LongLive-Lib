# Next Runtime Examples

This file contains shorter examples built on top of the current runtime facade and convenience helpers.

## 1. Fast Event Execution

```csharp
using LongLive.Next.Runtime;

var runtime = NextRuntimeFactory.Create();
if (!runtime.IsAvailable)
{
    return;
}

runtime.RunEvent("intro_event", tag: "intro");
```

## 2. Fast Script Execution

```csharp
using LongLive.Next.Runtime;

runtime.RunScript(@"
Call ShowTip*Runtime extension demo
Call SetInt*longlive.demo#1
", tag: "quick-script");
```

## 3. Centralized State Keys

```csharp
using LongLive.Next.Abstractions.State;
using LongLive.Next.Runtime;

runtime.SetString(LongLiveStateKeys.CurrentLocale, "en-US");
runtime.SetInt(LongLiveStateKeys.DebugEnabled, 1);

var locale = runtime.GetString(LongLiveStateKeys.CurrentLocale);
var debugEnabled = runtime.GetInt(LongLiveStateKeys.DebugEnabled);
```

## 4. Command Registration

```csharp
runtime.CommandRegistry.Register(
    "LongLiveEcho",
    (context, complete) =>
    {
        var text = context.GetString(0, "empty");
        runtime.SetString(LongLiveStateKeys.LastError, text);
        complete();
    });
```

## 5. Query Registration

```csharp
runtime.QueryRegistry.Register(
    "LongLiveDebugEnabled",
    _ => runtime.GetInt(LongLiveStateKeys.DebugEnabled, 0));
```

## 6. Suggested Usage Style

Prefer this layering style in calling code:

- use `NextRuntimeFactory` for construction
- use `NextRuntimeFacade` for grouped services
- use `NextRuntimeExtensions` for common short-form calls
- use `LongLiveStateKeys` instead of scattering raw key strings
