# Bootstrap Notes

This file records the current assumptions and constraints for the repository while it is still in the pre-environment, pre-framework stage.

## 1. Current Repository State

The root repository currently keeps only a minimal set of files and folders:

- `LICENSE`
- `.gitignore`
- `devdocs/`
- `src/`

This is deliberate.

The goal at this stage is:

- source study first
- no premature framework scaffolding
- no mixing of host integration, resource workflows, and native bridge concerns

## 2. Local Reference Sources

### BepInEx

Path: `devdocs/BepInEx-master/`

The most relevant parts right now are:

- `BepInEx.Core/`
  logging, configuration, paths, and base contracts
- `docs/`
  build and development documentation
- `Runtimes/`
  runtime-specific host implementations

### Next

Path: `devdocs/Next-main/`

The most relevant parts right now are:

- `Next/Next.csproj`
  real dependency and host assumptions
- `Next/Scr/Core/Helper.cs`
  the main quick-entry API surface
- `Next/Scr/Core/DialogEvent/`
  custom dialog command extension model
- `Next/Scr/Core/DialogSystem/`
  dialog, trigger, and expression runtime
- `Next/Scr/Core/FGUI/`
  custom UI support
- `doc/`
  mod-author-facing documentation

## 3. Current Design Principles

### 3.1 Define Layers Before Expanding Implementation

Do not start with a large catch-all DLL.

The future structure should keep these concerns separate:

- host-facing integration
- pure logic and shared models
- native core logic, if Rust is introduced later

### 3.2 Reuse Next Before Patching Around It

The preferred order is:

1. reuse an existing public Next API if it already solves the problem
2. add a cleaner C# wrapper over it
3. only drop to BepInEx / Harmony when there is a real capability gap

### 3.3 Keep the Cross-Language Boundary Thin

If the repository eventually adopts `C# + Rust`:

- C# should own the host layer and the integration layer
- Rust should own the heavy logic, rules, text processing, and other pure-core concerns

Rust should not be forced into the BepInEx plugin-host role.

## 4. Things That Should Stay Out of Scope for Now

- no Unity project setup yet
- no AssetBundle workflow yet
- no large Harmony patch layer yet
- no full FFI framework yet

Those all add complexity before the API boundaries are stable enough to justify them.

## 5. More Useful Immediate Work

The better current sequence is:

1. inventory Next's public C# surface
2. define the boundary between `LongLive.BepInEx` and `LongLive.Next`
3. pick one minimal runtime-facing target
4. only then decide whether a real host plugin project is needed

## 6. Good Candidates for the First Minimal Target

Each of the following is smaller and more useful than "start the full framework":

- a documented `LongLive.Next` API surface
- a minimal BepInEx host shell design
- a small event/command builder concept over Next
- a localization key/value model and directory convention

## 7. What the Repository Should Add Next When Code Expands

- `docs/api-inventory.md`
  a stable inventory of the Next public C# surface
- `docs/module-boundaries.md`
  the intended responsibility split between modules
- stronger `src/` project expansion only after the API surface is stable enough

The repository intentionally did not start with a larger code tree before these assumptions were documented.
