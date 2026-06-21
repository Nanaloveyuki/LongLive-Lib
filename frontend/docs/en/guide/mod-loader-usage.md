# Mod Loader Usage

This document shows how to use the current `LongLive.Mods` parsing and validation skeleton.

## 1. Current Scope

The current toolkit only handles:

- loading a JSON-mod package from a directory
- parsing the declared files
- validating structure and semantics

The repository also includes a first installation skeleton for `builtin` command and query entries.

## 2. Sample Package

A reference sample package is included under:

- `docs/samples/json-mod-demo/`

This sample is intentionally small and matches the current schema draft.

## 3. Basic Usage

```csharp
using LongLive.Mods;

var toolkit = new LongLiveModToolkit();
var report = toolkit.LoadAndValidate(@"F:\repo\LongLive-Lib\docs\samples\json-mod-demo");

var package = report.Package;
var validation = report.Validation;

if (!validation.IsValid)
{
    foreach (var issue in validation.Issues)
    {
        Console.WriteLine($"[{issue.Severity}] {issue.Code}: {issue.Message}");
    }
}
```

## 4. What You Get Back

`LongLiveModLoadReport` currently contains:

- `Package`
- `Validation`

The loaded package includes:

- manifest
- optional state-key file
- optional command file
- optional query file
- optional item file
- optional skill file
- optional buff file
- optional asset-mapping file
- locale resources loaded as text

## 5. Validation Behavior

The current validator checks things such as:

- required manifest fields
- duplicate ids
- unsupported backends
- unsupported state-key types
- invalid default-value types
- invalid locale JSON

## 6. Current Limitation

The toolkit does not yet:

- install commands into `LongLive.Next`
- install queries into `LongLive.Next`
- register locales into runtime localization
- map JSON declarations to executable host capabilities

That installation layer should come after the schema and validation behavior are stable enough.

## 7. Installer Skeleton

The repository now also includes a first installer skeleton in `LongLive.Mods.Installation`.

Current behavior:

- installs `builtin` command entries into `INextCommandRegistry`
- installs `builtin` query entries into `INextQueryRegistry`
- routes items, skills, buffs, and assets through `ILongLiveContentRegistry`
- uses a deferred content registry by default until a real runtime injection backend exists
- skips unsupported backends such as `next-script` for now

Example shape:

```csharp
using LongLive.Mods;
using LongLive.Mods.Installation;
using LongLive.Next.Runtime;

var toolkit = new LongLiveModToolkit();
var report = toolkit.LoadAndValidate(@"F:\repo\LongLive-Lib\docs\samples\json-mod-demo");

if (report.Validation.IsValid)
{
    var runtime = NextRuntimeFactory.Create();
    var installer = new LongLiveModInstaller(
        runtime.CommandRegistry,
        runtime.QueryRegistry,
        runtime.StateStore);
    var installReport = installer.Install(report.Package);
}
```

This is still a bootstrap installation layer, not the final full mod runtime.

The current install report therefore contains two different result shapes:

- command and query registrations that are actually installed now
- content entries that currently report `Deferred` unless a custom content registry is supplied

## 8. Host Demo Notes

`LongLive.BepInEx` can optionally run this loader and installer chain during plugin bootstrap.

That demo flow is intentionally explicit opt-in:

- `EnableJsonModDemoInstall` must be enabled
- `JsonModDemoPath` must point to a real package directory

If the path is empty, missing, or fails to parse, the host logs the problem and skips the demo install.
