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

The optional JSON-mod demo bootstrap is also explicit opt-in:

- enable `EnableJsonModDemoInstall`
- set `JsonModDemoPath` to a real package directory
- optionally set `ContentBackend` to `Deferred` or `Next`

The host project does not assume a repository-local sample path at runtime.

The current `Next` content backend option is only a host-side shell. It preserves the future runtime injection composition point while still reporting deferred content installation today.

You can then build the host shell with:

```powershell
./scripts/build-host.ps1
```

If you want to build and deploy the host shell into the local `BepInEx/plugins` directory in one step:

```powershell
./scripts/deploy-host.ps1
```

## Intended Responsibility

This project should eventually own:

- plugin entry
- host lifecycle glue
- host logging and config bridging
- runtime facade bootstrapping
- future patch registration only when necessary

It should not absorb high-level Next wrapper APIs that belong in `LongLive.Next`.
