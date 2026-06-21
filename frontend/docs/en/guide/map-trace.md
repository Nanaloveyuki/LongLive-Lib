# Map Trace

This document describes the current read-only map tracing layer in `LongLive.BepInEx`.

## 1. Purpose

The current map trace is an exploration tool.

It is intended to help answer questions such as:

- how map scenes are entered at runtime
- how world-map nodes register into `AllMapManage`
- how current node graphs look after scene load
- how the overview map UI is structured at runtime
- which metadata tables appear to drive world-map and overview-map behavior

It does not modify map behavior.

## 2. Config Switches

Both switches live under the `LongLive` config section.

- `EnableMapTrace`
  Enables read-only Harmony map tracing. Effective only when `EnableDebugLogging=true`.
- `EnableMapTraceVerbose`
  Enables additional node-sample output and repeated structure snapshots. Effective only when both `EnableDebugLogging=true` and `EnableMapTrace=true`.

## 3. Current Coverage

The current probe set logs these areas.

- `Tools.loadMapScenes(...)`
  Logs requested target scene, current scene, and player scene-return fields.
- Unity `SceneManager.sceneLoaded`
  Logs loaded scene snapshot and scene metadata.
- `AllMapManage.Awake()`
  Logs top-level world-map container state.
- `AllMapManage.RefreshLuDian()`
  Logs refreshed world-map runtime state when the structure signature changes.
- `BaseMapCompont.StartSeting()`
  Logs node registration into the world-map runtime container.
- `BaseMapCompont.setAvatarNowMapIndex()`
  Logs runtime movement into world-map nodes.
- `MapInstComport.setAvatarNowMapIndex()`
  Logs runtime movement into fuben-map nodes.
- `UIMapPanel.OpenDefaultMap()` / `OpenMap(...)` / `ShowPanel()` / `OpenHighlight(...)`
  Logs overview-map open mode and selected state.
- `UIMapNingZhou.Show()`
  Logs Ningzhou overview-map structure.
- `UIMapSea.Show()`
  Logs sea overview-map structure.

## 4. Log Prefixes

All map trace log lines use:

```text
[MapTrace]
```

Useful search keywords include:

- `Tools.loadMapScenes prefix`
- `AllMapManage.Awake`
- `AllMapManage.RefreshLuDian`
- `BaseMapCompont.StartSeting`
- `UIMapPanel.ShowPanel`
- `UIMapNingZhou.Show`
- `UIMapSea.Show`

## 5. Expected Output Shape

The current trace tries to summarize rather than dump everything.

Examples of summarized fields include:

- scene metadata from `SceneNameJsonData`
- node metadata from `AllMapLuDainType`
- node type counts in `AllMapManage.mapIndex`
- sample node graph entries
- overview-map highlight IDs
- overview-map node warp targets

When verbose mode is disabled, repeated structure logs are suppressed unless the snapshot signature changes.

## 6. Intended Next Use

This trace layer is meant to support the next design phase:

- typed scene descriptor models
- typed world-map page descriptors
- typed world-node descriptors
- validation-first registration planning for custom world maps

The immediate goal is observability, not installation.
