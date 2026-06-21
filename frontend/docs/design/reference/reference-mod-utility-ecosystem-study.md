# Reference Mod Study: Utility Library Ecosystem

This document expands the earlier map-focused study into a broader utility-library ecosystem review.

Reference packages inspected locally:

- `F:\cache\白泽工具库`
- `F:\cache\埋久工具库`
- `F:\cache\微风的工具库`
- `F:\cache\奶油的前置库`
- `F:\cache\自定义动态立绘`
- `F:\cache\Jtools`
- `F:\cache\HY指令库`
- `F:\cache\NextMoreCommand`
- plus the map stack already reviewed in `灵界` and `多人战斗拓展`

## 1. Short Answer

The ecosystem is not centered around one single universal framework.

It is centered around a repeated set of host-side capability families.

Those families keep showing up under different names:

- scene routing and warp helpers
- `Next` dialog event and dialog query bridges
- JSON and metadata extension helpers
- AssetBundle and UI bootstrap loaders
- scene-transition and scene-hook wrappers
- Spine-based character rendering replacement
- data-cap expansion patches for otherwise fixed-size game tables

This is good news for `LongLive`.

It means `LongLive` does not need to imitate one existing utility library wholesale.

It should instead provide one typed host surface over these recurring capability families and let external libraries become either:

- reference implementations
- optional compatibility targets
- migration inputs for future adapter layers

## 2. Capability Buckets Found In The Ecosystem

### 2.1 Scene Routing And Warp

Strong references:

- `白泽工具库/Util/WarpUtils.cs`
- `白泽工具库/DialogEvent/Map/IS_PlayerWarp.cs`
- `白泽工具库/DialogEvent/Map/IS_NpcWarp.cs`
- `微风的工具库/VTools.cs`
- `微风的工具库/VNext/DialogEvent/PlayerWarp.cs`
- `微风的工具库/VNext/DialogEvent/NpcWarp.cs`
- `微风的工具库/VNext/DialogEnvQuery/GetCurAllMapIndex.cs`
- `微风的工具库/VNext/DialogEnvQuery/GetCurFubenIndex.cs`

Observed pattern:

- host code owns the actual routing logic
- `Next` events are thin wrappers over that host logic
- the wrapper normally writes result flags back into dialog temporary state
- the helper also exposes scene-kind and current-index queries

Practical conclusion:

`LongLive` should own this family directly under a typed host module such as `LongLive.SceneRouting`.

`Next`-style bridge events should be treated as adapters, not as the primary implementation surface.

### 2.2 Dialog Events, Queries, And Triggers

Strong references:

- `微风的工具库/VNext/DialogEvent/*`
- `微风的工具库/VNext/DialogEnvQuery/*`
- `微风的工具库/VNext/DialogTrigger/*`
- `奶油的前置库/RecreateSceneOverAnimation/NextSupport/*`

Observed pattern:

- utility libraries routinely expose host functionality through `Next`
- the underlying capability is still implemented in C# through Harmony patches or game object control
- the bridge layer is usually lightweight and stringly typed

Practical conclusion:

`LongLive` should not become `Next`-only.

It should instead define:

- typed C# services first
- optional `Next` bridge shims second
- stable capability names that can later be mirrored into `Next`

That keeps the host authoritative while still allowing existing content workflows to call into it.

### 2.3 Metadata, JSON, And Data-Shape Extension

Strong references:

- `埋久工具库/CuntomDungeonPatch.cs`
- `埋久工具库/MoreNPCInfoPatch.cs`
- `白泽工具库/Util/JsonUtil.cs`
- `白泽工具库/Util/ModConfigUtils.cs`
- `HY指令库/Core/HY_SeidExpansionPatch.cs`

Observed pattern:

- some mods merge new records into host metadata tables
- some mods provide file and config helpers around that merge process
- some mods patch hardcoded game array sizes so extra effect or buff entries can exist at all

The `HY` library is especially important here.

It shows a separate extension family beyond maps and UI:

- host data capacity expansion
- controlled loading of custom records into extended index ranges

Practical conclusion:

`LongLive` should eventually distinguish between two metadata services:

