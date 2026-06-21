# LongLive Compatibility Roadmap

This document turns the reference-mod ecosystem study into an implementation-facing compatibility roadmap.

The goal is not to make `LongLive` depend on every existing utility library.

The goal is to define a stable relationship policy for each major external library family and a phased plan for how `LongLive` should absorb, bridge, or redirect the recurring host capabilities they represent.

## 1. Core Rule

`LongLive` should prefer owning host choke points over depending on downstream helper libraries.

That means:

- `BepInEx` is the host runtime foundation
- `Next` remains an important content-side bridge, not a replacement target
- third-party utility libraries are treated as design input and possible compatibility targets
- `LongLive` should only add direct runtime dependencies when the capability cannot reasonably be owned in-house

## 2. Relationship Modes

Every external library or framework should be assigned one of these relationship modes.

### 2.1 Foundation Dependency

Meaning:

- `LongLive` directly depends on this runtime and builds around it

Current fit:

- `BepInEx`

Rule:

- safe to integrate deeply
- safe to use for logging, lifecycle, config, Harmony setup, and plugin bootstrap

### 2.2 Bridge Dependency

Meaning:

- `LongLive` interoperates with it and may expose helper shims for it
- `LongLive` does not let it define the core host architecture

Current fit:

- `Next`

Rule:

- keep bridge surfaces narrow and explicit
- do not design the host runtime around `Next` Lua or dialog assumptions
- prefer typed C# services first and `Next` adapters second

### 2.3 Reference Only

Meaning:

- inspect the library for patch patterns and capability design
- do not mirror its API surface unless there is a concrete migration need

Current fit:

- one-off helper libraries with broad, unstructured utility facades

### 2.4 Capability-Compatible

Meaning:

- `LongLive` provides the same practical capability under a cleaner typed surface
- API names do not need to match the reference library

Current fit:

- warp helpers
- place queries
- scene classification
- metadata merge helpers

### 2.5 Adapter-Compatible

Meaning:

- `LongLive` may provide explicit aliases or bridge wrappers so existing content can migrate with limited edits

Current fit:

- selected `Next` dialog events
- selected utility-library semantics that are already ecosystem conventions

### 2.6 Choke-Point Redirection

Meaning:

- `LongLive` patches the game-level entry point and routes behavior through its own implementation

Current fit:

- scene loading transitions
- map bootstrap hooks
- data-cap expansion hooks
- performance-sensitive global behaviors

This should be the preferred mode when the base-game behavior is fragile, slow, or not extensible enough.

## 3. Library Decisions

This section records the current intended relationship between `LongLive` and the most relevant external libraries.

### 3.1 BepInEx

Mode:

- Foundation dependency

Decision:

- keep as the primary host runtime
- use its logger, config, plugin lifecycle, and Harmony integration directly
- do not add an extra abstraction just to hide standard `BepInEx` usage

### 3.2 Next

Mode:

- Bridge dependency

Decision:

- do not replace `Next`
- keep `LongLive` usable from pure C# mods
- provide optional `Next` bridge surfaces where they materially improve ecosystem adoption
- avoid locking core host modules to Lua-specific or dialog-string-specific designs

### 3.3 WhiteZe Tools

Mode:

- Capability-compatible first
- adapter-compatible later for selected calls

Decision:

- absorb its warp and map-query patterns into `LongLive.SceneRouting`
- do not depend on WhiteZe as a runtime requirement
- only add aliases if real migration demand appears

### 3.4 MaiJiu Tooling

Mode:

- Reference only for now
- capability-compatible later around metadata and registry work

Decision:

- study its custom-dungeon and metadata patching patterns
- avoid copying its project-specific helper surface directly

### 3.5 Ventulus VTools

Mode:

- Reference only for broad utility surface
- capability-compatible for routing and selected dialog semantics

Decision:

- do not chase feature parity across mail, NPC actions, and every helper in the facade
- reuse it as evidence for which high-level host helpers mod authors actually want
- consider later adapter shims only for the routing and query subset

### 3.6 UniqueCream Extra Tools

Mode:

- Choke-point redirection reference
- capability-compatible for asset bootstrap and scene transition behavior

Decision:

- treat it as the strongest reference for host-level scene-load interception
- use its design lesson, not its public API, as the basis for `LongLive.SceneTransition` and `LongLive.UiBootstrap`

### 3.7 MJSpine

Mode:

- Reference only for now

Decision:

- use it to understand content-facing folder layout, Spine bundle loading, and avatar replacement data shape
- do not promise compatibility before the base host runtime is stable

### 3.8 JTools

Mode:

- Reference only for now
- possible future capability-compatible target for render runtime concepts

Decision:

- use it as the richer reference for manager-driven render systems
- especially study how it separates context-specific config modes and skin persistence
- avoid promising drop-in compatibility with its patch surface

### 3.9 HY BuffSeid

Mode:

- Choke-point redirection reference
- capability-compatible later through data-expansion registries

Decision:

- use it as the primary reference for safe expansion of fixed-size game data arrays
- later expose that through `LongLive.DataExpansion` rather than one-off patches per feature

