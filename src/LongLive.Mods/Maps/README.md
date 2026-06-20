# LongLive.Mods Maps

This folder contains the first code-first map registry planning layer for `LongLive`.

Current scope:

- typed scene descriptors
- typed world-map page descriptors
- typed highlight-region descriptors
- typed world-node descriptors
- host-ID allocation planning
- validation of logical references and duplicate identifiers
- pure C# registries for future map-overview and custom-map-runtime feature shells
- read-only catalog query helpers for future host adapters and bridge layers
- map-overview routing projections from world nodes to typed scene-routing addresses
- custom-map-runtime bootstrap descriptors for entry/return route planning
- scene-local topology catalogs for runtime node graphs that do not belong on the world overview map

This layer is intentionally pure C# and host-agnostic.

It does not depend on BepInEx, Harmony, Unity, or Rust.
