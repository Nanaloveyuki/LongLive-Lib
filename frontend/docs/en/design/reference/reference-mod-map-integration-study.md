# Reference Mod Study: LingJie And Related Tool Libraries

This document summarizes what the `LingJie` mod stack and its dependencies suggest about a practical implementation path for `LongLive`.

Reference packages inspected locally:

- `F:\cache\灵界`
- `F:\cache\多人战斗拓展`
- `F:\cache\埋久工具库`
- `F:\cache\NextMoreCommand`
- `F:\cache\白泽工具库`

## 1. Short Answer

Yes.

`LongLive` should avoid rebuilding every low-level trick from scratch.

The inspected mods strongly suggest that a large part of the current ecosystem is already converging on the same practical patterns:

- patch host map and scene entry points through Harmony
- treat scene metadata, node metadata, and overview-map UI as separate integration layers
- use utility libraries to normalize warp, config, JSON loading, and metadata extension
- let content-side `Next` packages provide data and events while host-side DLLs own the fragile runtime hooks

That means the right `LongLive` direction is not:

- copy one specific mod implementation whole

The right direction is:

- absorb the repeated integration patterns into one top-level host bridge and expose stable, typed APIs over them

## 2. What LingJie Actually Does

`LingJie` is not just a simple landmark or one extra entry button.

It implements a new map experience by combining several layers.

### 2.1 Host-side runtime patching

Examples found in `plugins/package/LingJie/Patch/`:

- `ScenePatch/ToolsPatch.cs`
- `UIMapSeaPatch.cs`
- `UIMapNingZhouPatch.cs`
- `MapInstComportPatch.cs`
- `MapSeaCompentPatch.cs`
- `EndlessSeaMagPatch.cs`
- `AllMapShowLinePatch.cs`

This means the mod does not rely on one clean built-in registration API.

Instead, it patches:

- scene loading
- overview map node lists
- quick-move logic
- all-map visuals
- sea map behavior
- runtime scene transitions

### 2.2 Custom scene runtime inside the loaded scene

Examples found in `plugins/package/LingJie/Scene/`:

- `AllMapBase.cs`
- `AllMapComponent.cs`
- `AllMapClick.cs`
- `AllMapNpcController.cs`
- `AllMapJson.cs`
- `LudianJson.cs`

This is especially important.

`LingJie` is not only extending the base game's world-map UI.

It also constructs its own runtime map controller inside the custom scene:

- attaches `AllMapManage`
- builds node components dynamically
- resolves event bindings from JSON
- positions the player marker
- projects NPC presence into that map runtime

This is effectively a custom map runtime layer.

### 2.3 Data-driven map shape

`LingJie` also uses JSON and bundled assets to define scene behavior.

Examples:

- `plugins/AB/Json/SceneJson.json`
- `plugins/AB/MJScene/...`
- `plugins/AssetBundle/Scene/...`

So the actual implementation is hybrid:

- code controls host integration and runtime behavior
- data controls scene content and map definitions

## 3. What The Tool Libraries Suggest

The tool libraries are arguably even more valuable than the big feature mods, because they show which low-level operations keep repeating across projects.

### 3.1 WhiteZe tools: warp and map utility abstraction

`白泽工具库` contains reusable map helpers instead of one-off content logic.

Important examples:

- `Util/WarpUtils.cs`
- `DialogEvent/Map/IS_PlayerWarp.cs`
- `DialogEvent/Map/IS_NpcWarp.cs`
- `DialogEnv/Map/IS_GetMapType.cs`
- `DialogEnv/Map/IS_GetCurAllMapIndex.cs`
- `DialogEnv/Map/IS_GetCurFubenIndex.cs`

This is strong evidence that the ecosystem already needs a shared abstraction for:

- player warp
- NPC warp
- scene kind detection
- current node index lookup
- current fuben index lookup
- name resolution for the current place

`LongLive` should absolutely absorb this category into a first-class API instead of forcing downstream mods to hand-roll it repeatedly.

### 3.2 WhiteZe and MaiJiu libraries: JSON and metadata extension

