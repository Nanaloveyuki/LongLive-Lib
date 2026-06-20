# Scene Routing Design

This document records the first concrete `LongLive.SceneRouting` design after the initial compatibility and ecosystem planning phase.

## 1. Goal

`SceneRouting` exists to turn host scene and warp behavior into a typed service.

It is not only a helper around `Tools.instance.loadMapScenes(...)`.

It is intended to become the routing middle layer between:

- raw host runtime behavior
- map registry plans
- future bridge adapters
- future custom map runtime registration

## 2. Current Split

The implementation is intentionally split into two layers.

### 2.1 Shared contracts

Located under:

- `src/LongLive.Mods/SceneRouting/`

Purpose:

- declare route kinds
- declare route requests and results
- declare routing snapshots
- declare the route catalog and registry
- stay pure C# and host-agnostic

### 2.2 Host implementation

Located under:

- `src/LongLive.BepInEx/Plugin/SceneRouting/`

Purpose:

- resolve current scene and place state from the live game runtime
- execute player and NPC routing through host APIs
- register the current base-game route catalog from map snapshot data
- host feature shells for future `MapOverview` and `CustomMapRuntime` integration

The current host layer now includes a small coordination stack:

- `LongLiveSceneRoutingHost`
- `LongLiveSceneRoutingCoordinator`
- `LongLiveHostMapSnapshotRouteSource`

The purpose of that stack is to keep installer code thin and to make future external route sources go through the same registration path.

The current public host entry points now exposed through `LongLivePluginContext` are:

- `SceneRouting`
- `SceneRoutingHost`
- `MapOverview`
- `CustomMapRuntime`
- `RegisterSceneRoutingFeature(...)`
- `RegisterSceneRouteSource(...)`
- `RegisterMapRegistryPlan(...)`
- `TryGetSceneRoutingFeature<TFeature>(...)`

That means future external C# modules do not need to reach into installer internals to participate in route registration.

## 3. Current Inputs

The current `SceneRouting` host implementation uses two input sources.

### 3.1 Direct host heuristics

Examples:

- `AllMaps`
- `S...`
- `F...`
- `Sea...`
- `FRandomBase`

These allow immediate routing even before a catalog is fully populated.

### 3.2 Registered route catalog

The host installer now captures the current base-game map snapshot and converts it into a `LongLiveMapRegistryPlan`.

That plan is then registered into the routing catalog.

This gives `SceneRouting` a first structured source of:

- route kind
- display name
- host outside-scene position
- scene logical identity

The current registration path is now formalized through:

- `ILongLiveSceneRouteRegistrationSource`
- `ILongLiveSceneRoutingRegistrationSink`
- `LongLiveSceneRoutingRegistration`
- `ILongLiveMapRegistryFeature`
- `LongLiveSceneRoutingCoordinator`

That means future external map modules do not need to register by talking directly to one concrete installer class.

It also means one validated `LongLiveMapRegistryPlan` can now be fanned out across multiple host feature registries instead of only being consumed by `SceneRouting` itself.

Late-added host features are now initialized automatically once the routing host has already entered the initialized state.

That matters because future external C# modules should be able to register additional routing-aware feature shells without depending on installer order.

## 4. Why The Catalog Matters

Without a route catalog, `SceneRouting` would remain a thin string-prefix helper.

That is not enough for the actual `LongLive` direction.

The catalog matters because it creates the first shared lookup layer between:

- `LongLiveMapRegistryPlan`
- host route execution
- future custom scene registration
- future `NextBridge` adapters

In other words, it is the first step from:

- host utility method

to:

- host routing subsystem

## 5. Current Supported Behavior

### 5.1 Player routing

Current support:

- world map routing
- region scene routing
- sea scene routing
- dungeon scene routing
- random dungeon scene routing

Current host behavior still maps to:

- `Tools.instance.loadMapScenes(...)`
- `SceneEx.LoadFuBen(...)`
- `PlayerEx.Player.NowMapIndex`

### 5.2 NPC routing

Current support:

- world map node routing
- region scene routing
- current active fuben runtime routing

Current limitation:

- cross-fuben NPC routing into a different inactive fuben scene is intentionally not supported yet

That is a deliberate guardrail.

It avoids pretending the host has a universal NPC scene-placement API when it actually has different runtime storage models for:

- world map NPC placement
- three-scene placement
- fuben-local placement

## 6. Deliberate Non-Goals In This Phase

This phase does not try to solve all map problems at once.

Not included yet:

- custom map runtime bootstrap
- overview-map node injection
- route unlock rules
- route transition effects
- `Next` bridge command/query adapters
- route persistence as a separate data layer

Those are separate layers that should build on top of the typed routing service.

The current codebase now includes placeholder host feature shells for:

- `MapOverview`
- `CustomMapRuntime`

They are still scaffolding only, but the integration seam now exists.

Those feature shells now also expose read-only catalog queries so later systems can consume registered map plans through stable lookup APIs instead of mutating internal registries directly.

`MapOverview` now also owns a first explicit routing-projection layer.

That projection layer turns world-map nodes into typed scene-routing addresses instead of leaving each future UI or bridge module to reconstruct route requests from raw node metadata on its own.

The current projection scope is intentionally narrow:

- node logical ID -> typed `LongLiveSceneAddress`
- node logical ID -> scene logical ID / scene name / route kind
- page / region grouping over those projections

This keeps `SceneRouting` authoritative for warp execution while allowing overview-facing systems to work with precomputed route intents.

`CustomMapRuntime` now also owns a first bootstrap catalog.

That bootstrap catalog does not install runtime scenes yet.

Its current role is to formalize the minimum identity needed for future runtime activation:

- runtime scene logical ID and scene name
- owning mod identity
- overview page and highlight-region ownership
- entry node and preferred entry index
- return scene identity and preferred return index
- typed entry and return `LongLiveSceneAddress` creation

This means later host runtime installers can consume a stable bootstrap contract instead of reverse-engineering return paths and overview ownership from raw scene descriptors.

The current dispatch path is:

1. a registration source creates a `LongLiveMapRegistryPlan`
2. the coordinator validates and registers the plan into `SceneRouting`
3. the same coordinator fans that plan out across map-registry-aware feature shells

The intended external usage path is now also explicit:

1. build a `LongLiveMapRegistryPlan` directly, or implement an `ILongLiveSceneRouteRegistrationSource`
2. call `LongLivePluginContext.RegisterMapRegistryPlan(...)` or `RegisterSceneRouteSource(...)`
3. let the host distribute the plan across `SceneRouting`, `MapOverview`, and `CustomMapRuntime`

## 7. Recommended Next Step

The next routing-focused step should be:

1. add explicit registration APIs for external map registry plans
2. define how `SceneRouting` cooperates with `MapOverview`
3. define how custom scene runtime bootstrap resolves entry indices and return routes
4. only then add bridge-level adapters

That keeps routing authoritative at the host layer before the bridge surface grows.