## 4. LongLive Module To Ecosystem Mapping

Each planned `LongLive` module should have a clear relationship to external libraries.

### 4.1 `LongLive.SceneRouting`

Primary source references:

- WhiteZe warp helpers
- VTools warp helpers

Compatibility target:

- behavior-compatible first
- adapter-compatible later for selected `Next` events

Current priority:

- highest

### 4.2 `LongLive.MapOverview`

Primary source references:

- LingJie world-map patching
- WhiteZe scene button patterns

Compatibility target:

- own host implementation

Current priority:

- highest

### 4.3 `LongLive.CustomMapRuntime`

Primary source references:

- LingJie scene runtime bootstrap

Compatibility target:

- own host implementation

Current priority:

- high

### 4.4 `LongLive.Metadata`

Primary source references:

- MaiJiu metadata patches
- WhiteZe JSON/config helpers

Compatibility target:

- capability-compatible

Current priority:

- high

### 4.5 `LongLive.DataExpansion`

Primary source references:

- HY BuffSeid

Compatibility target:

- own host implementation
- optional future registries for extension ranges

Current priority:

- medium

### 4.6 `LongLive.AssetBundles`

Primary source references:

- UniqueCream Extra Tools
- MJSpine
- JTools

Compatibility target:

- capability-compatible

Current priority:

- medium

### 4.7 `LongLive.UiBootstrap`

Primary source references:

- UniqueCream Extra Tools
- existing LongLive main-menu entry and diagnostics UI

Compatibility target:

- own host implementation

Current priority:

- medium

### 4.8 `LongLive.NextBridge`

Primary source references:

- WhiteZe `Next` events and queries
- VTools `VNext`

Compatibility target:

- adapter-compatible by design

Current priority:

- medium

### 4.9 `LongLive.CharacterRender`

Primary source references:

- MJSpine
- JTools

Compatibility target:

- reference-driven only for now

Current priority:

- low until map and metadata foundations are stable

## 5. Phased Implementation Plan

### Phase 0: Preserve Current Host Foundations

Already aligned with current project direction:

- `BepInEx` host plugin bootstrap
- diagnostics and workshop bridge state
- item-use and combat instrumentation work
- map trace groundwork

Rule:

- do not destabilize these foundations just to imitate third-party library surfaces

### Phase 1: Own Core Routing And Map Presence

Deliverables:

- `LongLive.SceneRouting`
- stable scene-kind and place-name queries
- typed warp APIs for player and NPC
- initial `Next` bridge shims only where they help migration
- `LongLive.MapOverview` registration model

Success condition:

- new map-capable mods can depend on `LongLive` for routing without importing WhiteZe or VTools semantics directly

### Phase 2: Own Custom Map Runtime And Metadata

Deliverables:

- `LongLive.CustomMapRuntime`
- `LongLive.Metadata`
- map registry and map-type reservation policy
- host-side scene bootstrap and node graph binding

Success condition:

- a custom world-map location and its runtime scene can be registered through `LongLive` without copying LingJie patch stacks mod-by-mod

### Phase 3: Own Shared Host Utilities

Deliverables:

- `LongLive.AssetBundles`
- `LongLive.UiBootstrap`
- optional `LongLive.SceneTransition`

Success condition:

- shared UI panels, prefabs, and transition helpers stop being ad hoc feature-local code

### Phase 4: Own Data Expansion And Selected Adapters

Deliverables:

- `LongLive.DataExpansion`
- explicit extension-range policies
- selected adapter aliases for common ecosystem bridge calls

Success condition:

- fixed-size game table expansion becomes a controlled host service instead of a per-mod patch gamble

### Phase 5: Evaluate Character Rendering Runtime

Deliverables:

- architecture document first
- implementation only if there is a concrete downstream need

Success condition:

- render replacement becomes an intentional host module, not an opportunistic copy of MJSpine or JTools

## 6. Compatibility Guardrails

To keep the project maintainable, the following rules should stay in force.

### 6.1 No Broad Runtime Dependency Creep

- do not make WhiteZe, VTools, JTools, MJSpine, or other third-party utility libraries hard dependencies of `LongLive`

### 6.2 No Fake Drop-In Claims

- do not claim drop-in compatibility with another library unless `LongLive` intentionally preserves the behavior contract and that contract is tested

### 6.3 Prefer Behavior Over Naming Mimicry

- matching useful behavior matters more than matching old method names

### 6.4 Patch The Choke Point, Not Every Caller

- if a capability is global and fragile, patch the host entry point once and route downstream logic through the controlled implementation

### 6.5 Keep Bridge Layers Thin

- `Next` adapters and compatibility aliases should forward into typed host services, not become the core implementation site

## 7. Immediate Next Steps

The next practical steps should be:

1. formalize `LongLive.SceneRouting` as the first stable host service
2. define the map registry and map-type reservation model around `LongLive.MapOverview` and `LongLive.CustomMapRuntime`
3. pull existing trace and snapshot work into the future metadata and map-registration design
4. delay Spine-runtime work until the map and metadata layers stop moving

That sequence keeps `LongLive` aligned with the actual high-value host problems first.
