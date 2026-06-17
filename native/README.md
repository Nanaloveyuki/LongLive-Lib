# Native Prototype

This folder contains minimal Rust-native feasibility probes for `LongLive-Lib`.

The current goal is intentionally narrow:

- confirm that a Rust `cdylib` can be built in the current repository
- confirm that a C# project can call it through `DllImport`
- avoid pulling the native layer into the main host/runtime path too early

This is a feasibility track, not the full native-core architecture yet.
