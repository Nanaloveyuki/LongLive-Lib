# Item-Use Optimization

This document records the current `LongLive.BepInEx` bulk item-use behavior.

## 1. Scope

The current optimization targets one concrete gameplay problem:

- consuming a very large number of items in quick succession can freeze or stall the game
- repeated EXP pop tips such as `你的修为提升了N` can flood the right-side UI

The current implementation is intentionally focused on item-use smoothing and pop-tip aggregation.

It is not a generic inventory rewrite.

## 2. Current Behavior

When `EnableBulkItemUseOptimization` is enabled, LongLive currently provides:

- right-click long-press quantity selection for bulk item use
- frame-sliced item consumption after quantity confirmation
- EXP pop-tip aggregation into one final summary line
- pop-tip queue cleanup on scene changes and plugin shutdown

The current flow is implemented inside:

- `src/LongLive.BepInEx/Plugin/ItemUse/LongLiveBulkItemUseRuntime.cs`
- `src/LongLive.BepInEx/Plugin/ItemUse/LongLiveBulkItemUsePatches.cs`
- `src/LongLive.BepInEx/Plugin/ItemUse/LongLiveBulkItemUseInstaller.cs`

## 3. Ownership Model

LongLive now owns its own long-press bulk-use handling.

That means:

- LongLive patches `SlotBase.OnPointerDown`
- LongLive patches `SlotBase.OnPointerUp`
- LongLive runs its own long-press state machine in a small host `MonoBehaviour`
- LongLive opens `USelectNum` directly when the hold threshold is reached

This is no longer treated as a thin extension over EasyBatch's internal runtime state.

## 4. Compatibility Boundary

LongLive currently treats `EasyBatch` as incompatible for the long-press batch-use path.

The practical rule is:

- LongLive disables `EasyBatch.Plugin.Update`
- LongLive provides its own long-press quantity-selection flow instead

This avoids fragile dependence on EasyBatch's private state machine and reduces runtime conflicts.

If the project is released publicly, this conflict should be documented explicitly.

## 5. Runtime Controls

The current host options for this feature are:

- `EnableBulkItemUseOptimization`
- `BulkItemUseChunkSize`
- `BulkItemUseFrameBudgetMs`

Current purpose:

- `EnableBulkItemUseOptimization`
  enables or disables the entire bulk-use optimization layer
- `BulkItemUseChunkSize`
  limits the maximum number of `Use()` calls attempted in one processing pass
- `BulkItemUseFrameBudgetMs`
  limits approximate per-frame processing time for the bulk-use worker

## 6. Current Non-Goals

The current implementation does not yet attempt to:

- rewrite the underlying item-effect logic
- batch-compute all item results in one synthetic domain operation
- coalesce all game-side `MSG_PLAYER_USE_ITEM` observers into one synthetic event
- optimize every other inventory-side UI callback in the game

The present goal is practical stability first.

## 7. Future Improvement Directions

If more optimization is required later, the next good candidates are:

- reduce inventory UI refresh frequency further during large bulk-use runs
- inspect the heaviest `MSG_PLAYER_USE_ITEM` listeners and add targeted throttling where safe
- introduce a purpose-built item-use middleware layer before game-side side effects are committed
- optionally move pure prediction logic into the native core when there is a clear data contract
