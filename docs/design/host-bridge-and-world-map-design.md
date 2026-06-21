# LongLive Host Bridge And World Map Design

This document defines the intended role of `LongLive` as a host bridge between the game's low-level runtime structures and a higher-level, modder-facing API.

It also defines the first serious expansion target for that bridge: fully custom world-map support implemented as a code-first C# API.

## 1. Positioning

`LongLive` should not be treated as a direct replacement for `Next`.

The more useful boundary is:

- `Next` remains a content-oriented framework focused on script-driven behavior, resource packaging, and patch-friendly extension flows.
- `LongLive` becomes a host bridge that converts hard-to-extend game internals into stable, typed, inspectable abstractions.

That means `LongLive` should primarily solve problems such as:

- runtime structure discovery
- typed access to unstable game internals
- validation before runtime mutation
- compatibility-oriented patching
- performance and safety middleware
- code-first extension surfaces for advanced mods

In short, `LongLive` should sit between:

- the game's concrete Unity and managed runtime implementation
- higher-level mod authors who need a safer and more coherent API

## 2. Core Design Principle

The project should expose abstractions, not raw runtime accidents.

Many game systems are currently discoverable only through:

- scene names
- integer map indices
- JSON table coupling
- fixed prefab hierarchies
- implicit UI assumptions
- state spread across unrelated runtime classes

Those details are useful for reverse engineering, but they are not a good public API.

`LongLive` should therefore aim to:

1. observe the real host runtime
2. model it as explicit typed contracts
3. validate requested changes before mutating host state
4. provide extension points at the abstraction boundary, not at the raw patch boundary

This is the bridge concept in practical terms.

## 3. Why World Map Support Is A Good First Major Target

Custom world-map support is an appropriate flagship feature because it forces `LongLive` to solve the exact problems that define its role.

It touches:

- scene registration
- node graphs
- player navigation
- overview and thumbnail map UI
- task and highlight mapping
- asset loading
- host validation
- compatibility with existing runtime flows

It also clearly differentiates `LongLive` from a content-only extension model.

The intended target is not a simple landmark entry into an existing map.

The intended target is:

- a fully custom map scene or map family
- its own world-map page or equivalent overview representation
- its own nodes and connections
- integration into the game's larger overview or thumbnail map flow
- stable entry and exit behavior compatible with the rest of the game

## 4. What The Runtime Evidence Suggests

Current local inspection suggests that the game's map systems are spread across multiple layers rather than one central open registration API.

Observed host-side concepts include:

- scene loading through `Tools.loadMapScenes(...)`
- scene metadata through `SceneNameJsonData`
- world-node metadata through `AllMapLuDainType`
- task and location mapping through `MapIndexData`
- runtime node containers through `AllMapManage.mapIndex`
- specialized overview map UIs that scan prebuilt node and highlight trees

This implies two important conclusions.

First, fully custom map support is probably feasible.

Second, feasibility does not come from a single built-in API. It comes from building a controlled host bridge that can coordinate multiple runtime layers safely.

## 5. Public Direction: Code-First Before JSON-First

For this area, `LongLive` should prioritize a C# code-first API before any declarative JSON authoring model.

Reasons:

- the runtime contracts are still being discovered
- world-map behavior is heavily coupled to host logic, not only static data
- a premature JSON DSL would likely encode weak or incorrect assumptions
- code-first registration is easier to validate incrementally in real gameplay tests

JSON support can still be added later as an import or packaging format.

But the primary source of truth for the first serious world-map implementation should be typed C# registration.

## 6. Proposed Layering

The custom world-map feature should not be implemented as one giant patch block.

It should be split into explicit host-bridge layers.

### 6.1 Host Observation Layer

Purpose:

- inspect relevant runtime structures without mutating them
- centralize runtime discovery logic
- provide stable typed snapshots for diagnostics and validation

Examples:

- current scene snapshot
- registered scene metadata snapshot
- active world-node snapshot
- overview map UI container snapshot
- known highlight-region snapshot

This layer should be read-oriented first.

### 6.2 Scene Registry Layer

Purpose:

- represent map scenes and related host metadata as typed registration objects

Suggested responsibilities:

- register a scene identity
- bind display names and map category information
- define outside-scene return behavior
- define overview highlight bindings
- define asset or bundle sources when needed

This layer should hide direct mutation of low-level scene metadata tables.

### 6.3 World Map Page Registry Layer

Purpose:

- model a full world-map page, not just a single landmark or warp point

Suggested responsibilities:

- page identity and ordering
- title and visible tab metadata
- background art
- highlight-region definitions
- node-root layout definition
- previous and next page behavior when relevant

This is one of the clearest differentiators between `LongLive` and simpler extension flows.

### 6.4 World Node Registry Layer

Purpose:

