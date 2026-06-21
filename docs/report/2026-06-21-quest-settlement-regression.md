# Quest Settlement Regression Report

## Summary

This report records the investigation of a regression affecting the NPC commission flow for the `协助杀妖` line.

The final conclusion is:

- the kill-progress side of the commission could already be repaired from the LongLive battle layer
- the visible failure was not caused by the commission condition itself
- the real regression came from the fade optimization path that redirected the normal `YSNewFight -> AllMaps` return flow through the async `NextScene` bridge
- restoring the original map return flow for battle exit fixed the commission settlement issue

The release consequence for `0.2.1` is:

- keep fade optimization in general
- keep battle kill-progress compensation
- stop redirecting the normal battle return flow from `YSNewFight` back to `AllMaps`

## User-Visible Symptom

The symptom was stable across repeated tests:

- the player accepted an NPC commission of type `协助杀妖`
- the battle itself completed normally
- after the battle, the commission did not visibly complete
- the NPC still kept the commission marker
- reward feedback did not appear in the expected way

At first glance this looked like a commission-condition failure, but repeated tracing showed that the deeper problem was the settlement chain after battle return.

## Initial Hypotheses

Several hypotheses were tested during the investigation.

### Hypothesis 1: Kill progress was not recorded

This was the first and most direct suspicion.

The relevant vanilla code paths showed that random commission kill progress depends on:

- `GlobalValue 401`
- `GlobalValue 402`
- `NomelTaskMag.AutoNTaskSetKillAvatar(...)`
- `NomelTaskFlag[taskId].killAvatar`

The investigation confirmed the active commission and target values:

- `taskId=2024`
- `taskTypeId=12024`
- task name `协助杀妖-炼气`
- current step type `2`
- target value `5600`

This led to a LongLive-side compensation path in `Avatar.die.prefix` that:

- inspected the active random commission state
- checked whether the current battle target matched the active commission target
- called `NomelTaskMag.AutoNTaskSetKillAvatar(...)`
- force-wrote `killAvatar` if vanilla still failed to record it

This part worked.

Runtime logs confirmed:

- `hasProgressAfterSync=True`
- `nowChildIndex=-1`
- `canFinishTask=True`

That ruled out the first hypothesis as the final root cause.

### Hypothesis 2: The commission could finish internally but the NPC marker was left behind

After kill progress started working, the next suspicion moved to visible state.

The investigation focused on:

- `UINPCData.IsNeedHelp`
- `UINPCData.IsTask`
- `UINPCSVItem.CheckTask()`
- `ThreeSenceJsonData.TaskIconValue / TaskIconValueX`
- `CmdCloseNPCTask`

This looked plausible because vanilla has two different marker paths:

- normal NPC task state through `IsNeedHelp` and `IsTask`
- three-scene task icon state through `TaskIconValue` and `GlobalValue`

Several probe and cleanup paths were added to test this theory.

This led to some accurate observations, but not to the final fix.

### Hypothesis 3: LongLive should auto-complete the commission once kill progress is known to be complete

Because the commission state already reached `canFinishTask=True`, LongLive tried an experimental auto-complete route.

That route attempted to:

- call `EndNTask(taskId)` directly
- clear task tracking
- clear NPC help state
- notify completion and reward
- clear possible three-scene marker state on scene load

This experiment produced useful data, but it did not solve the player-visible issue.

In fact, it became clear that this was too invasive.

The commission data layer could be forced into a completed state, but the final player-visible result still remained wrong or incomplete.

This was the point where the investigation had to move away from commission data alone.

## What the Logs Proved

The most important turning point was the evidence collected from runtime logs.

Two facts mattered most.

### Fact 1: The commission condition itself was already satisfied

The LongLive battle probes showed:

- the current active commission matched the battle target
- kill progress was written successfully
- the active step was resolved
- `canFinishTask=True`

This meant the original symptom was no longer about failing to kill the correct monster.

### Fact 2: The visible failure happened together with the battle return redirection

In the same failing runs, the logs also showed:

- `redirected map-scene load to async NextScene bridge: current=YSNewFight, target=AllMaps`

This line belonged to `LongLiveFadeOptimizationRuntime.TryHandleAsyncMapSceneLoad(...)`.

That redirection was originally introduced to speed up some map-return black-screen transitions, but it also changed the original control flow of `Tools.loadMapScenes(...)` during battle settlement.

This was the first strong sign that the regression might not be a commission bug at all, but a battle return pipeline bug.

## Why the Earlier Guesses Were Reasonable But Incomplete

The failed guesses were still useful because they narrowed the problem space.

### Why the marker hypothesis was reasonable

The commission marker staying on the NPC strongly suggested a UI-state mismatch.

Vanilla really does have multiple ways to represent quest availability:

- direct NPC help-state
- task icon state in three-scene data
- task or email bridge state

So investigating marker sources was necessary.

What made that hypothesis incomplete was that the marker problem was a consequence, not the deepest cause.

