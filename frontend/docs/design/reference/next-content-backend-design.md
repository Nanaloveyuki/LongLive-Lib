# Next Content Backend Design

This document defines the planned boundary for a future Next-oriented content installation backend.

It does not implement runtime injection yet.

## 1. Why This Boundary Exists

`LongLive.Mods` now supports declarative content entries for:

- items
- skills
- buffs
- asset mappings

However, the runtime shape behind those entries is not one single API.

Based on the current Next source mirror, content flows through multiple layers:

- editor-side content models such as `ModItemData`, `ModSkillData`, and `ModBuffData`
- runtime JSON data loading triggered by `JsonDataPatch`
- runtime resource overrides routed through patches such as `ModResourcesLoadSpritePatch` and `ModResourcesLoadTexturePatch`

Those are related, but they are not the same abstraction.

## 2. Design Rule

`LongLive` should not bind its content installation contract directly to Next editor types.

The content installation boundary should remain centered on:

- `LongLive` JSON package models
- package root and manifest context
- install-result reporting

This is why `ILongLiveContentRegistry` now receives typed content install requests instead of bare content DTOs.

## 3. Current Request Context

The current request model carries:

- the full `LongLiveModPackage`
- the mod root directory
- the manifest
- the concrete content DTO being processed

This is the minimum context required for a real backend to:

- resolve asset source files relative to the package root
- make backend decisions from manifest identity or version
- emit stable install diagnostics

## 4. Planned Backend Split

The likely future split is:

- `LongLiveDeferredContentRegistry`
  default no-op/deferred implementation used in compile-ready environments
- `LongLiveNextContentRegistry`
  a Next-oriented backend that translates `LongLive` content requests into real runtime contribution behavior

That future Next backend will probably need to split its own work again into at least two internal areas:

- JSON/game-data contribution handling for items, skills, and buffs
- resource override handling for textures, sprites, and related asset mappings

## 5. Next Runtime Facts Observed

From the local Next source mirror:

- `JsonDataPatch` triggers `ModManager.FirstLoadAllMod()` during game JSON initialization
- runtime resource replacement is intercepted through Harmony patches on `ModResources`
- mod resources can come from files or asset bundles through types under `SkySwordKill.Next.Res`

This suggests that a future backend is not only a data conversion problem.

It is also a runtime lifecycle and resource-resolution problem.

## 6. Immediate Constraint

Until a real host environment is wired and validated, do not implement a fake runtime installer that pretends content is already injected.

Instead:

- keep the request/context boundary stable
- keep reporting explicit `Deferred` results by default
- add a real backend only when it can be validated against the actual host process

## 7. What The Future Backend Should Probably Expose

When the host environment is ready, a Next-oriented backend will likely need:

- access to `NextRuntimeFacade` or a stronger typed host bridge
- access to host lifecycle timing so content registers before or during Next mod load
- file and asset resolution helpers rooted in the mod package directory
- explicit handling for resource kinds such as `portrait`, `sprite`, and `texture`

That work should live beside the existing installation contracts, not inside the schema models themselves.
