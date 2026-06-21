# LongLive Distribution And Bridge Strategy

This document defines how `LongLive` should be distributed in practice.

The core rule is simple:

- the low-level host must live where `BepInEx` can load it
- content mods and compatibility shells can live where the game and `Next` expect them

`LongLive` should therefore be distributed as a layered system rather than one single folder drop.

## 1. Distribution Roles

`LongLive` should be split into three roles.

### 1.1 LongLive.Host

`LongLive.Host` is the real runtime host.

Responsibilities:

- BepInEx plugin entry
- Harmony patches
- host diagnostics
- map tracing and snapshot export
- performance middleware
- future custom world-map installation
- stable handshake surface for higher-level mods

Install location:

- `BepInEx/plugins/`

This part is not a normal `Next` content mod.

If it is not present inside a real `BepInEx/plugins` directory, the game host will not load it and none of the deeper runtime features will exist.

### 1.2 LongLive.Bridge

`LongLive.Bridge` is the user-facing compatibility shell.

Responsibilities:

- detect whether `LongLive.Host` is installed
- check host version and capability compatibility
- show a user-facing reminder when host prerequisites are missing
- provide a stable attachment point for Next-oriented local-test mods or workshop-delivered content packages

Install location:

- `觅长生/本地Mod测试/.../plugins/Next/...`
- or a workshop-style mod payload that resolves through the same `Next` mod discovery path

The Bridge should stay lightweight.

Its job is not to replace the host. Its job is to help content packages find and validate the host.

### 1.3 LongLive.Content.*

This layer contains actual mod features built on top of the host and, when useful, on top of `Next`.

Examples:

- a custom world-map package
- UI enhancement mods
- code-first content packs with a small Next-facing shell
- future typed gameplay extensions that depend on LongLive host capabilities

Install location:

- local test or workshop mod structure
- optionally with a Bridge dependency

## 2. Why One-Folder Distribution Is Not Enough

The game's mod ecosystem does not treat all content roots equally.

Observed practical constraints:

- `BepInEx` host plugins load from `BepInEx/plugins`
- `Next` local-test content uses `觅长生/本地Mod测试/.../plugins/Next/...`
- workshop-distributed mods may carry their own managed payload layout, but they still depend on the host being loaded correctly

That means a single package cannot realistically serve all three concerns without either:

- duplicating host binaries into places that do not actually load them
- or pretending a content mod is self-sufficient when it is not

`LongLive` should not hide this boundary.

It should document it clearly and build tooling around it.

## 3. Recommended User Installation Model

The recommended install flow should be:

1. install `BepInEx` if it is not already present
2. install `LongLive.Host` into `BepInEx/plugins`
3. install a `LongLive.Bridge` or `LongLive.Content.*` package into the game's normal mod path
4. let the Bridge validate the host at runtime and report status clearly

This model is explicit, honest, and maintainable.

It also matches the current practical reality better than trying to auto-copy host binaries into protected runtime directories behind the user's back.

## 4. Why Automatic Host Copying Is Not The Default

At least for the early project stages, `LongLive` should not assume that a content package may silently install or update host binaries.

Reasons:

- workshop and local-test content locations are not the same as the active host plugin location
- users may run different BepInEx layouts
- host binaries can be locked while the game is running
- silent host replacement makes debugging compatibility problems harder
- manual host prerequisites are common and understandable in this ecosystem

Tooling may still help users stage files.

But the default release story should remain explicit:

- install Host separately
- install Bridge or content packages separately

## 5. Host Handshake Requirements

The Bridge needs a stable way to detect the host.

The first handshake surface should answer at least these questions:

- is the host loaded
- what host version is running
- what handshake protocol version is exposed
- what capabilities are available
- what install root did the host resolve for itself

This handshake should remain:

- read-only
- stable
- simple enough for reflection-based detection if the Bridge cannot directly reference the host assembly at compile time

## 6. First Capability Model

The capability model should stay coarse at first.

Initial examples:

- `host-bootstrap`
- `next-runtime`
- `main-menu-entry`
- `native-probe`
- `battle-trace`
- `battle-guard`
- `bulk-item-use`
- `map-trace`
- `map-snapshot-export`
- `map-registry-planning`

This is enough for a Bridge to say:

- host missing
- host present but too old
- host present but missing a capability that this content package requires

The capability list does not need to expose every patch or every internal subsystem.

## 7. Repository Script Expectations

Repository scripts should reflect the same distribution shape that real users will see.

That means script outputs should be grouped by install target.

Recommended script intent:

- `build-host.ps1`
  Build the host assembly only.
- `deploy-host.ps1`
  Copy host binaries into the active `BepInEx/plugins` directory.
- `deploy-next-localtest.ps1`
  Stage a local-test Bridge or content shell under `觅长生/本地Mod测试`.
- future package helpers
  Produce a host package folder and a separate content package folder.

The important part is consistency:

- what the repository scripts output
- what the README describes
- what the player installs

should all describe the same layered model.

## 8. Recommended Release Story

The early release story should be conservative.

### 8.1 Host Release

Ship `LongLive.Host` as a normal BepInEx plugin package.

Contents should include:

- `LongLive.BepInEx.dll`
- dependent `LongLive.*` assemblies
- optional native DLL if a released feature needs it
- `LongLiveAssets/` when required by the host UI shell
- installation instructions that point directly at `BepInEx/plugins`

### 8.2 Bridge Or Content Release

Ship `LongLive.Bridge` or a content package separately.

Contents should include:

- `Config/modConfig.json`
- `plugins/Next/mod.../`
- any JSON or content payload needed by the package
- a host requirement notice

### 8.3 User Messaging

If the Bridge detects that the host is missing or incompatible, it should:

- explain that `LongLive.Host` must be installed into `BepInEx/plugins`
- optionally show the currently detected host version when one exists
- allow the reminder to be disabled

The project should prefer clear diagnostics over silent failure.

## 9. Immediate Implementation Direction

The next concrete steps for this strategy are:

1. add a minimal host handshake object and `TryGet...` API
2. expose the current capability list from the host
3. update repository scripts so Host deployment and Next local-test staging are clearly separated
4. later add a small Bridge-side detector that can surface host status in-game

This gives `LongLive` a realistic distribution path without pretending that the host/runtime boundary does not exist.
