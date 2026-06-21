# LingJie Map Draft Adapter

This document records the first concrete third-party map metadata adapter added to `LongLive`.

## 1. Scope

The current adapter does not try to replace `LingJie`.

It only translates the easiest stable metadata subset into a `LongLiveMapRegistryDraft`:

- scene names
- node display names
- node positions
- node warp targets
- page and region grouping
- access-static-value gates
- lock-visibility hints
- non-executable access-rule summaries

It intentionally does not intercept or replace:

- LingJie's own Harmony patch stack
- overview-map UI cloning behavior
- quick-move override logic
- custom scene runtime controllers

## 2. Current Source Coverage

The current adapter uses metadata derived from:

- `UIMapNingZhouPatch`
- `UIMapSeaPatch`

That means the first covered targets are:

- `天阳城` on the Ningzhou side
- `月池国` on the sea map
- `酆都` on the sea map
- the sea-route `天阳城` entry

## 3. Why This Adapter Exists

This adapter proves the intended compatibility direction.

`LongLive` should not try to patch over a downstream map mod wholesale.

Instead, it should:

1. extract reusable map metadata
2. convert that metadata into `LongLiveMapRegistryDraft`
3. register that draft through the host's standard map registration path

That keeps `LongLive` aligned with host-owned routing and future map registration work.

## 4. Current Limitations

The current adapter is deliberately incomplete.

The newest adapter round also carries a small amount of gate metadata into LongLive's standard node and routing-projection models:

- sea nodes now expose `AccessStaticValueId`
- sea nodes now expose `HideOnLock`
- the Ningzhou `天阳城` node now exposes an `AccessRuleSummary` derived from LingJie's `Show_Postfix()` condition

These values are metadata only.

They do not mean `LongLive` now executes LingJie's access logic on its own.

It does not yet model:

- icon asset mapping
- quick-move special-case rules
- custom scene-local runtime behavior
- runtime node enable or disable state
- executable gate evaluation

Those belong to later layers once the host-owned `MapOverview` and `CustomMapRuntime` models become richer.

## 5. Intended Next Step

The next practical extension should be to generalize this pattern:

- keep one adapter per third-party library family
- extract metadata only when the source semantics are stable enough
- delay any replacement of patch-owned runtime behavior until `LongLive` owns that capability directly
