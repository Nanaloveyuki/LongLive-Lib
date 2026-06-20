# Map Compatibility Redirection Matrix

This document records which external library surfaces should be treated as map-related compatibility targets for `LongLive`, and which ones should remain reference-only.

The goal is not broad drop-in compatibility.

The goal is to redirect only the map-facing capability seams that directly support the current `LongLive` roadmap:

- `LongLive.SceneRouting`
- `LongLive.MapOverview`
- `LongLive.CustomMapRuntime`
- future map registry and map-type reservation work

## 1. Decision Rule

For map-related compatibility, `LongLive` should only redirect an external surface when all of the following are true:

- the surface represents a stable host capability rather than one mod's private patch stack
- the semantics can be preserved under `LongLive` ownership
- the redirected capability is directly useful for routing, overview-map integration, custom-map runtime bootstrap, or map metadata registration

If a library implements its own large Harmony patch stack for scene loading, map UI mutation, or custom runtime behavior, `LongLive` should study it as a reference implementation instead of trying to intercept it wholesale.

## 2. Current Classification

### 2.1 WhiteZe Tools

Relationship mode:

- adapter-compatible for routing and lightweight map queries
- reference-only for deeper utility internals that do not yet expose a stable bridge surface

Relevant map-facing surfaces already suitable for redirection:

- `IS_PlayerWarp`
- `IS_NpcWarp`
- `IS_PlayerWalk`
- `IS_PlayerMove`
- `IS_GetCurAllMapIndex`
- `IS_GetCurFubenIndex`
- `IS_GetCurSceneName`
- `IS_GetPlaceName`
- `IS_GetMapType`

Current status:

- these surfaces already map well onto `LongLive.SceneRouting` and related runtime queries
- they should continue being redirected through `LongLive` instead of depending on the WhiteZe runtime

Deferred map-adjacent WhiteZe utility operations:

- direct helper-only methods such as internal `WarpUtils` behaviors that do not currently present a stable `Next` adapter contract
- any future map-overview or custom-map registration semantics that are not already exposed through a practical bridge surface

Decision:

- keep redirecting the current routing/query subset
- do not force WhiteZe itself to become a dependency

### 2.2 VTools

Relationship mode:

- adapter-compatible for routing, map queries, and map-adjacent dialog events

Relevant map-facing surfaces suitable for redirection now:

- `PlayerWarp`
- `NpcWarp`
- `PlayerWalk`
- `PlayerMove`
- `GetCurAllMapIndex`
- `GetCurFubenIndex`
- `GetPlaceName`
- `CreateDongFu`
- `SetNowDongFuID`
- `SetDongFuName`
- `NpcMapRemoveNpc`

Reason:

- these are thin `Next` wrappers over stable host capabilities
- they align directly with routing and map-state ownership that `LongLive` already needs
- they do not require `LongLive` to imitate VTools mail, quest, or broader NPC utility features

Current status:

- routing/query compatibility already exists
- selected map-state commands should be redirected through `LongLive` as part of the map roadmap

Decision:

- continue expanding only the routing and map-state subset
- do not chase VTools-wide parity

### 2.3 LingJie

Relationship mode:

- reference-only for implementation study
- future registration target through top-level `LongLive` host services, not alias redirection

Observed map-facing implementation families:

- scene-load interception through Harmony
- overview-map node insertion
- sea-map UI patching
- custom scene-local map runtime controllers
- NPC placement into custom scene containers

Why this should not be redirected directly:

- the implementation is not a thin capability wrapper
- it is a mod-owned patch stack spanning scene load, UI graphs, and custom runtime control
- redirecting it wholesale would make `LongLive` responsible for preserving one concrete mod architecture rather than one reusable host capability

Decision:

- use LingJie as the reference model for:
  - overview-map node registration
  - custom runtime scene bootstrap
  - scene-route metadata projection
- do not build a fake LingJie drop-in layer

### 2.4 MoreFight

Relationship mode:

- reference-only for scene return, fuben placement, and combat-to-scene transition patterns

Relevant map-facing observations:

- uses scene return and final-scene restoration
- injects NPCs into fuben runtime containers
- depends on its own broader fight runtime assumptions

Decision:

- study its runtime placement patterns where useful
- do not redirect it as a compatibility target at this stage

### 2.5 MaiJiu Utility Layers

Relationship mode:

- reference-only for metadata and registration patterns
- capability-compatible later through `LongLive.Metadata` and map registry planning

Decision:

- use as input for registry and metadata design
- do not add compatibility redirection until a stable external bridge contract exists

### 2.6 JTools

Relationship mode:

- metadata-import compatible for scene registration
- reference-only for scene loading, asset-bundle bootstrapping, and map-event runtime systems

Observed stable surfaces:

- `DataManager.Inst.sceneNameEntities`
- `DataManager.Inst.MapInfos`

Why this is a good fit for LongLive:

- JTools already normalizes custom scene metadata into runtime-owned containers
- those containers are much thinner and more stable than its scene-loading helpers or event patches
- LongLive can translate that metadata into `LongLiveMapRegistryDraft` without inheriting JTools' runtime patch stack

Current decision:

- allow a metadata-import adapter for `SceneName` style registration
- delay `MapInfo` node projection until there is a reliable shared semantic mapping between JTools nodes and LongLive overview/runtime nodes
- do not redirect `AssetBundleManager`, `MapEventManager`, or custom scene boot loaders

## 3. Immediate Redirect Scope

The immediate redirect scope for the map roadmap should stay narrow.

Allowed now:

- player routing
- NPC routing
- scene and place queries
- map index and fuben index queries
- map-adjacent DongFu state commands
- NPC removal from host map containers when exposed as a thin bridge event

Not allowed now:

- direct imitation of a full custom map mod
- overview-map UI patch interception from feature mods
- scene-load patch replacement that is specific to one downstream mod
- broad non-map utility parity work

## 4. Practical Rule For Future Libraries

When a newly inspected library has map-related behavior, classify it with this question first:

- does it expose a stable bridge surface over a reusable host capability?

If yes:

- consider redirecting that surface into `LongLive`

If no, and the library instead owns a large patch stack:

- treat it as a reference implementation for the next top-level `LongLive` service

## 5. Current Summary

The current `LongLive` map compatibility strategy is:

- redirect WhiteZe and VTools where they present stable routing and map-state bridge calls
- import JTools scene metadata where it already exposes stable runtime containers
- keep LingJie and similar feature mods as reference implementations for future top-level host services
- avoid redirection work that would tie `LongLive` to one downstream mod's private scene or UI patch architecture

This keeps `LongLive` aligned with its actual role:

- own the reusable host choke points
- expose typed map services
- add compatibility aliases only where they help existing mods migrate onto that host-owned foundation
