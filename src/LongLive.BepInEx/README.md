# LongLive.BepInEx

This project is the formal home for BepInEx-facing host code.

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

The host also now includes a first visible in-game validation shell on the main menu. Its purpose is to make plugin load success obvious before deeper content or native-core integration is tested in-game.

You can then build the host shell with:

```powershell
./scripts/build-host.ps1
```

If you want to build and deploy the host shell into the local `BepInEx/plugins` directory in one step:

```powershell
./scripts/deploy-host.ps1
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

If you want to stage the current JSON demo package as a real Next local-test mod under `觅长生/本地Mod测试/`, use:

```powershell
./scripts/deploy-next-localtest.ps1
```

That script creates a valid local group shell at `本地Mod测试/LongLive.LocalTest/plugins/Next/modLongLiveDemo/` and copies the current JSON demo package under `LongLive/json-mod-demo/`.

## Intended Responsibility

This project should eventually own:

- plugin entry
- host lifecycle glue
- host logging and config bridging
- runtime facade bootstrapping
- future patch registration only when necessary

It should not absorb high-level Next wrapper APIs that belong in `LongLive.Next`.
