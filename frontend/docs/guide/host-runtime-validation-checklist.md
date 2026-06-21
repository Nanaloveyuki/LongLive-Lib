# Host Runtime Validation Checklist

This checklist is the current recommended runtime-validation flow for `LongLive.Host`.

It is intentionally focused on the player-facing optimization tracks that are still awaiting real in-game verification:

- bulk consumable batching
- pop-tip aggregation and cleanup
- TuJian pinyin search
- fade / black-screen acceleration

## 1. Preflight

Before starting the game:

1. ensure all game-root processes are fully closed
2. run:

```powershell
./scripts/check-host-deploy.ps1
```

Expected result:

- required `LongLive*.dll` files match the current local build output

Then run:

```powershell
./scripts/redeploy-host.ps1
```

Expected result:

- the guarded redeploy completes successfully

Then run:

```powershell
./scripts/check-host-runtime.ps1
```

Expected result:

- deploy verification passes
- runtime log contains the new `LongLive feature state:` startup summary line

If any of the above steps fail, do not trust later in-game results yet.

## 2. Startup Confirmation

After launching the game, confirm these points first:

1. the `LongLive` main-menu entry is present
2. the diagnostics panel still opens correctly
3. the current BepInEx log includes:
   - `LongLive feature state:`
   - `LongLive host bootstrap completed.`

Useful command:

```powershell
./scripts/read-host-log.ps1 -Scope LatestStartup -Tail 300 -Pattern 'LongLive feature state:|LongLive host module MVID:|LongLive host bootstrap completed.|LongLive observed scene load:'
```

For a grouped snapshot centered on the latest LongLive startup block:

```powershell
./scripts/collect-runtime-validation.ps1 -Scope LatestStartup -Mode Startup
```

## 3. Bulk Item Use

Goal:

- verify that high-volume consumable use no longer causes severe freezing
- verify that summary prompts feel natural across more than one item family

Suggested test groups:

1. cultivation-gain items
2. HP or HP-max consumables
3. aptitude / comprehension / mindset consumables
4. at least one mod-added consumable family if available

Expected behavior:

- long-press right click still opens the quantity selector
- middle click also opens the quantity selector
- large batches are processed smoothly across frames
- summary prompts are aggregated rather than fully spammed
- when only a generic fallback is possible, the batch still completes cleanly

Watch for these regressions:

- right-click use path being swallowed when selector opening fails
- duplicated HP-heal summaries when HP only rose because HP max rose
- missing or awkward wording for common stat-delta summaries

Useful log filters:

```powershell
./scripts/read-host-log.ps1 -Tail 400 -Pattern 'LongLive feature state:|\[BulkItemUse\]'
```

Grouped evidence collection:

```powershell
./scripts/collect-runtime-validation.ps1 -Scope LatestStartup -Mode Bulk
```

If the grouped collector prints `stale-startup-block-detected`, the current log still only proves an older LongLive startup. In that case, relaunch the game once with the current deploy before evaluating the feature logs.

## 4. Pop-Tip Optimization

Goal:

- verify that repeated prompts no longer fill the screen for too long
- verify that non-bulk prompt merging is still natural

Suggested scenarios:

1. rapid repeated ordinary prompts
2. repeated numeric-suffix prompts with the same prefix
3. leaving a save or scene while prompts are still active

Expected behavior:

- repeated identical messages can collapse into `xN`
- repeated numeric-suffix messages can collapse into summed-value form
- prompt lifetime shortens under queue pressure
- prompts do not linger badly after scene changes

Watch for these regressions:

- false merges between messages that should remain distinct
- fast-mode timing leaking into later ordinary prompts
- prompt loss during bulk-item aggregation sessions that should have been summarized elsewhere

Useful log filters:

```powershell
./scripts/read-host-log.ps1 -Tail 400 -Pattern 'LongLive feature state:|\[PopTipOptimization\]|\[BulkItemUse\]'
```

Grouped evidence collection:

```powershell
./scripts/collect-runtime-validation.ps1 -Scope LatestStartup -Mode PopTips
```

## 5. TuJian Pinyin Search

Goal:

- verify that TuJian search now accepts pinyin and initials without breaking original substring search

Suggested cases:

1. original Chinese substring queries
2. full pinyin queries
3. initials-only queries
4. mixed-content queries where a panel checks both title and description fields

Expected behavior:

- original non-pinyin search still works
- pinyin and initials work on the same entries when dictionary coverage exists
- if the pinyin service fails internally, the original search path still works rather than breaking the panel

Watch for these regressions:

- false positives from overly broad pinyin keys
- mismatches on common multi-character phrases
- behavior differences after toggling `EnableTuJianPinyinSearch`

Useful log filters:

```powershell
./scripts/read-host-log.ps1 -Tail 300 -Pattern 'LongLive feature state:|\[PinyinSearch\]'
```

Grouped evidence collection:

```powershell
./scripts/collect-runtime-validation.ps1 -Scope LatestStartup -Mode Pinyin
```

## 6. Fade Optimization

Goal:

- verify that targeted black-screen transitions feel faster without obvious state leakage or broken sequencing

Suggested scenarios:

1. title to world-map transition
2. world to island transition
3. island or settlement return path
4. battle-result or settlement-style black overlay flow

Expected behavior:

- supported fades are visibly shorter
- door or scene sequencing still completes correctly
- later unrelated animator states do not stay sped up accidentally

Watch for these regressions:

- reused animators remaining too fast after a fade path ends
- a specific transition becoming too short to remain readable
- a targeted callsite still feeling unchanged because it is routed through a different path than expected

Useful log filters:

```powershell
./scripts/read-host-log.ps1 -Tail 300 -Pattern 'LongLive feature state:|\[FadeOptimization\]'
```

Grouped evidence collection:

```powershell
./scripts/collect-runtime-validation.ps1 -Scope LatestStartup -Mode Fade
```

## 7. Result Recording

When a validation round finishes, capture:

1. which runtime track was tested
2. whether the deploy and runtime checks passed first
3. the exact user-visible result
4. the relevant filtered log excerpt
5. whether the issue is:
   - fixed
   - improved but still awkward
   - unchanged
   - regressed

This makes it much easier to update `TEMP_GOAL_HANDOFF.md` and keep the next goal turn grounded in current evidence.

If one validation round touches multiple tracks, a single grouped snapshot is also useful:

```powershell
./scripts/collect-runtime-validation.ps1 -Scope LatestStartup
```
