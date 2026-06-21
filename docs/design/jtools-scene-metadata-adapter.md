# JTools Scene Metadata Adapter

This document records the first `JTools` compatibility seam accepted into `LongLive`.

## 1. Scope

The current adapter is intentionally narrow.

It reads `JTools` runtime-owned scene metadata from:

- `DataManager.Inst.sceneNameEntities`

It then converts that metadata into a `LongLiveMapRegistryDraft` containing:

- pages
- highlight regions
- scene descriptors

It does not generate world-map overview nodes.

Instead, `MapInfo` is now treated as a separate scene-local topology source.

## 2. Why This Boundary Was Chosen

`JTools` contains several different map-adjacent systems:

- scene metadata builders
- map-info graph containers
- asset-bundle scene loading helpers
- map-event runtime logic

Only the scene metadata container is a clear low-risk import seam right now.

That data is already normalized, host-facing, and close to the same fields `LongLive` needs:

- scene id
- scene display name
- map type
- sell/page grouping
- highlight id
- outside-scene name
- outside-scene position

## 3. Current Mapping

The current adapter maps `JTools` sell groups into three LongLive pages:

- `tierneyjohn.jtools.page.ningzhou`
- `tierneyjohn.jtools.page.near-sea`
- `tierneyjohn.jtools.page.far-sea`

It also preserves host-like metadata on each generated scene descriptor:

- `HostMapType`
- `HostHighlightId`
- `HostOutsideScenePos`

## 4. Scene-Local Topology Boundary

`JTools` also exposes `DataManager.Inst.MapInfos`.

That data is not treated as world-map overview input.

It is closer to a scene-local node graph with:

- node names
- local positions
- hidden/city flags
- static avatar bindings
- adjacency edges

LongLive now treats that as `SceneLocalTopology` metadata under `CustomMapRuntime` instead of forcing it into `MapOverview`.

The host now also exposes a runtime snapshot and optional logging path for this imported topology layer.

That makes it possible to verify, in-game, whether:

- the current scene resolved to a registered LongLive scene
- a scene-local topology was matched for that scene
- the imported topology contains the expected node sample

## 5. Current Limitations

The current adapter does not yet import:

- world-map node positions
- node-to-scene overview bindings
- map-event data
- asset-bundle loading metadata

This is deliberate.

`MapInfo` appears to be a scene-local graph container, but it is not a one-to-one match for LongLive's world-overview node abstraction.

## 6. Intended Next Step

The next useful extension should be:

1. expose scene-local topology query helpers more broadly
2. connect imported topology batches to future runtime node binding work
3. keep world-overview projection as a separate decision that requires explicit semantics, not guesswork

Until then, the adapter should remain a scene-registration plus scene-topology import seam rather than a full JTools runtime compatibility layer.