- represent navigable map nodes as first-class typed objects

Suggested responsibilities:

- node identity
- display name
- visual position
- node grouping
- connection graph
- warp target scene
- optional interaction rules
- optional icon or tooltip metadata

The public API should not force downstream mods to manipulate raw integer dictionaries directly.

### 6.5 Navigation Service Layer

Purpose:

- centralize movement into, within, and out of custom world maps

Suggested responsibilities:

- enter scene
- leave scene
- return to outside scene
- move from overview node to target scene
- preserve host-side last-scene expectations where required

This avoids repeating fragile scene-flow logic in every mod.

### 6.6 Overview UI Adapter Layer

Purpose:

- inject custom map pages into the game's large overview and thumbnail map flow

Suggested responsibilities:

- install custom page tab or selector UI
- install custom background and node roots
- bind hover and highlight behavior
- bind click and warp behavior
- integrate page switching controls

This layer is expected to be patch-heavy internally, but patch-heavy does not mean public-API-heavy.

Its public face should stay small and declarative.

### 6.7 Validation Layer

Purpose:

- detect invalid or conflicting registrations before they become runtime failures

Suggested checks include:

- duplicate scene identifiers
- duplicate page identifiers
- duplicate node identifiers
- duplicate highlight identifiers
- missing target scene references
- broken node connection references
- missing required assets
- impossible outside-scene return configuration

This layer is essential.

Without it, custom map support will become brittle very quickly.

### 6.8 Compatibility And Diagnostics Layer

Purpose:

- explain what was installed, what was patched, and what conflicts were observed

Suggested outputs:

- runtime registration summaries
- detected mod conflicts
- map install reports
- host capability probes
- debug-mode structure dumps

This is another major area where `LongLive` should be stronger than a minimal extension framework.

## 7. How This Differs From Next

The distinction should stay deliberate.

`Next` is still valuable for:

- existing content and script workflows
- data-driven patch mods
- resource packaging flows
- author-facing extension patterns that do not need deep host restructuring

`LongLive` should focus on areas where a bridge layer adds the most value:

- typed host abstractions
- deeper runtime integration
- structure validation
- compatibility management
- performance-sensitive middleware
- advanced systems that need both high-level ergonomics and low-level control

The goal is not to erase `Next`.

The goal is to expose a cleaner and more powerful C# host surface on top of the game and, where useful, alongside `Next`.

## 8. Proposed Public API Direction

The exact types can evolve, but the API should trend toward a small set of stable registration surfaces.

Illustrative examples:

- `ILongLiveWorldMapRegistry`
- `ILongLiveWorldMapPageRegistry`
- `ILongLiveWorldNodeRegistry`
- `ILongLiveSceneRegistry`
- `ILongLiveNavigationService`
- `ILongLiveWorldMapValidator`
- `ILongLiveHostDiagnostics`

Suggested public model categories:

- scene descriptors
- world-map page descriptors
- node descriptors
- highlight descriptors
- navigation requests
- validation results
- install reports

Public naming should stay typed and descriptive rather than imitating raw game class names.

Internally, adapter code may still talk to classes such as `AllMapManage`, `SceneNameJsonData`, or other host-specific structures.

Externally, consumers should work against `LongLive` concepts.

## 9. Recommended Implementation Order

The implementation should proceed in small host-verifiable increments.

### Stage 1

Build observation and diagnostics first.

Targets:

- inspect scene metadata
- inspect world-node state
- inspect overview UI structure
- log stable snapshots in debug mode

### Stage 2

Add scene registration and validation.

Targets:

- register custom scene descriptors
- validate target references
- prove stable enter and exit behavior

### Stage 3

Add world-map page registration.

Targets:

- inject a custom overview page shell
- render a background
- support page selection UI

### Stage 4

Add node registration and navigation.

Targets:

- place custom nodes
- define connections
- warp into registered scenes

### Stage 5

Add highlight, task, and compatibility integration.

Targets:

- highlight-region binding
- task-location mapping
- conflict detection and install reporting

This order reduces rework and keeps each phase testable inside the game.

## 10. Immediate Design Rule

Do not expose raw low-level patch points as the primary public extension mechanism.

Instead:

- keep the patching machinery internal
- keep runtime observation explicit
- keep registrations typed
- keep validation mandatory for complex host mutations

If `LongLive` succeeds at this boundary, it will become exactly what it should be:

- a bridge between difficult game internals and high-level mod development
- a place where advanced systems can be built without forcing every downstream author to reverse engineer the host from scratch

## 11. Immediate Next Steps

The next implementation work for this design should likely begin with:

- a read-only world-map diagnostics pass
- typed scene and map descriptor models
- a first internal registry shape for custom map pages
- a validation-first registration pipeline

Those pieces create a stable foundation for the later UI and navigation injection work.
