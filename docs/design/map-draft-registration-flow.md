# Map Draft Registration Flow

This document records the intended host-facing flow for external C# modules that want to register map metadata into `LongLive`.

## 1. Why This Entry Point Exists

`LongLive` now owns three related host-facing map subsystems:

- `SceneRouting`
- `MapOverview`
- `CustomMapRuntime`

All three consume overlapping map metadata.

If external modules had to register those pieces one registry at a time, the integration surface would become brittle very quickly.

The standard draft-registration path exists to keep one source of truth.

## 2. Standard Flow

The standard flow for an external C# module is:

1. create a `LongLiveMapRegistryDraft`
2. submit it through `LongLivePluginContext.RegisterMapRegistryDraft(...)`
3. let the host planner validate logical references and allocate host-side numeric IDs
4. let the host coordinator fan the resulting plan out across `SceneRouting`, `MapOverview`, and `CustomMapRuntime`

## 3. When To Use `CreateMapRegistryPlan(...)`

Use `LongLivePluginContext.CreateMapRegistryPlan(...)` when a module needs to inspect the result before registration.

Examples:

- diagnostics tooling
- unit-style validation checks inside a mod
- migration helpers that want to inspect assigned map-type, node, or highlight IDs

Use `RegisterMapRegistryDraft(...)` when the module simply wants the host to register the map set.

## 4. Why This Matters For Compatibility

This is the preferred future seam for map-related compatibility work.

If a third-party library already has enough metadata to describe:

- scenes
- overview pages
- highlight regions
- world nodes

then the compatibility layer should aim to translate that metadata into a `LongLiveMapRegistryDraft`.

It should not directly mutate internal host registries or clone one downstream mod's private patch architecture.

## 5. Current Practical Boundary

This path is suitable today for:

- code-first custom map registration
- compatibility adapters that can extract stable map metadata
- future C# map APIs built on top of `LongLive`

It is not yet a full runtime installer for custom scenes or overview UIs by itself.

That still belongs to later host-side implementation layers.
