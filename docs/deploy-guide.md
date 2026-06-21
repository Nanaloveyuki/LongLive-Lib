# LongLive Deployment Guide

This guide is the short operator-facing entrypoint for local deployment.

## Script Layout

The repository now groups scripts by purpose:

- `scripts/deploy/`
  - host deployment and local-test staging
- `scripts/verify/`
  - runtime and log verification
- `scripts/release/`
  - workshop upload staging
- `scripts/test/`
  - offline regression scripts for development

For convenience, the root `scripts/` folder still keeps compatibility wrappers with the old names.

## Recommended Daily Commands

Use the unified entrypoint whenever possible:

```powershell
./scripts/longlive.ps1 -Action host-redeploy
```

If the game might still be running, use:

```powershell
./scripts/longlive.ps1 -Action host-redeploy -Wait
```

After launching and exiting the game, verify that the current build actually ran:

```powershell
./scripts/longlive.ps1 -Action host-runtime-check
```

To stage the local-test content shell under `本地Mod测试`:

```powershell
./scripts/longlive.ps1 -Action localtest-stage
```

To stage a workshop upload folder:

```powershell
./scripts/longlive.ps1 -Action release-stage -Version 0.2.1
```

The staged upload root stays fixed at `artifacts/workshop/LongLive.Lib/` so the game-generated `Mod.bin` can keep tracking the same workshop item across updates.

## Direct Category Entry Points

If you prefer direct scripts, the categorized paths are:

- `scripts/deploy/redeploy-host.ps1`
- `scripts/verify/check-host-runtime.ps1`
- `scripts/deploy/deploy-next-localtest.ps1`
- `scripts/release/stage-workshop-release.ps1`

## What You Can Ignore Most Of The Time

You usually do not need to run anything under `scripts/test/` manually unless you are working on the code itself.

Those scripts exist for offline regression checks during development, not for normal player-facing deployment.
