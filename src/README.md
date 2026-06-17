# Source Layout

The current formal source tree currently contains three projects:

- `LongLive.Next.Abstractions`
- `LongLive.Next.Runtime`
- `LongLive.BepInEx`
- `LongLive.Mods`

The first runtime-backed services currently implemented are:

- event execution
- state access
- command registration
- query registration
- Lua window opening / bulk window closing
- localization inspection and translation lookup
- shared runtime facade / factory composition

These projects currently use a **reflection-based bridge to Next**.

This is not meant to be the final integration strategy by default. It is a practical bootstrap strategy so the repository can have:

- a real compile-ready C# structure
- stable API boundaries
- no immediate dependency on a fully prepared `Next.dll` / BepInEx / Unity host build chain

## Current Strategy

- `Abstractions`
  only contains stable interfaces and lightweight models
- `Runtime`
  resolves Next runtime types by reflection and invokes them dynamically

## Current Composition Entry

The runtime project now also exposes a small composition layer:

- `NextRuntimeFacade`
- `NextRuntimeFactory`

Their purpose is limited and practical:

- create the current service set from one place
- share one reflection bridge instance across the runtime-backed services
- give future host integration a single construction point to replace or extend

## Current Host Bootstrap

The source tree now also includes a formal `LongLive.BepInEx` project.

Its current role is intentionally limited:

- reserve a stable home for BepInEx-facing code
- keep plugin entry concerns outside `LongLive.Next`
- allow local host-reference based compilation later without restructuring the repository

By default, the repository does not require local game assemblies to build the main solution.

The plugin entry files inside `LongLive.BepInEx/Plugin/` are only compiled when local host references are explicitly enabled through `eng/LocalReferences.props`.

## Why This Is Useful Right Now

- the projects can compile immediately on the current machine
- the repository can stabilize layout and naming before host integration is finished
- the Next wrapper API can evolve without forcing an early dependency layout decision

## Current Mod Schema Work

The source tree now also includes a formal `LongLive.Mods` project.

Its current responsibility is intentionally limited to:

- JSON-mod DTOs
- parsing
- validation

It does not install or execute JSON mods yet.

## Expected Evolution

Once the host environment is ready, there are two realistic options:

1. keep the reflection-based runtime bridge as a compatibility layer
2. add a stronger typed implementation layer that directly references Next

Until that decision is necessary, keeping the project structure stable is more valuable than prematurely locking the integration style.