Examples:

- `JsonUtil.cs`
- `ModConfigUtils.cs`
- `MoreNPCInfoPatch.cs`
- `CuntomDungeonPatch.cs`

These show another repeated pattern:

- extend game metadata tables from external JSON
- normalize file lookup and config persistence
- patch host data loaders to recognize extra records

This maps directly onto a future `LongLive` host content-extension layer.

### 3.3 MoreFight: feature modules anchored by host hooks plus content packages

`多人战斗拓展` uses the same general structure:

- host DLL for fragile runtime and UI control
- `Next` package for data, triggers, Lua, and assets
- persistent custom state managed in code

This reinforces the idea that `LongLive` should be the host-side backbone, not a replacement for every content-layer tool.

## 4. Practical Conclusion For LongLive

The right top-level strategy is:

- do not manually rebuild each one-off patch path mod-by-mod
- do not directly copy one reference mod's concrete feature stack
- do centralize the repeated host integration patterns into reusable, typed services

In other words, `LongLive` should unify the ecosystem's repeated low-level solutions, not clone one finished mod.

## 5. What LongLive Should Abstract First

Based on the inspected mods, the first reusable top-level abstractions should be these.

### 5.1 Scene and warp service

`LongLive.SceneService`

Responsibilities:

- load `S...`, `F...`, `Sea...`, and `AllMaps`
- derive or override `NowMapIndex`
- support player warp and NPC warp
- support scene-kind detection
- resolve current place names

This should absorb the recurring logic now visible in `WarpUtils.cs` and similar helpers.

### 5.2 Map overview injection service

`LongLive.MapOverviewService`

Responsibilities:

- add world-map overview nodes
- add sea overview nodes
- set node names, icons, and warp targets
- manage lock and visibility conditions
- optionally patch quick-move calculation for custom destinations

This should cover the pattern currently visible in `UIMapSeaPatch.cs` and `UIMapNingZhouPatch.cs`.

### 5.3 Custom map runtime installer

`LongLive.CustomMapRuntime`

Responsibilities:

- attach runtime components when a custom scene loads
- build node controllers from data
- bind click handlers to events or scripts
- place player marker and map NPCs
- manage custom scene-local map rules

This is the reusable version of what `LingJie` currently does through `AllMapBase` and related classes.

### 5.4 Host metadata extension layer

`LongLive.MetadataRegistry`

Responsibilities:

- extend scene metadata
- extend node metadata
- extend NPC info metadata
- expose a stable source of truth for extra records loaded from files or code

This should take over the recurring JSON patch pattern seen across `MoreNPCInfoPatch`, custom dungeon patches, and scene JSON add-ons.

### 5.5 Capability-oriented tool surface for content mods

`LongLive.NextBridge` or equivalent bridge-facing service

Responsibilities:

- expose stable `Next` state keys for host presence and capabilities
- expose stable dialog events or command shims for common host actions
- keep content-side mods from depending on fragile Harmony details

## 6. What LongLive Should Not Do Yet

The inspected mods also show what not to over-design too early.

Do not start with:

- a giant universal JSON DSL for every map behavior
- a promise that all custom map features are data-only
- a full replacement of `Next`
- a full copy of another tool library's public API surface

The underlying host behavior is still too patch-oriented and context-specific.

The stable part is not the exact patch code.

The stable part is the category of operation.

## 7. Recommended Implementation Direction

For `LongLive`, the recommended path is:

1. formalize a scene and warp abstraction first
2. formalize overview-map node injection second
3. formalize custom scene map runtime installation third
4. unify metadata extension and persistence helpers underneath those layers
5. only then design a higher-level authoring model

That sequence lets `LongLive` provide real value quickly without hand-authoring each future map feature from scratch.

## 8. Final Judgment

Yes, these reference mods are useful.

But their real value is not that they provide a single ready-made implementation to copy.

Their value is that they reveal the repeated host integration primitives already needed by the mod ecosystem.

`LongLive` should become the top-level implementation of those primitives.
