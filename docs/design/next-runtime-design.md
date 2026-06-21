# Next Runtime Design

This document describes the current design status of `LongLive.Next`.

It separates three concerns that should not be blurred together:

- the stable public API surface intended for downstream consumers
- the current bootstrap integration strategy used inside this repository
- the future migration path once a real host environment is available

## 1. Scope

`LongLive.Next` is currently a wrapper layer over the existing `Next` framework.

It is not designed as a replacement runtime for Next.

The current goal is narrower:

- expose a cleaner and more maintainable developer-facing API
- preserve compatibility with Next's current extension model
- keep the repository compile-ready before the full game host environment is configured

## 2. Stable Public API Surface

The current stable-facing abstractions live in `LongLive.Next.Abstractions`.

At the current stage, the intended public surface is:

- `INextEventRunner`
- `INextStateStore`
- `INextCommandRegistry`
- `INextQueryRegistry`
- `INextUiService`
- `INextLocalizationService`

The supporting lightweight models and delegates in the abstractions project are also part of the intended surface, including:

- event and script request models
- run handle models
- command and query handler delegates
- command and query context models
- integration exception types

These types are the part of the codebase that should remain as stable as practical while the host integration strategy evolves.

## 3. Current Bootstrap Integration Strategy

The current implementation project is `LongLive.Next.Runtime`.

This project uses a reflection-based bridge to call Next runtime types dynamically.

This is a deliberate bootstrap strategy, not an accidental shortcut.

The reasons are practical:

- the repository can compile immediately on the current machine
- project layout and naming can stabilize before host wiring is ready
- the codebase is not forced to choose a final Next reference strategy too early

The runtime project currently resolves and invokes Next capabilities through:

- public static method invocation
- public instance method invocation
- public property access
- controlled non-public static invocation where Next does not expose a public registration path
- runtime-generated proxy types for Next extension interfaces

## 4. What Is Considered Bootstrap Glue

The following implementation details are bootstrap glue and should not be treated as long-term public API contracts:

- `NextReflectionBridge`
- runtime proxy factories built with `System.Reflection.Emit`
- internal proxy handler interfaces
- reflection-based access to non-public Next registration methods

These types exist to make the current repository usable before strong host references are introduced.

They are internal implementation details and may change substantially without being treated as a breaking API change.

## 5. Why Command and Query Registration Use Proxies

Next exposes extension points through runtime interfaces such as:

- `SkySwordKill.Next.DialogEvent.IDialogEvent`
- `SkySwordKill.Next.DialogSystem.IDialogEnvQuery`

The current repository does not strongly reference `Next.dll` yet.

That means `LongLive.Next` cannot directly compile classes that implement those interfaces at build time.

The current solution is to:

1. keep public handlers in `LongLive.Next.Abstractions`
2. generate runtime proxy types that implement the actual Next interfaces
3. adapt the native Next callback data into `LongLive` context objects

This preserves the extension capability without forcing an early strong-reference integration model.

## 6. Why Query Registration Uses Controlled Non-Public Reflection

There is an important asymmetry between command registration and query registration in Next.

- command registration is exposed through a public static method
- query registration is currently routed through a non-public static method in `DialogAnalysis`

Because of that, `LongLive.Next.Runtime` includes a narrow internal path for invoking that specific category of non-public registration entry point.

This should not be generalized casually.

The rule is:

- use public Next APIs first whenever they exist
- only use controlled non-public reflection when a real extension point exists but Next does not expose a public registration method for it

## 7. Current Module Boundary

The present source layout is intentionally small.

- `LongLive.Next.Abstractions`
  stable interfaces, models, delegates, and exceptions
- `LongLive.Next.Runtime`
  current Next-backed implementations and bootstrap reflection glue

The runtime project also includes a small composition entry:

- `NextRuntimeFacade`
- `NextRuntimeFactory`

These types are intended to reduce construction sprawl while the repository still lacks a full host plugin project.

At this stage, there is no separate host plugin project in the formal source tree yet.

That is intentional.

The repository is still separating API shape from host-environment mechanics.

## 8. What Is Not Stable Yet

The following areas should still be considered provisional:

- exact runtime construction and service wiring shape
- how host availability is detected beyond the current AppDomain-based strategy
- whether reflection remains the primary compatibility path after host setup is complete
- whether a direct typed implementation layer will be added beside the reflection implementation

The facade and factory shape are useful now, but they should still be treated as composition helpers first, not as the deepest long-term architecture commitment.

These are engineering decisions that should be made after the real BepInEx + Next host environment is available.

## 9. Planned Migration Paths

Once the host environment is ready, there are two realistic directions.

### Option A: Keep Reflection as a Compatibility Runtime

In this model:

- the current runtime stays available
- a reflection-based implementation remains useful for loose coupling and compatibility scenarios
- higher-level API stability remains anchored in `LongLive.Next.Abstractions`

### Option B: Add a Strongly Typed Runtime Beside It

In this model:

- a second implementation layer directly references Next
- the public abstraction layer stays the same
- the reflection-backed runtime remains optional or fallback-only

This second option is likely to become attractive once the host environment, references, and packaging strategy are all clear.

## 10. Immediate Design Rule

Until a real host project exists, prioritize:

- stable public abstractions
- minimal internal implementation surface
- clean project boundaries
- no premature expansion into Unity project assets, Harmony patch forests, or native bridge infrastructure

The current repository should continue behaving like an API-first integration library, not like a partially assembled host mod.
