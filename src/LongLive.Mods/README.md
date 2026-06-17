# LongLive.Mods

This project contains the first JSON-mod schema, model, parsing, and validation skeleton for `LongLive`.

## Current Scope

The current scope is intentionally limited to:

- manifest parsing
- state-key file parsing
- command file parsing
- query file parsing
- locale file discovery and basic validation
- semantic validation

It does not include runtime installation yet.

That statement now has one narrow exception:

- the project includes an installer skeleton for mapping declarative JSON command/query entries to current `LongLive.Next` builtin capabilities

This installer layer is intentionally limited and does not yet cover locale installation, state-key installation, or `next-script` execution.

Content-oriented entries such as items, skills, buffs, and assets now also flow through an installation registry, but the default implementation reports them as deferred until a real runtime injection backend is added.

## Convenience Entry

The project now also exposes:

- `LongLiveModToolkit`
- `LongLiveModLoadReport`

The installation layer now also includes:

- `ILongLiveContentRegistry`
- `LongLiveDeferredContentRegistry`
- `LongLiveContentInstallContext`
- `LongLiveContentInstallRequest<TContent>`
- content install report entries with explicit `Installed`, `Deferred`, and `Skipped` status

These are thin convenience types over the loader and validator.

## Design Rule

JSON mods are treated as declarative contribution packages.

They are not intended to become a second scripting language.

Complex host logic should remain in C# and, where already appropriate, in Next's own Lua layer.