### Why the auto-complete experiment was reasonable

Once `canFinishTask=True` had been proven, it was reasonable to ask whether LongLive should simply complete the commission itself.

That experiment was valuable because it proved something important:

- forcing the commission to end at the data layer was not equivalent to letting the original battle settlement chain run normally

That distinction was critical.

## Root Cause Analysis

The final root cause is best described as a control-flow regression.

### Vanilla expectation

During normal battle completion, vanilla expects the return path to proceed through its original `Tools.loadMapScenes(...)` sequence.

That original sequence not only changes scenes, but also preserves the timing and ordering that other systems depend on.

Those systems include quest and commission settlement.

### LongLive behavior before the fix

LongLive fade optimization had a special fast path that redirected:

- current scene `YSNewFight`
- target scene `AllMaps`

into an async `NextScene` bridge.

This path improved perceived transition speed, but it changed the original map-return order.

That change was enough to disturb the settlement chain for the `协助杀妖` commission.

The result was:

- the commission condition was already satisfied
- but the original post-battle settlement sequence did not finish in the expected order
- visible completion and reward behavior broke

### Final conclusion

The regression was primarily caused by the battle return redirection inside fade optimization, not by the commission condition logic itself.

## Final Fix

The final fix intentionally stayed narrow.

### Kept

- battle kill-progress compensation in `Avatar.die.prefix`
- general fade optimization behavior
- black-screen shortening that does not rewrite the battle return pipeline

### Removed or disabled for this path

- the async `NextScene` bridge for the normal battle return path from `YSNewFight` back to `AllMaps`

The effective rule is now:

- keep the original `Tools.loadMapScenes(...)` path for battle return stability

This preserved most of the fade optimization work while avoiding the specific path that broke commission settlement.

## Temporary Experimental Code That Was Rolled Back

Several experimental paths were added during investigation and later removed once the real cause was confirmed.

Those paths included:

- LongLive-side auto-completion of the random commission
- direct clearing of NPC help-state as a primary fix
- direct reward notification as a commission-fix mechanism
- three-scene task icon cleanup as a primary fix
- scene-load cleanup hooks that only existed to support the failed auto-complete route

These were useful as diagnostic tools, but they were not appropriate as the final solution.

Removing them keeps the `0.2.1` code path smaller and safer.

## Methods Used

The investigation relied on a combination of code reading, probes, and repeated in-game validation.

### Decompiled code inspection

Relevant vanilla code was inspected from the decompiled managed assemblies, especially:

- `KBEngine.NomelTaskMag`
- `KBEngine.Avatar`
- `Fight.FightVictory`
- `UINPCData`
- `UINPCSVItem`
- `CmdCloseNPCTask`
- related task and UI classes

### Targeted runtime tracing

LongLive battle trace hooks were extended to capture:

- active commission task id and step type
- current target id and battle target id
- `killAvatar` progress state
- `canFinishTask`
- commission step snapshots after sync

### Behavioral isolation

The investigation repeatedly narrowed the problem by separating:

- kill-progress correctness
- commission completion correctness
- NPC marker correctness
- battle return flow correctness

This was what allowed the final cause to be isolated.

## Release Notes Impact

For `0.2.1`, the most relevant behavior change is:

- fade optimization still exists
- battle kill-progress compensation still exists
- normal battle return from `YSNewFight` to `AllMaps` no longer uses the async `NextScene` bridge

This means the release intentionally prefers settlement stability over the most aggressive return-path acceleration.

## Practical Guidance For Future Work

This investigation leaves a clear engineering rule for future transition work.

### Safe principle

Speeding up a transition is usually safer than rewriting its scene-routing strategy.

### Unsafe principle

If a transition is part of the original gameplay settlement chain, replacing its routing path can easily break unrelated systems that depend on timing and order.

### Recommended future boundary

For battle-return paths:

- allow visual shortening
- allow overlay or black-mask acceleration
- do not replace the original map-return control flow unless the new flow is validated against quest, reward, and UI settlement paths

## Files Touched By The Final Fix

- `src/LongLive.BepInEx/Plugin/BattleTrace/LongLiveBattleTracePatches.cs`
  - added commission kill-progress compensation from `Avatar.die.prefix`
- `src/LongLive.BepInEx/Plugin/BattleTrace/LongLiveBattleTraceRuntime.cs`
  - retained the commission kill-progress compensation and tracing
  - removed the failed auto-complete experiment before release
- `src/LongLive.BepInEx/Plugin/Transition/LongLiveFadeOptimizationRuntime.cs`
  - preserved fade optimization in general
  - disabled the async battle-return bridge for `YSNewFight -> AllMaps`

## Final Outcome

After disabling the async battle-return bridge for this path, the commission issue was resolved in live testing.

That result matched the final diagnosis:

- the commission condition repair was necessary
- but the regression itself came from the modified battle return flow
- restoring the original return pipeline for that path fixed the problem
