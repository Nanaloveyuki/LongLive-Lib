# Native Core Feasibility

This document records the first-stage feasibility path for a Rust-native core in `LongLive-Lib`.

## 1. Current Goal

The current native-core goal is intentionally narrow:

- build a minimal Rust `cdylib`
- call it from C# through `DllImport`
- prove the cross-language route is viable in this repository

This is not yet the final native-core architecture.

## 2. Current Prototype Layout

- `native/longlive-native-core/`
  minimal Rust library with a C ABI
- `src/LongLive.NativeProbe/`
  small .NET console probe that loads the native library through `DllImport`

The probe is intentionally kept outside the main BepInEx host path.

That keeps the validation cheap and avoids polluting the host bootstrap before the FFI path is proven.

## 3. Why This Is Enough For Now

If this probe works, the main unanswered feasibility question is already resolved:

- Rust can produce a game-loadable native DLL for the repository
- C# can bind to it with an explicit library path

The remaining work after that is architecture, packaging, and lifecycle design, not basic possibility.

## 4. Expected Near-Term Usage Pattern

The intended future split is still:

- C# owns host integration, BepInEx glue, and Unity-facing behavior
- Rust owns pure or mostly-pure core logic with a thin C ABI

Examples of good early native-core candidates:

- numeric battle helpers
- deterministic rule evaluation
- text/token transformation helpers
- compact config or data validation engines

Examples of bad early native-core candidates:

- Unity object lifecycle code
- BepInEx plugin entry
- direct Harmony patch ownership

## 5. Verification Commands

Build the native DLL:

```powershell
cargo build --manifest-path .\native\Cargo.toml
```

Run the C# probe:

```powershell
dotnet run --project .\src\LongLive.NativeProbe\LongLive.NativeProbe.csproj
```

The current expected result is a small output showing ABI version, arithmetic result, and ready flag.

The probe now also exercises a small battle-oriented numeric helper:

- `longlive_native_core_compute_turn_damage`

This keeps the prototype closer to the actual game domain without prematurely turning the native layer into a full battle simulator.

For the earliest in-game validation stage, the native layer is still kept out of the main-menu diagnostics popup itself.

That popup is currently meant to validate:

- plugin load
- visible UI entry installation
- Next runtime visibility
- content inspection availability

The native layer remains verified out of process until host packaging and deployment details are ready.
