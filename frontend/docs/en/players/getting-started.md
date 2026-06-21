# Getting Started

If you only want to use the mod, these are the main things to know.

## Using it together with Next is recommended

LongLive Lib lives in the `BepInEx` and `Next` ecosystem.

Running without Next is theoretically possible, but using both together is still the safer choice for compatibility.

## Slightly longer startup is normal

LongLive Lib loads extra runtime content, so startup can take longer than usual.

## Most player-facing switches live in F1 config

Common options include:

- `EnableBulkItemUseOptimization`
- `EnablePopTipOptimization`
- `EnableExperimentalBattleGuard`
- `EnableFadeOptimization`

## Check the main-menu entry if you want to confirm loading

If the `LongLive Lib` entry appears in the main menu, the host side is usually up and running.
