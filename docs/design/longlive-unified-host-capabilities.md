# LongLive Unified Host Capability Plan

This document turns the reference-mod study into a concrete `LongLive` implementation plan.

## 1. Goal

`LongLive` should provide one top-level host layer that downstream mods can depend on instead of repeatedly re-implementing the same low-level patch patterns.

The focus is not to copy another mod's feature set directly.

The focus is to unify the repeated host operations those mods all need.

## 2. Core Capability Groups

The following capability groups should be treated as first-class host modules.

### 2.1 Scene Routing

Module idea:

- `LongLive.SceneRouting`

Responsibilities:

- load scenes by host scene id
- distinguish `AllMaps`, `S...`, `F...`, and `Sea...`
- apply consistent `NowMapIndex` behavior before transfer
- expose typed player warp and NPC warp operations

Suggested public operations:

- `WarpPlayerToScene(...)`
- `WarpPlayerToWorldNode(...)`
- `WarpNpcToScene(...)`
- `WarpNpcToWorldNode(...)`
- `GetCurrentSceneKind()`
- `GetCurrentPlaceName()`

### 2.2 World Map Overview Injection

Module idea:

- `LongLive.MapOverview`

Responsibilities:

- inject custom overview nodes into region maps
- inject custom overview nodes into sea maps
- assign icon, label, destination scene, and unlock rule
- optionally adjust quick-move routing

Suggested public operations:

- `RegisterRegionOverviewNode(...)`
- `RegisterSeaOverviewNode(...)`
- `SetOverviewNodeUnlockRule(...)`
- `RegisterQuickMoveOverride(...)`

### 2.3 Custom Map Scene Runtime

Module idea:

- `LongLive.CustomMapRuntime`

Responsibilities:

- detect when a custom scene is loaded
- attach node components and click handlers
- bind player spawn placement
- project NPC positions into the map runtime
- keep scene-local map behavior independent from raw patch code

Suggested public operations:

- `RegisterCustomMapScene(...)`
- `BindSceneNodeGraph(...)`
- `BindSceneNpcProjection(...)`
- `BindSceneEntryActions(...)`

### 2.4 Metadata Extension

Module idea:

- `LongLive.Metadata`

Responsibilities:

- extend scene metadata
- extend map node metadata
- extend NPC info metadata
- normalize data-file loading and merging

Suggested public operations:

- `RegisterSceneMetadata(...)`
- `RegisterWorldNodeMetadata(...)`
- `RegisterNpcInfoMetadata(...)`
- `LoadMetadataFile(...)`

### 2.5 Mod Utility Bridge

Module idea:

- `LongLive.ModSupport`

Responsibilities:

- expose content-facing events and queries
- publish stable state keys
- provide high-level wrappers around common runtime actions

Suggested public operations:

- `PublishHostState(...)`
- `RegisterBridgeAction(...)`
- `RegisterBridgeQuery(...)`

## 3. Recommended Initial Build Order

The order matters.

### Phase 1

- scene routing
- warp abstraction
- current-place and current-map queries

This gives immediate reuse value and lets multiple later systems stop duplicating warp logic.

### Phase 2

- region overview node injection
- sea overview node injection
- unlock-rule abstraction

This covers the most obvious "custom map appears on the big map" problem.

### Phase 3

- custom scene runtime bootstrap
- node graph binding
- scene-local event binding

This is where a real custom map starts feeling native instead of just being a teleport destination.

### Phase 4

- metadata extension registry
- file merge pipeline
- optional authoring helpers

This is where repeated JSON patch logic gets pulled under `LongLive`.

## 4. What Should Stay Outside LongLive

The following should remain content-layer concerns for now.

- story scripts
- quest text
- one-off Lua events
- individual dungeon reward tables
- art-heavy content packages

`LongLive` should supply the stable host bridge, not absorb every content payload.

## 5. Architectural Rule

For each repeated ecosystem trick, `LongLive` should define:

- one typed model
- one host service
- one bridge-safe public surface

Downstream mods should not need to know which Harmony patch or host class made that behavior possible.

That is the actual value of a top-level library here.
