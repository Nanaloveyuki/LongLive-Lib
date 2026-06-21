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

If you can also open the panel and see state information, the basic runtime path is usually working.

## Where to look first when something feels wrong

If your problem is closer to “the feature did not trigger”, “the entry is missing”, or “the game still feels stuck”, start with:

- [Workshop Notes](../workshop/overview.md)
- [Upload Checklist](../workshop/upload-checklist.md)

If you are deploying locally, also check:

- [Deployment Guide](../guide/deploy-guide.md)

## Which switches can be disabled

If you do not like a specific optimization, these are the common toggles in `F1`:

- `EnableBulkItemUseOptimization`
- `EnablePopTipOptimization`
- `EnableExperimentalBattleGuard`
- `EnableFadeOptimization`
