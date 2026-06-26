# LongLive-Lib Notes

This file is a short repo-local note for future work.

## What This Repo Is

- `src/LongLive.BepInEx/`
  - host-side plugin code loaded by `BepInEx/plugins`
  - this is where runtime patches, config, diagnostics, UI, compatibility, and scene-routing host logic live
- `src/LongLive.Mods/`
  - shared contracts and registries
  - should stay relatively clean and reusable instead of depending on Unity host details
- `src/LongLive.Next.Runtime/`
  - runtime bridge for talking to Next by reflection
- `frontend/`
  - public docs site
- `docs/`
  - internal project docs, reports, design notes, deploy notes
- `scripts/`
  - build, deploy, verify, release, and test helpers

## Deployment

The current local host references come from:

- `eng/LocalReferences.props`
  - `McsGameRoot = D:\Appdata\Steam\steamapps\common\觅长生`
  - `BepInExCoreDir = D:\Appdata\Steam\steamapps\workshop\content\1189490\2824349934\BepInEx\core`

Current deploy targets:

- Host plugin:
  - `D:\Appdata\Steam\steamapps\workshop\content\1189490\2824349934\BepInEx\plugins`
- Local-test Next shell:
  - `D:\Appdata\Steam\steamapps\common\觅长生\本地Mod测试\LongLive.LocalTest\plugins\Next\modLongLiveBridge`

Recommended commands:

```powershell
./scripts/longlive.ps1 -Action host-redeploy -SkipRuntimeCheck
./scripts/longlive.ps1 -Action host-runtime-check
./scripts/longlive.ps1 -Action localtest-stage
./scripts/longlive.ps1 -Action release-stage -Version 0.2.2
```

Use `host-redeploy -SkipRuntimeCheck` when the game has not yet been relaunched after deploy.

## Script Groups

- `scripts/deploy/`
  - real deploy and cleanup logic
- `scripts/verify/`
  - deploy verification and runtime log checks
- `scripts/release/`
  - workshop staging
- `scripts/test/`
  - offline regression scripts for specific features
- `scripts/longlive.ps1`
  - unified entrypoint, use this first

## Current Practical Rules

- Keep real config keys stable in English for compatibility.
- Player-facing text can be localized through `src/LongLive.BepInEx/Localization/*.json`.
- Host-side changes usually need redeploy to `BepInEx/plugins`.
- Content-shell changes usually need `localtest-stage`.
- A deploy-file match is not enough to prove the game loaded the new build; use runtime check after launching and exiting the game.

## Current F1 Config i18n Shape

- Config keys remain stable English keys such as `EnableFadeOptimization`.
- Player-facing config category/name/description is now routed through:
  - `src/LongLive.BepInEx/Plugin/Configuration/LongLiveLocalizedConfigBinder.cs`
  - `src/LongLive.BepInEx/Plugin/Configuration/LongLiveConfigurationManagerTagFactory.cs`
- If a ConfigurationManager-style panel is present, it can pick up localized display names and categories.

## When Resuming Work

- Read `scripts/Agents.md` for script-specific details.
- Read `docs/deploy-guide.md` for daily deploy flow.
- For gameplay regressions, check `docs/report/` first.
- For scene-routing and custom-map work, check `docs/design/` and the current runtime classes under `src/LongLive.BepInEx/Plugin/SceneRouting/`.
