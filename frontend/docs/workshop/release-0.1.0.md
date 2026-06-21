# LongLive 0.1.0 Workshop Release Plan

This document records the practical release shape for `LongLive 0.1.0` under the game's current workshop uploader constraints.

## 1. Important Constraint

The in-game uploader accepts one mod root folder.

That selected folder must directly contain a `plugins/` directory.

For `Next` content, the layout under that `plugins/` directory must follow:

- `plugins/Next/mod.../`

For ordinary managed mods, the layout is:

- `plugins/*.dll`

The uploader is therefore friendly to one self-contained content package, but it is not a good fit for multi-root installation instructions such as:

- install one part into `BepInEx/plugins`
- install another part into a separate workshop or local-test path

## 2. Recommended 0.1.0 Shape

For `LongLive 0.1.0`, the most practical release shape is a single workshop package that contains:

- the `LongLive.Host` managed plugin DLLs under `plugins/`
- the `LongLiveAssets/` folder under `plugins/` when the custom menu button art is desired
- the sample `LongLive.Bridge` shell under `plugins/Next/modLongLiveBridge/`
- the generated `Config/modConfig.json` under that same `modLongLiveBridge` folder

This means the workshop package is intentionally a hybrid delivery shape.

It is not the ideal architecture for long-term separation, but it matches the actual uploader rules and gives players a one-click install path for `0.1.0`.

## 3. Files To Publish

The current `0.1.0` workshop package should include these managed files under the top-level `plugins/` directory:

- `LongLive.BepInEx.dll`
- `LongLive.Mods.dll`
- `LongLive.Next.Abstractions.dll`
- `LongLive.Next.Runtime.dll`
- `Microsoft.Bcl.AsyncInterfaces.dll`
- `System.Buffers.dll`
- `System.Memory.dll`
- `System.Numerics.Vectors.dll`
- `System.Runtime.CompilerServices.Unsafe.dll`
- `System.Text.Encodings.Web.dll`
- `System.Text.Json.dll`
- `System.Threading.Tasks.Extensions.dll`
- `System.ValueTuple.dll`

Do not include:

- `*.pdb`
- `*.xml`
- other mods' DLLs
- local development helper files

## 4. Assets To Publish

If you want the custom LongLive main-menu entry art, include:

- `plugins/LongLiveAssets/Next/logo_default.png`
- `plugins/LongLiveAssets/Next/logo_press.png`
- `plugins/LongLiveAssets/Next/logo_selector.png`

These are part of the current visible host UX and should stay in the `0.1.0` package.

## 5. Bridge Files To Publish

The package should also include the Bridge shell under:

- `plugins/Next/modLongLiveBridge/Lua/...`
- `plugins/Next/modLongLiveBridge/NData/...`
- `plugins/Next/modLongLiveBridge/Config/modConfig.json`

This keeps the compatibility reminder and Bridge state reporting available immediately after install.

## 6. Native DLL Decision For 0.1.0

Do not include `longlive_native_core.dll` in the default `0.1.0` workshop package.

Reasoning:

- the current native path is still optional
- the host already falls back safely when native adjudication is unavailable
- a smaller first workshop release is easier to validate
- this avoids introducing an extra native binary into the first public packaging pass unless it is required by the release feature set

If a later release makes native adjudication a real player-facing feature, revisit this decision and stage a separate package test.

Current native role in the codebase:

- optional battle-segment adjudication middleware
- optional native probe and ABI check
- optional acceleration path for future heavier combat rules

It is not currently required for the released host feature set.

The main gameplay-facing optimizations already active in `0.1.0`, such as bulk item-use smoothing and the current battle-guard path, still remain functional without the native DLL.

## 7. Folder To Select In The Uploader

When uploading, select the package root folder itself.

That folder must be the one that directly contains `plugins/`.

For the repository staging script, that means selecting a stable folder shaped like:

- `LongLive.Lib/`

And inside it:

- `plugins/LongLive.BepInEx.dll`
- `plugins/Next/modLongLiveBridge/...`

Do not select the inner `plugins/` folder itself.

## 8. Staging Script

The repository now includes:

- `scripts/stage-workshop-release.ps1`

Default usage:

```powershell
./scripts/stage-workshop-release.ps1 -Configuration Debug -Version 0.1.0
```

This stages a release folder under:

```text
artifacts/workshop/LongLive.Lib/
```

That staged folder is the one to select in the in-game uploader.

## 9. Suggested 0.1.0 Workshop Metadata

Suggested title:

- `LongLive Lib`

Suggested type:

- tool or framework-oriented type, whichever best matches the uploader's available categories

Suggested dependency:

- `Next`

Suggested description points:

- Chinese-first or bilingual wording is recommended for this workshop community
- installs the LongLive host plugin and compatibility bridge together
- adds the LongLive diagnostics entry on the main menu
- includes Host and Bridge state reporting
- includes bulk item-use optimization
- includes battle guard groundwork and map diagnostics as internal host capabilities
- native core is not required for `0.1.0`

Suggested short bilingual description:

- `LongLive Lib 主程序与兼容桥接。提供诊断入口、批量使用物品优化，以及 Host / Bridge 状态提示。LongLive host and compatibility bridge with diagnostics, bulk item-use optimization, and Host / Bridge status reporting.`

## 10. After Upload

Keep the generated `Mod.bin` file inside the uploaded package root.

That file is required for future in-place workshop updates through the in-game uploader.

The staging script preserves an existing `Mod.bin` when rebuilding the same staged package directory, so the upload/update flow can continue using the same folder.

If the staging path is migrated from an older versioned folder such as `LongLive.Lib.0.1.0/`, the script should recover the most recent legacy `Mod.bin` into the fixed `LongLive.Lib/` path before rebuilding.
