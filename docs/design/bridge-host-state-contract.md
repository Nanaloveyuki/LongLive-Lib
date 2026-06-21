# LongLive Bridge Host State Contract

This document defines the first stable state contract between `LongLive.Host` and a future `LongLive.Bridge` package.

The initial goal is practical:

- the host publishes its presence and coarse capability state into Next runtime state keys
- a Bridge package reads those keys through normal Next runtime and Lua facilities

This avoids forcing the first Bridge implementation to reference the host assembly directly.

## 1. Why Use Next State Keys First

The host already depends on `NextRuntimeFacade` when runtime-backed installers become available.

That makes Next state publication the simplest first compatibility surface.

Benefits:

- Bridge packages can read the data through existing Next runtime APIs
- the transport is already understood by the game's mod ecosystem
- the first Bridge shell can stay lightweight and script-friendly
- host internals remain private while detection stays stable

This does not replace the C# handshake object.

It complements it.

The C# handshake remains the typed host-side source of truth, while the published state keys provide a low-friction bridge surface.

## 2. Published State Keys

Current host publication keys live in `LongLiveStateKeys`.

### Presence And Identity

- `longlive.host.present`
- `longlive.host.plugin_guid`
- `longlive.host.plugin_name`
- `longlive.host.version`

### Handshake And Runtime

- `longlive.host.handshake_version`
- `longlive.host.next_runtime_available`
- `longlive.host.published_at_utc`

### Capabilities

- `longlive.host.capabilities`

This value is a comma-separated capability list.

Current coarse examples include entries such as:

- `host-bootstrap`
- `next-runtime`
- `main-menu-entry`
- `bulk-item-use`
- `map-trace`
- `map-snapshot-export`
- `map-registry-planning`

### Diagnostics

- `longlive.host.install_root`

This path is intended for diagnostics only.

Bridge packages should not treat it as permission to overwrite host files.

## 3. Publication Timing

The host publishes these keys once Next runtime becomes available and runtime-backed installers are actually allowed to run.

That means:

- host process startup alone is not enough
- the publication happens at the point where LongLive can already talk to Next runtime safely

This is the right moment for Bridge detection because Bridge packages also depend on the same runtime path.

## 4. Bridge Read Pattern

The recommended first Bridge read pattern is:

1. read `longlive.host.present`
2. if it is `1`, read `version`, `handshake_version`, and `capabilities`
3. if it is not `1`, treat the host as missing or not yet published
4. show a user-facing reminder only when the Bridge package actually requires the host

This keeps detection logic simple.

## 5. Sample Bridge Package

The repository now includes a sample Next local-test Bridge shell under:

- `docs/samples/next-bridge-demo/`

Its current purpose is minimal:

- trigger on `EnterGame`
- run a Lua probe
- inspect the published LongLive host state keys
- emit a lightweight reminder when the host is missing
- emit a separate reminder when the host is present but incompatible
- cache a stable Bridge-local compatibility token in Next state for later debugging
- cache a separate detailed compatibility summary for diagnostics and log review

The sample also exposes a player-facing setting in `modConfig.json`:

- `longlive.bridge.enable_missing_host_reminder`

When this toggle is disabled, the Bridge still records host status but suppresses the pop-tip reminder.

Current Bridge-local status keys written by the sample shell:

- `longlive.bridge.last_status`
- `longlive.bridge.last_status_detail`
- `longlive.bridge.last_host_version`
- `longlive.bridge.last_host_present`
- `longlive.bridge.last_missing_host_reminder_enabled`
- `longlive.bridge.last_host_compatible`
- `longlive.bridge.last_host_compatibility_reason`
- `longlive.bridge.last_host_handshake_version`
- `longlive.bridge.last_host_capabilities`

`longlive.bridge.last_status` is the stable coarse token intended for Bridge-side gating:

- `ok`
- `missing`
- `incompatible:handshake`
- `incompatible:capability:<name>`

`longlive.bridge.last_status_detail` remains a verbose fact dump intended for diagnostics only.

It is not the final Bridge UX.

It is only the first reproducible compatibility shell.

## 6. Future Evolution

This contract should evolve conservatively.

Likely next steps:

- add a minimum-required-version field on Bridge-side content packages
- add structured capability requirements per package
- optionally add a richer UI shell for diagnostics and user-facing install help
- keep the published key names stable once external packages start depending on them

The sample Bridge shell now also demonstrates a minimal compatibility gate:

- required handshake version
- required capability names
- locale-aware reminder text loaded from `Lua/i18n`

That logic is still hard-coded inside the sample Lua file for now, but it is enough to distinguish:

- host missing
- host present but incompatible

Current sample compatibility requirement:

- handshake version `>= 1`
- capabilities `host-bootstrap` and `next-runtime`

The important rule is that Bridge detection should consume stable published facts, not ad hoc internal host details.
