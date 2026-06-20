# Scripts Agent Guide

This file is the local operator note for future agents working inside `F:\repo\LongLive-Lib\scripts`.
Read it before changing deployment or verification scripts.

## Scope

The `scripts/` tree exists to support four recurring workflows:

- build and deploy the BepInEx host layer
- stage the separate local-test content shell
- validate that the deployed host is the one the game actually loaded
- run offline regression checks for runtime helpers without booting the game

This directory intentionally keeps both categorized scripts and root-level compatibility wrappers.

## Layout

- `scripts/longlive.ps1`
  - unified entrypoint for normal daily use
- `scripts/deploy/`
  - real deployment and cleanup implementations
- `scripts/verify/`
  - real runtime, deploy, and log verification implementations
- `scripts/release/`
  - workshop staging flow
- `scripts/test/`
  - offline regression scripts for helper logic and transpiler behavior
- `scripts/*.ps1`
  - compatibility wrappers that forward to the categorized scripts or to `longlive.ps1`

When editing script logic, prefer updating the categorized script first.
Keep the root wrapper behavior stable unless there is a good reason to break compatibility.

## Recommended Entry Points

For normal host work, prefer the unified entrypoint:

```powershell
./scripts/longlive.ps1 -Action host-redeploy
```

Useful actions:

- `host-redeploy`
  - build and deploy `LongLive.BepInEx` into the active BepInEx plugin directory
- `host-runtime-check`
  - verify deploy state plus runtime log identity markers
- `localtest-stage`
  - stage the local-test content shell under the game's `本地Mod测试` directory
- `release-stage`
  - stage a workshop upload folder under `artifacts/workshop`

If the game may still be running, use:

```powershell
./scripts/longlive.ps1 -Action host-redeploy -Wait
```

## Direct Script Responsibilities

### Deploy

- `deploy/build-host.ps1`
  - builds `src/LongLive.BepInEx/LongLive.BepInEx.csproj`
- `deploy/deploy-host.ps1`
  - copies required runtime DLLs and host assets into the resolved BepInEx plugins directory
- `deploy/redeploy-host.ps1`
  - safe redeploy chain: process guard, build, copy, deploy check, optional runtime check
- `deploy/wait-and-redeploy-host.ps1`
  - waits for game-root processes to exit before redeploying
- `deploy/deploy-next-localtest.ps1`
  - stages the local-test shell used by the game's mod test folder
- `deploy/clean-host.ps1`
  - removes deployed host files from the target plugins directory
- `deploy/clean-next-localtest.ps1`
  - removes staged local-test shell files

### Verify

- `verify/check-host-deploy.ps1`
  - compares current build outputs against the deployed host files
- `verify/check-host-runtime.ps1`
  - checks deploy state, reads the latest startup block, and verifies runtime identity markers such as feature state and MVID
- `verify/read-host-log.ps1`
  - focused log reader with `Tail`, `LatestStartup`, and `Auto` modes
- `verify/collect-runtime-validation.ps1`
  - broader summary collector for startup and player-facing runtime features

### Test

Representative offline regression scripts:

- `test/test-fade-transpiler.ps1`
- `test/test-pop-tip-aggregation.ps1`
- `test/test-pinyin-search.ps1`
- `test/test-numeric-message-parser.ps1`
- `test/test-bulk-summary-count.ps1`

These are development checks, not normal player deployment steps.

## Environment Assumptions

Scripts resolve local runtime paths from:

- `eng/LocalReferences.props`

Important properties:

- `LongLiveEnableLocalHostReferences`
- `McsGameRoot`
- `BepInExCoreDir`

Current host deployment targets are expected to resolve from those values.
Do not hardcode a different machine-specific path into new script logic unless there is no better option.

## Verification Order

For host-side changes, the normal verification order is:

1. `dotnet build .\src\LongLive.BepInEx\LongLive.BepInEx.csproj -v minimal`
2. run targeted offline test scripts if the change touched helper logic
3. `./scripts/longlive.ps1 -Action host-redeploy -SkipRuntimeCheck` when the game has not yet been relaunched
4. ask for a fresh game launch and exit
5. `./scripts/longlive.ps1 -Action host-runtime-check`
6. use `scripts/verify/read-host-log.ps1` or `collect-runtime-validation.ps1` for deeper diagnosis

Do not treat a deploy-file match as proof that the game loaded the new build.
Runtime log identity markers are the authoritative proof.

## Known Current Runtime Trap

There is a recent goal-track issue where deployment can succeed but the latest available BepInEx log may still only show an older `LongLive Lib plugin awake.` startup block.

Implications:

- always check the log timestamp before trusting a runtime conclusion
- confirm the log path is the active one for the workshop-installed BepInEx layout
- if only an older startup block exists, that is not proof that the new build failed; it may simply mean the game has not been launched since redeploy
- if a fresh launch still stops after `plugin awake`, inspect startup exceptions and Harmony patch registration paths before continuing broader feature work

## Editing Rules For Future Agents

- keep script output concise and operator-focused
- preserve root wrapper compatibility where practical
- prefer one authoritative implementation in the categorized script tree instead of duplicating logic between wrapper and real script
- avoid introducing destructive cleanup defaults
- avoid silently changing deployment targets
- when adding a new verification step, make sure it reflects real evidence rather than inferred success

## Fast Lookup

- deploy current host: `./scripts/longlive.ps1 -Action host-redeploy`
- deploy and wait for exit lock release: `./scripts/longlive.ps1 -Action host-redeploy -Wait`
- verify deployed files: `./scripts/verify/check-host-deploy.ps1`
- verify runtime identity: `./scripts/verify/check-host-runtime.ps1`
- inspect latest startup block: `./scripts/verify/read-host-log.ps1 -Scope LatestStartup`
- stage workshop package: `./scripts/longlive.ps1 -Action release-stage -Version 0.2.0`