- `LongLive.Metadata` for merge and registry concerns
- `LongLive.DataExpansion` for array-size or index-range extension patches

Those are related, but they are not the same problem.

### 2.4 AssetBundle Loading And UI Bootstrap

Strong references:

- `奶油的前置库/AbPackageManager.cs`
- `奶油的前置库/TabExtraSetting/Patch/TabSetPanelSetting.cs`
- `奶油的前置库/RecreateSceneOverAnimation/*`
- `自定义动态立绘/Main.cs`

Observed pattern:

- utility libraries often load reusable UI prefabs or runtime assets from AssetBundles
- they cache those bundles in-process
- they then instantiate prefabs into existing game UI roots through patches

This category is broader than visual polish.

It is a general-purpose host bootstrap mechanism for:

- settings panels
- overlay widgets
- scene transition canvases
- custom editor or debug panels
- runtime support UI for other features

Practical conclusion:

`LongLive` should have a small reusable asset runtime, for example:

- `LongLive.AssetBundles`
- `LongLive.UiBootstrap`

This should cover bundle caching, prefab lookup, and safe attach points, without turning `LongLive` into an asset-heavy content pack.

### 2.5 Scene Transition And Scene Hook Control

Strong references:

- `奶油的前置库/RecreateSceneOverAnimation/Patch/ToolsLoadMapScene.cs`
- `奶油的前置库/RecreateSceneOverAnimation/Patch/ToolsLoadOtherScene.cs`
- `奶油的前置库/RecreateSceneOverAnimation/NextSupport/ShowSceneOverAnimation.cs`

Observed pattern:

- a utility library can intercept scene-load entry points, not just react after the fact
- custom transition systems are installed by replacing or redirecting the game's load path
- content-level commands then call the new transition manager instead of the raw scene loader

Practical conclusion:

This is an important architectural lesson for `LongLive`.

When a game choke point is unstable or visually poor, `LongLive` should prefer host-level redirection over content-level duplication.

That matches the broader compatibility direction already identified for `LongLive`.

### 2.6 Spine Rendering Replacement

Strong references:

- `自定义动态立绘/Main.cs`
- `自定义动态立绘/Patch/PlayerSetRandomFacePatch.cs`
- `自定义动态立绘/Patch/UIHeadPanelPatch.cs`
- `Jtools/Manager/SpineManager.cs`
- `Jtools/Patch/SpinePatch.cs`
- `Jtools/Manager/CgManager.cs`

Observed pattern:

- both libraries replace or augment the base portrait pipeline with Spine assets
- both load Spine assets from bundles and pair them with per-character configuration
- both patch many UI surfaces individually because the game does not expose one unified avatar render hook
- both treat animation, skin selection, position, scale, and masking as first-class runtime concerns

The two libraries differ in emphasis.

`自定义动态立绘` is closer to direct avatar replacement driven by `MJSpine` fields on character JSON.

`Jtools` is closer to a reusable Spine runtime framework with:

- skin persistence
- context-specific config modes
- wider patch coverage across shop, dialog, fight, save, tab, and auction views
- CG scene helpers through `CgManager`

Practical conclusion:

If `LongLive` ever supports character-render replacement, it should not copy either API directly.

It should define one narrower host abstraction first, such as:

- `LongLive.CharacterRender`
- or `LongLive.SpineRuntime`

That abstraction should cover:

- asset registration
- render profile registration
- context-based placement rules
- skin selection and persistence
- optional bridge hooks for content mods

It should not start by promising full compatibility with either existing library.

## 3. Classification Of The Newly Inspected Libraries

### 3.1 WhiteZe Tools

Best classified as:

- scene and warp utility reference
- map query reference
- metadata and config helper reference

Value to `LongLive`:

- high design value
- strong candidate for future compatibility aliases

Not recommended as a hard dependency.

### 3.2 MaiJiu Tooling

Best classified as:

- metadata extension reference
- custom dungeon registration reference
- shared helper precedent for file and JSON loading

Value to `LongLive`:

- high design value for registries and metadata layering

### 3.3 Ventulus VTools

Best classified as:

- broad utility facade over NPC, routing, mail, and event helpers
- `Next` bridge example for wrapping host services

