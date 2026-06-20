# LongLive.BepInEx

This project is the formal home for BepInEx-facing host code.

It should be treated as `LongLive.Host` in practical distribution terms.

## Current Status

The project is present in the main solution now, but its actual plugin entry source files are gated behind local host references.

That means the default repository build can stay green even on a machine that does not currently have:

- the game installation
- Unity managed runtime assemblies
- a final local BepInEx runtime directory

## Local Build Switch

To compile the actual plugin entry shell:

1. copy `eng/LocalReferences.props.example` to `eng/LocalReferences.props`
2. set `LongLiveEnableLocalHostReferences` to `true`
3. fill in `McsGameRoot`
4. fill in `BepInExCoreDir`

Once enabled, the `Plugin/` source files are compiled and the project references the local host runtime assemblies.

The current main-menu validation entry now follows Next's own integration style more closely. It patches `MainUIMag.OpenMain` through Harmony, clones the `神仙斗法` button at the same UI lifecycle point, and applies the LongLive sprite set directly onto `Image` and `FpBtn`.

The optional JSON-mod demo bootstrap is also explicit opt-in:

- enable `EnableJsonModDemoInstall`
- set `JsonModDemoPath` to a real package directory
- optionally set `ContentBackend` to `Deferred` or `Next`

There is also an optional experimental battle safety switch:

- enable `EnableExperimentalBattleGuard`

When battle trace is enabled together with debug logging, the host now also emits automatic battle-level summaries at key combat exit checkpoints such as:

- `Fight.FightResultMag.ShowVictory()`
- `Fight.FightVictory.SetVictory()`
- `Avatar.die()`
- the next battle reset

Those summaries are intended to support broad detection across unknown skills rather than requiring one-by-one manual log counting.

Current summary groups include:

- top negative-HP avatars
- top damage-attempt and `recvDamage(...)` skill IDs
- top `Spell.onBuffTick(...)` skill IDs
- top guard-blocked skill IDs
- top `Buff.onLoopTrigger`, `Buff.loopRealizeSeid`, and `Buff.ListRealizeSeid71` `buffID` / `seid` counters

Current behavior of the experimental guard:

- it targets non-player combat avatars only
- it short-circuits confirmed post-death `recvDamage(...)`, `Buff.*`, and later `Spell.onBuffTick(...)` re-entry for the same marked skill path
- it is intended to reduce battle stalls and repeated audio storms caused by runaway post-death processing
- it is not meant to be a final combat rebalance system

There is also an optional read-only runtime inspection path:

- enable `EnableContentRuntimeInspection`
- optionally enable `EnableDebugLogging` for per-type property/method detail

The host project does not assume a repository-local sample path at runtime.

The current `Next` content backend option is only a host-side shell. It preserves the future runtime injection composition point while still reporting deferred content installation today.

There is now also an optional host map snapshot export path:

- enable `EnableAutoExportMapSnapshot`
- keep `EnableDebugLogging` enabled

There is also an optional scene-local-topology runtime log path for imported custom scene graphs such as `JTools` `MapInfo`:

- enable `EnableSceneLocalTopologyLogging`
- optionally enable `EnableSceneLocalTopologyVerbose`
- keep `EnableDebugLogging` enabled

When active, the host exports the current observed map snapshot as JSON under:

```text
BepInEx/plugins/LongLiveExports/
```

The same export can also be triggered from the `LongLive Diagnostics` button on the main menu.

The host also now includes a first visible in-game validation shell on the main menu. Its purpose is to make plugin load success obvious before deeper content or native-core integration is tested in-game.

The repository script layout is now grouped by purpose:

- `scripts/deploy/`
- `scripts/verify/`
- `scripts/release/`
- `scripts/test/`

The old root-level script names are kept as compatibility wrappers, but the recommended daily entrypoint is:

```powershell
./scripts/longlive.ps1 -Action host-redeploy
```

You can then build the host shell with:

```powershell
./scripts/build-host.ps1
```

If you want to build and deploy the host shell into the local `BepInEx/plugins` directory in one step:

```powershell
./scripts/deploy-host.ps1
```

To verify whether the host plugin directory is actually running the same `LongLive*.dll` set as the current local build output, run:

```powershell
./scripts/check-host-deploy.ps1
```

To read the current BepInEx host log and filter it down to `LongLive`-related lines, run:

```powershell
./scripts/read-host-log.ps1
```

If you want to read only the latest detected LongLive startup block instead of an arbitrary tail window, use:

```powershell
./scripts/read-host-log.ps1 -Scope LatestStartup -Tail 300 -Pattern 'LongLive feature state:|LongLive host module MVID:|LongLive observed scene load:'
```

You can also override the tail window or regex pattern, for example:

```powershell
./scripts/read-host-log.ps1 -Tail 300 -Pattern 'LongLive|\[PopTipOptimization\]|\[FadeOptimization\]|\[PinyinSearch\]'
```

To combine deploy-state verification with a runtime-log startup-summary check, run:

```powershell
./scripts/check-host-runtime.ps1
```

This helper currently expects the new host startup log to contain the `LongLive feature state:` summary line.

To perform a guarded host redeploy that aborts early when the current game-root processes or old target DLL state would make replacement unreliable, run:

```powershell
./scripts/redeploy-host.ps1
```

If you want Codex or your local shell to wait for the game-root processes to exit and then automatically start the guarded redeploy flow, run:

```powershell
./scripts/wait-and-redeploy-host.ps1
```

For the recommended in-game validation order after redeploy succeeds, see:

```text
docs/host-runtime-validation-checklist.md
```

For grouped runtime evidence collection after a validation round, use:

```powershell
./scripts/collect-runtime-validation.ps1 -Scope LatestStartup
```

If that helper reports `stale-startup-block-detected`, the current log still only proves an older LongLive startup and the current deploy must be launched once before the collected evidence should be trusted.

If you want to remove the currently deployed Host files before a clean redeploy, use:

```powershell
./scripts/clean-host.ps1
```

If you want to stage a separate local-test `Next` shell that represents the content-side install path players will see, use:

```powershell
./scripts/deploy-next-localtest.ps1
```

If you want to remove staged LongLive local-test groups before re-staging, use:

```powershell
./scripts/clean-next-localtest.ps1 -AllLongLive
```

That helper now stages two different content-side pieces by default:

- the sample `LongLive.Bridge` local-test shell under `Lua/` and `NData/`
- the optional JSON demo payload under `LongLive/json-mod-demo/`

If you want the staged Bridge shell to default to a silent missing-host mode, pass:

```powershell
./scripts/deploy-next-localtest.ps1 -DisableMissingHostReminder
```

On workshop-driven installations like the current local setup, the deploy helper also falls back to the plugin directory adjacent to `BepInEx/core` when the game root does not contain a standalone `BepInEx/plugins` directory.

The host main-menu entry also supports optional custom button sprites under:

```text
src/LongLive.BepInEx/LongLiveAssets/Next/
```

Expected filenames:

- `logo_default.png`
- `logo_press.png`
- `logo_selector.png`

Each image should match Next's current button size of `100x100`.

If all three files are present, the host plugin replaces the cloned button sprites directly through the same main-menu patch timing that Next uses.

That local-test deploy helper creates a valid content-side shell under `本地Mod测试` and is intentionally separate from host deployment.

This distinction matters:

- `LongLive.Host` belongs in `BepInEx/plugins`
- `LongLive.Bridge` or content shells belong in `本地Mod测试/.../plugins/Next/...`

The host now also exposes a minimal read-only handshake surface through `LongLivePluginContext`.

Current handshake usage includes:

- host presence detection
- host version lookup
- capability lookup
- install-root reporting for diagnostics

The host now also exposes the first dedicated scene-routing service through `LongLivePluginContext.SceneRouting`.

