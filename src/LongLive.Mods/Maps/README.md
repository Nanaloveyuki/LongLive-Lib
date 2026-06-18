# LongLive.Mods Maps

This folder contains the first code-first map registry planning layer for `LongLive`.

Current scope:

- typed scene descriptors
- typed world-map page descriptors
- typed highlight-region descriptors
- typed world-node descriptors
- host-ID allocation planning
- validation of logical references and duplicate identifiers

This layer is intentionally pure C# and host-agnostic.

It does not depend on BepInEx, Harmony, Unity, or Rust.
