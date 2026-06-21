# Next Runtime Usage

This document shows the intended first-step usage of the current `LongLive.Next` runtime surface.

It is written for the current bootstrap stage.

That means:

- the code compiles now
- the public wrapper APIs are available now
- actual runtime execution still requires a real host process with Next loaded into the AppDomain

## 1. Create the Runtime Facade

The current entry point is `NextRuntimeFactory`.

```csharp
using LongLive.Next.Runtime;

var runtime = NextRuntimeFactory.Create();

if (!runtime.IsAvailable)
{
    // Next runtime types are not loaded in the current process.
    return;
}
```

The facade gives access to the currently implemented service set:

- `runtime.EventRunner`
- `runtime.StateStore`
- `runtime.CommandRegistry`
- `runtime.QueryRegistry`
- `runtime.Ui`
- `runtime.Localization`

## 2. Run an Existing Event

```csharp
using LongLive.Next.Abstractions.Models;

var handle = runtime.EventRunner.RunEvent(
    new NextEventRequest(
        eventId: "my_event_id",
        onCompleted: () =>
        {
            // event completed
        },
        tag: "startup-test"));
```

Current notes:

- `eventId` should match an event already known to Next
- `tag` is only a lightweight caller-side marker right now
- the returned `NextRunHandle` is informational at this stage

## 3. Run a Temporary Script

```csharp
using LongLive.Next.Abstractions.Models;

var handle = runtime.EventRunner.RunScript(
    new NextScriptRequest(
        script: @"
Call ShowTip*Hello from LongLive.Next
Call SetInt*longlive.demo#1
",
        onCompleted: () =>
        {
            // script completed
        },
        tag: "demo-script"));
```

Current notes:

- the script format is still Next-native script text
- `LongLive.Next` does not yet provide a typed script builder DSL
- if a richer authoring layer is added later, it should build on top of this surface rather than replacing it

## 4. Read and Write State

```csharp
using LongLive.Next.Abstractions.State;

runtime.StateStore.SetInt(LongLiveStateKeys.DebugEnabled, 1);
var debugEnabled = runtime.StateStore.GetInt(LongLiveStateKeys.DebugEnabled);

runtime.StateStore.SetString(LongLiveStateKeys.CurrentLocale, "en-US");
var locale = runtime.StateStore.GetString(LongLiveStateKeys.CurrentLocale);

runtime.StateStore.SetInt("longlive", "counter", 42);
var counter = runtime.StateStore.GetInt("longlive", "counter");
```

Recommended practice:

- define your keys in one place
- keep a project-level naming convention
- avoid scattering raw string keys across unrelated files

The repository now includes a starter key container:

- `LongLiveStateKeys`

## 5. Register a Custom Command

```csharp
runtime.CommandRegistry.Register(
    "LongLiveEcho",
    (context, complete) =>
    {
        var message = context.GetString(0, "default-message");
        var amount = context.GetInt(1, 0);

        // perform custom logic here

        complete();
    });
```

What the command context currently provides:

- `CommandName`
- `RawCommand`
- `IsEnd`
- parsed string parameters
- access to native Next objects through `NativeCommand` and `NativeEnvironment`

Current design intent:

- use the typed helper methods first
- only touch native objects if you are intentionally dropping down to host-specific behavior

## 6. Register a Custom Query

```csharp
runtime.QueryRegistry.Register(
    "LongLiveValue",
    context =>
    {
        var baseValue = context.GetInt(0, 0);
        return baseValue + 10;
    });
```

What the query context currently provides:

- positional evaluated arguments
- helper conversions such as `GetInt`, `GetFloat`, `GetBool`, and `GetString`
- access to native Next query context and environment when needed

Design guidance:

- keep query handlers focused on expression-friendly logic
- avoid leaking heavy host concerns into expression functions unless there is a strong reason

## 7. Open and Close UI

```csharp
runtime.Ui.OpenLuaWindow(
    packageName: "MyPackage",
    componentName: "MainView",
    scriptPath: "ui/main.lua",
    modal: true,
    onClosed: () =>
    {
        // optional callback
    });

runtime.Ui.CloseAll(force: false);
```

Current notes:

- this is a thin wrapper over the current Next UI entry points
- it is intentionally not a full window framework
- richer window abstractions should sit above this service, not inside it by default

## 8. Use Localization

```csharp
var translated = runtime.Localization.Translate("Menu.Start");
var currentDir = runtime.Localization.GetCurrentLanguageDirectory();
var allDirs = runtime.Localization.GetAvailableLanguageDirectories();
```

Current notes:

- this is only a thin bridge to Next's current language system
- a future `LongLive` localization layer can build on top of this without forcing callers to use raw Next language APIs directly

## 9. Availability and Failure Model

Every current service exposes `IsAvailable`.

The runtime facade also exposes `IsAvailable`.

This means:

- `true` indicates that the core Next runtime types were found in the current AppDomain
- `false` indicates that the current process is not yet a usable Next host environment

If a caller ignores availability and directly invokes a runtime service without Next being loaded, the runtime may throw `NextIntegrationException`.

## 10. Current Bootstrap Limitations

The current usage surface is practical, but it still has clear limits.

- it is not a host plugin by itself
- it does not ship a Unity project setup
- it does not include a direct typed reference to `Next.dll`
- command and query registration currently rely on runtime proxy generation
- query registration currently depends on a controlled internal reflection path because Next does not expose that registration path publicly

These limitations are acceptable at the current stage because the goal is to stabilize API shape before committing to a final host integration model.

## 11. Recommended Near-Term Usage Pattern

If you are building against the current repository state, treat it this way:

1. use `LongLive.Next.Abstractions` as the main API contract
2. use `LongLive.Next.Runtime` as the current implementation package
3. keep host-specific assumptions localized near the boundary where the game process is actually available
4. avoid taking hard dependencies on internal reflection-glue types

That keeps future migration to a stronger typed host integration path manageable.

## 12. Convenience Helpers

The runtime project now also includes `NextRuntimeExtensions` for short-form usage.

Example:

```csharp
runtime.RunEvent("intro_event", tag: "intro");
runtime.SetInt(LongLiveStateKeys.DebugEnabled, 1);
```

These helpers are intentionally thin wrappers over the existing facade services.