Related host-facing framework entry points now also include:

- `LongLivePluginContext.SceneRoutingHost`
- `LongLivePluginContext.MapOverview`
- `LongLivePluginContext.CustomMapRuntime`
- `LongLivePluginContext.CreateMapRegistryPlan(...)`
- `LongLivePluginContext.RegisterMapRegistryDraft(...)`
- `LongLivePluginContext.RegisterSceneRoutingFeature(...)`
- `LongLivePluginContext.RegisterSceneRouteSource(...)`
- `LongLivePluginContext.RegisterMapRegistryPlan(...)`
- `LongLivePluginContext.TryGetSceneRoutingFeature<TFeature>(...)`

Current `SceneRouting` scope includes:

- scene-kind resolution for `AllMaps`, `S...`, `F...`, `Sea...`, and `FRandomBase`
- current routing snapshot capture
- typed player warp requests
- typed NPC warp requests

Current implementation limits include:

- cross-fuben NPC routing is not yet supported unless the target scene is already the active host fuben runtime
- route resolution does not yet consume custom map registry plans directly

The current map-facing feature shells now also expose read-only catalog query helpers so future host extensions can inspect registered pages, regions, nodes, and scenes without reaching into mutable registry internals.

`MapOverview` now also exposes a routing projection surface so world-map nodes can be converted into typed `LongLiveSceneAddress` values without each future module rebuilding that mapping manually.

That routing projection surface now also carries read-only node access metadata such as `AccessStaticValueId`, `HideOnLock`, and `AccessRuleSummary` when a registration source provides them.

`CustomMapRuntime` now also exposes a bootstrap catalog for future runtime activation planning, including entry route, return route, overview ownership, and runtime identity metadata.

`CustomMapRuntime` now also exposes `SceneLocalTopologies`, a separate read-only catalog for scene-local node graphs that should not be confused with world-overview nodes.

This split is important for compatibility work:

- world-overview entry points belong to `MapOverview`
- scene-internal node graphs belong to `CustomMapRuntime.SceneLocalTopologies`

For external C# map modules, the recommended registration path is now:

1. build a `LongLiveMapRegistryDraft`
2. call `LongLivePluginContext.RegisterMapRegistryDraft(...)`
3. let the host create the plan, validate it, allocate host-side IDs, and fan it out across `SceneRouting`, `MapOverview`, and `CustomMapRuntime`

This avoids pushing external modules to hand-roll `LongLiveMapRegistryPlanner` usage unless they explicitly need pre-registration inspection of the resulting `LongLiveMapRegistryPlan`.

This is intended to be the stable detection entry point for a future `LongLive.Bridge` package.

The first Bridge-facing state contract is also published into Next runtime state keys once runtime-backed installers are active.

Examples:

- `longlive.host.present`
- `longlive.host.version`
- `longlive.host.handshake_version`
- `longlive.host.capabilities`
- `longlive.host.install_root`

The sample Bridge shell in `docs/samples/next-bridge-demo/` reads those keys through Next runtime on `EnterGame`.

That sample Bridge shell also ships with a `modConfig` toggle for the missing-host reminder so players can disable the pop-tip without uninstalling the Bridge shell.

The same sample shell now writes a stable compatibility token into Next state:

- `longlive.bridge.last_status`

Current values are:

- `ok`
- `missing`
- `incompatible:handshake`
- `incompatible:capability:<name>`

For debugging, it also writes a verbose detail string into:

- `longlive.bridge.last_status_detail`

The Bridge-side Lua shell now loads its own localized reminder text from `Lua/i18n/` based on the host-published `longlive.current_locale` value.

## Intended Responsibility

This project should eventually own:

- plugin entry
- host lifecycle glue
- host logging and config bridging
- runtime facade bootstrapping
- scene routing and warp ownership
- future patch registration only when necessary

It should not absorb high-level Next wrapper APIs that belong in `LongLive.Next`.
