# Map Registry Planning

This document describes the current code-first map planning layer added to `LongLive.Mods` and the first read-only host snapshot adapter added to `LongLive.BepInEx`.

## 1. Current Split

The current work is intentionally split into two parts.

### Pure planning layer

Lives in:

- `src/LongLive.Mods/Maps/`

Responsibilities:

- typed scene descriptors
- typed world-map page descriptors
- typed highlight-region descriptors
- typed world-node descriptors
- host-ID allocation planning
- validation of duplicate IDs and broken references
- sample registry drafts for API-shape validation

This layer is pure C#.

It does not depend on Unity, BepInEx, Harmony, or Rust.

### Read-only host adapter layer

Lives in:

- `src/LongLive.BepInEx/Plugin/LongLiveMapSnapshotAdapter.cs`

Responsibilities:

- inspect currently loaded host scene metadata
- inspect current `AllMapManage` node registrations when present
- inspect overview-map UI presence when present
- project those observations into the same descriptor family used by the planning layer

This layer is observation-only.

It does not install custom maps yet.

## 2. Why This Split Matters

`LongLive` should not bind custom map design directly to host patch code.

If the descriptor and allocation layer is correct, then future host installation logic can evolve independently.

That gives three immediate benefits.

- API shape can stabilize before runtime injection is implemented.
- Validation can be tested without running the game.
- Host observation can reveal missing descriptor fields before installation code exists.

## 3. Current Planning Types

The planning layer currently includes:

- `LongLiveMapKind`
- `LongLiveSceneDescriptor`
- `LongLiveWorldMapPageDescriptor`
- `LongLiveHighlightRegionDescriptor`
- `LongLiveWorldNodeDescriptor`
- `LongLiveMapRegistryDraft`
- `LongLiveMapRegistryValidator`
- `LongLiveMapAllocationRegistry`
- `LongLiveMapRegistryPlanner`
- `LongLiveMapRegistryPlan`
- `LongLiveMapRegistrationReport`

The intent is that downstream mods should eventually register logical IDs such as:

- `my_mod.scene.pengsha-island`
- `my_mod.page.pengsha-sea`
- `my_mod.region.pengsha`
- `my_mod.node.pengsha-dock`

Then `LongLive` assigns host-side numeric IDs such as:

- `HostMapType`
- `HostHighlightId`
- `HostNodeIndex`
- `HostOutsideScenePos`

## 4. Current Validation Scope

The current validator checks:

- missing logical IDs
- duplicate page logical IDs
- duplicate scene logical IDs
- duplicate scene names
- duplicate highlight-region logical IDs
- duplicate node logical IDs
- missing page references from scenes
- missing page references from highlight regions
- missing page references from nodes
- missing target-scene references from nodes
- missing connected-node references

It also warns when logical IDs are not namespaced.

This is the minimum structure validation needed before future host installation work begins.

## 5. Current Sample Draft

The first sample builder lives in:

- `LongLiveMapRegistrySamples.CreateSeaIslandSample(...)`

Its purpose is to validate a realistic shape for:

- one custom sea overview page
- one highlight region
- one sea scene
- one island scene
- two nodes with a connection relationship

This is not meant to match final host installation behavior exactly.

It is meant to validate whether the current descriptor family is expressive enough.

## 6. Current Host Snapshot Adapter

The current read-only adapter projects host-side observations into the planning descriptors.

Current data sources include:

- `SceneNameJsonData.DataList`
- `AllMapManage.instance.mapIndex`
- `AllMapLuDainType.DataDict`
- `UIMapPanel.Inst`

Current limitations:

- page mapping is still heuristic
- node-to-scene binding is still heuristic
- not all scene types are modeled precisely yet
- no runtime install pipeline exists yet

That is acceptable for the current stage.

The adapter exists to surface modeling gaps, not to pretend installation is already solved.

## 7. Immediate Next Step

The next serious task should be to compare:

- host snapshots produced by the adapter
- sample drafts produced by the planner

Then identify which fields are still missing for a future installation layer.

Likely areas to refine next:

- page-switching metadata
- highlight-region geometry or binding details
- stronger node-to-scene routing metadata
- scene return-path semantics beyond simple `OutsideSceneName`

Only after that comparison is reasonably stable should `LongLive` begin implementing real custom map installation into the host runtime.