Value to `LongLive`:

- strong evidence that ecosystem users want high-level host helpers
- useful compatibility target for selected routing and query operations

Risk:

- its surface area is already very broad, so `LongLive` should not chase one-to-one feature parity.

### 3.4 UniqueCream Extra Tools

Best classified as:

- asset bootstrap helper
- scene transition redirection reference
- UI extension reference

Value to `LongLive`:

- strong precedent for host-level redirection when a base engine entry point is too limited

### 3.5 MJSpine

Best classified as:

- focused character-render replacement library
- asset-plus-config loader for content-provided Spine packs

Value to `LongLive`:

- useful reference for content-facing data shape and mod folder scanning
- useful proof that a Workshop/local-mod content folder can supply rendering assets to a host plugin

### 3.6 JTools

Best classified as:

- broader runtime framework than a single-feature library
- strong reference for reusable Spine host services
- partial example of host managers that own save hooks, scene hooks, and UI patch families

Value to `LongLive`:

- very strong reference for future rendering abstractions
- useful reminder that some capabilities should be modeled as managers with persistent state, not just static helpers

### 3.7 HY BuffSeid

Best classified as:

- data-cap expansion library
- index-range extension patch

Value to `LongLive`:

- reference for future game-data extensibility work
- especially relevant if `LongLive` later wants a safe registry for custom `Seid` ranges or similar effect systems

## 4. What LongLive Should Absorb First

The ecosystem suggests a clear priority order.

### Phase 1

- scene routing
- player and NPC warp
- current-scene and current-index queries
- bridge-safe wrappers for those operations

### Phase 2

- world-map and custom-map integration
- metadata registry
- file merge helpers

### Phase 3

- asset bundle runtime
- UI bootstrap and host panel helpers
- scene transition redirection hooks where justified

### Phase 4

- data expansion registries for index-based game tables
- compatibility aliases for common utility-library calls

### Phase 5

- character rendering runtime if it becomes a real project goal

That rendering work is feasible, but it is not the right first abstraction to stabilize.

## 5. Recommended Compatibility Strategy

`LongLive` should use four relationship modes with external utility libraries.

### 5.1 Reference Only

Use the library to study patterns, but do not mirror its public API.

Good fit:

- one-off feature clusters
- highly project-specific helper surfaces

### 5.2 Capability-Compatible

Provide a `LongLive` operation with the same practical behavior, but under a cleaner typed surface.

Good fit:

- warp helpers
- place queries
- scene classification queries

### 5.3 Adapter-Compatible

Optionally expose a thin alias or bridge entry so existing content can migrate with minimal changes.

Good fit:

- selected `Next` dialog events
- stable map queries
- scene transition commands

### 5.4 Choke-Point Redirection

Patch the game-level entry point and route all downstream behavior through the improved implementation.

Good fit:

- scene loading transitions
- heavy or unstable host behaviors
- global data-extension hooks

This is the closest match to the direction already discussed for `LongLive`.

It preserves compatibility while keeping implementation ownership inside `LongLive`.

## 6. Recommended LongLive Module Families

Based on the expanded ecosystem study, the most justified module families are:

- `LongLive.SceneRouting`
- `LongLive.MapOverview`
- `LongLive.CustomMapRuntime`
- `LongLive.Metadata`
- `LongLive.DataExpansion`
- `LongLive.AssetBundles`
- `LongLive.UiBootstrap`
- `LongLive.NextBridge`
- `LongLive.ModSupport`

Possible later additions:

- `LongLive.CharacterRender`
- `LongLive.SceneTransition`

## 7. Architectural Rule Going Forward

When `LongLive` encounters a useful external library, the first question should not be:

- should `LongLive` depend on this library?

The first question should be:

- which host capability family does this library prove is missing from the base game?

Then `LongLive` should decide whether that family belongs in:

- the core host runtime
- an optional bridge layer
- or a future compatibility adapter

That keeps `LongLive` from turning into a loose collection of copied helpers.

It also keeps the project aligned with its actual value proposition:

- one controlled host abstraction layer between the game's fragile internals and higher-level mod code.
