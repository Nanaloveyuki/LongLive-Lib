# BepInEx Host Bootstrap

This document describes the current bootstrap strategy for `LongLive.BepInEx`.

## 1. Purpose

The current host project is not intended to be a full production plugin host yet.

Its present role is smaller:

- provide a formal place for BepInEx-facing code to live
- keep plugin entry concerns separate from `LongLive.Next`
- allow the repository to grow into a real host plugin without moving files later

## 2. Why the Host Project Is Gated

The current repository still does not assume that every machine has:

- a local game installation
- local Unity managed assemblies
- local BepInEx runtime assemblies in final paths

Because of that, `LongLive.BepInEx` is set up with a local-reference switch.

Default behavior:

- the main solution remains buildable without game-host references
- the plugin entry source files are excluded from compilation unless local host references are explicitly enabled

This keeps the repository stable before the host environment is finalized.

## 3. Local Reference Flow

When you are ready to compile the host plugin shell:

1. copy `eng/LocalReferences.props.example` to `eng/LocalReferences.props`
2. set `LongLiveEnableLocalHostReferences` to `true`
3. set `McsGameRoot`
4. set `BepInExCoreDir`

At that point the `LongLive.BepInEx` project will include the plugin entry source files and reference the required local runtime assemblies.

## 4. Current Host Surface

The initial host shell currently includes:

- `LongLivePlugin`
- `LongLivePluginContext`
- `LongLiveHostOptions`
- `LongLiveBootstrapper`
- `LongLiveContentRegistryProvider`

The current bootstrap chain can also run a JSON-mod demo install flow when explicitly enabled through host config.

The plugin bootstraps a shared `NextRuntimeFacade` instance and exposes it through a small context helper.

These files live under `src/LongLive.BepInEx/Plugin/` and are only compiled when local host references are enabled.

## 5. Current Non-Goals

At this stage the host shell intentionally does not include:

- Harmony patch registration
- complex lifecycle orchestration
- custom update loop routing
- config schema expansion
- native bridge wiring

Those should only be added after the host environment is available and the requirements are concrete.

## 6. Logging and Config Direction

The host layer uses BepInEx-native facilities directly.

- logging uses the plugin `ManualLogSource`
- host options use `Config.Bind(...)`

The current design intentionally does not add a separate custom logging abstraction on top of BepInEx.

## 7. JSON Mod Demo Flow

The host layer now includes an optional JSON-mod demo install flow.

When enabled, it will:

1. load the configured demo package directory
2. validate the package
3. install `builtin` commands and queries
4. route content entries through the selected host content backend
5. log content counts for items, skills, buffs, and assets

It does not yet install content data into the actual game runtime.

Current configuration behavior:

- `EnableJsonModDemoInstall` is `false` by default
- `JsonModDemoPath` is empty by default
- `ContentBackend` defaults to `Deferred`
- the demo flow only runs when both the switch is enabled and the directory path is explicitly configured

This avoids coupling a real game deployment to a repository-local sample path.

The host now also owns the composition point for content backend selection.

- `Deferred` keeps content entries in explicit deferred state
- `Next` selects a host-side Next content backend shell that is not implemented yet but reserves the future runtime injection slot

## 8. Lifecycle and Build Helper

The current host shell now also includes:

- plugin metadata in `LongLivePluginMetadata`
- minimal lifecycle hooks in `LongLivePlugin`
- a local build helper at `scripts/build-host.ps1`
- a local deploy helper at `scripts/deploy-host.ps1`
- installer-style registration through `ILongLiveInstaller`

The build helper validates the expected local host reference paths before invoking `dotnet build` for the host project.

The deploy helper builds the host project and copies the resulting plugin artifacts into the local `BepInEx/plugins` directory.
