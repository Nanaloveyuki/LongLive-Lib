# LongLive Workshop Upload Checklist

Use this checklist for the actual in-game workshop upload flow.

## 1. Prepare The Package

Run:

```powershell
./scripts/stage-workshop-release.ps1 -Configuration Debug -Version 0.2.0
```

Current staged upload root:

```text
artifacts/workshop/LongLive.Lib.0.2.0/
```

This is the folder to select in the uploader.

Do not select the inner `plugins/` folder.

## 2. Confirm Folder Shape

Before uploading, confirm the selected folder directly contains:

- `plugins/LongLive.BepInEx.dll`
- `plugins/LongLive.Mods.dll`
- `plugins/LongLive.Next.Abstractions.dll`
- `plugins/LongLive.Next.Runtime.dll`
- `plugins/Next/modLongLiveBridge/...`

Optional but currently recommended:

- `plugins/LongLiveAssets/Next/logo_default.png`
- `plugins/LongLiveAssets/Next/logo_press.png`
- `plugins/LongLiveAssets/Next/logo_selector.png`

It should not contain:

- `*.pdb`
- `*.xml`
- unrelated mod DLLs
- repository junk files

## 3. In-Game Upload Steps

1. Run the game in windowed mode.
2. Open the workshop uploader.
3. Select the staged package root folder.
4. Select a `512x512` cover image if possible.
5. Set dependency to `Next`.
6. Fill in title, description, visibility, and mod type.
7. Upload.

## 4. Suggested Metadata

Title:

- `LongLive Lib`

Short description direction:

- Chinese-first or bilingual wording is recommended
- installs the LongLive host and bridge together
- adds the LongLive diagnostics entry
- includes host and bridge status reporting
- includes bulk item-use optimization
- native core is not required for `0.2.0`

Suggested description text:

- `LongLive Lib 主程序与兼容桥接。提供诊断入口、批量使用物品优化，以及 Host / Bridge 状态提示。LongLive host and compatibility bridge with diagnostics, bulk item-use optimization, and Host / Bridge status reporting.`

## 5. After Upload

Keep the generated `Mod.bin` file in the same staged package root.

That file is required for future updates through the in-game uploader.

The staging script preserves `Mod.bin` if it already exists in that folder.

## 6. Local Test Cleanup

Before validating the workshop version, remove any conflicting local-test copy from:

```text
觅长生/本地Mod测试/
```

Do not leave an older local-test `LongLive` copy enabled while checking the uploaded package.
