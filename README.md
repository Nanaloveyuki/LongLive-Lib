# LongLive-Lib

`LongLive-Lib` is an experimental library repository for the Tale of Immortal mod ecosystem.
Its current direction is to build a cleaner and more maintainable developer-facing layer on top of `BepInEx` and `Next`.

In practical terms, the repository is converging on a layered runtime model:

- `LongLive.Host` in `BepInEx/plugins`
- future `LongLive.Bridge` or content shells in the normal `Next` mod path

At this stage, the repository is intentionally lightweight. The focus is on source study, API inventory, project boundaries, and a minimal compile-ready skeleton, not on a full production framework yet.

## Current Status

- Code: compile-ready bootstrap skeleton with first runtime-backed wrappers
- Reference sources: stored under [`devdocs/`](./devdocs/)
- Main direction: `C# host layer + Rust core`

## Planned Layering

The current working split is:

- `LongLive.Core`
  Shared logic, models, result/error types, and future Rust-oriented core abstractions
- `LongLive.BepInEx`
  Host-layer integration for BepInEx concerns such as plugin entry, logging, configuration, and lifecycle glue
- `LongLive.Next`
  High-level wrappers over Next, including event execution, command registration, state access, and UI helpers
- `LongLive.I18n`
  Localization and text-resource related concerns, to be split further when the runtime boundary is clearer

## Constraints Right Now

- The full host development environment is not set up yet
- A full framework is intentionally postponed
- The current priority is documentation, boundary definition, and API stabilization

## Repository Contents

- [`devdocs/BepInEx-master/`](./devdocs/BepInEx-master/): local BepInEx source and documentation copy
- [`devdocs/Next-main/`](./devdocs/Next-main/): local Next source and documentation copy
- [`docs/design/bootstrap-notes.md`](./docs/design/bootstrap-notes.md): current bootstrap assumptions and constraints
- [`docs/design/next-runtime-design.md`](./docs/design/next-runtime-design.md): current `LongLive.Next` API and bootstrap runtime design
- [`docs/next-runtime-usage.md`](./docs/next-runtime-usage.md): current usage patterns for the bootstrap runtime facade
- [`docs/next-runtime-examples.md`](./docs/next-runtime-examples.md): short examples for the facade, extensions, and state-key helpers
- [`docs/design/bepinex-host-bootstrap.md`](./docs/design/bepinex-host-bootstrap.md): current `LongLive.BepInEx` host bootstrap strategy
- [`docs/deploy-guide.md`](./docs/deploy-guide.md): short daily deployment guide and script layout
- [`docs/design/distribution-and-bridge-strategy.md`](./docs/design/distribution-and-bridge-strategy.md): planned Host, Bridge, and content-package distribution model
- [`docs/design/bridge-host-state-contract.md`](./docs/design/bridge-host-state-contract.md): published Next-state contract for Bridge-side host detection
- [`docs/design/mod-schema-draft.md`](./docs/design/mod-schema-draft.md): first draft of the declarative JSON-mod schema
- [`docs/mod-loader-usage.md`](./docs/mod-loader-usage.md): current usage pattern for JSON-mod loading and validation
- [`docs/design/content-schema-draft.md`](./docs/design/content-schema-draft.md): first draft of content-oriented JSON-mod declarations
- [`docs/design/next-content-backend-design.md`](./docs/design/next-content-backend-design.md): planned boundary for a future Next-oriented content backend
- [`docs/design/native-core-feasibility.md`](./docs/design/native-core-feasibility.md): first-stage Rust native-core feasibility path
- [`src/`](./src/): the first compile-ready C# project skeleton

## Recommended Next Steps

1. Keep documenting which `LongLive.Next` APIs are intended to be stable and which runtime details are bootstrap-only
2. Continue separating what should wrap Next directly from what would require lower-level BepInEx or Harmony work
3. Add minimal runtime composition helpers before introducing a real plugin host project

## Daily Deployment

Use the unified script entry for normal local work:

```powershell
./scripts/longlive.ps1 -Action host-redeploy
```

See [`docs/deploy-guide.md`](./docs/deploy-guide.md) for the categorized script layout and the small set of commands you actually need day to day.

Until then, Unity projects, AssetBundle workflows, and deep patching should remain out of scope.
