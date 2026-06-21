# Native Battle Adjudication

This document records the current `LongLive` native-core battle-adjudication path.

## 1. Purpose

The current native path exists for one narrow reason:

- provide a small, testable bridge for battle damage-segment evaluation
- keep the heavy decision surface outside the Unity host when practical
- prepare a cleaner extension point for later overflow and termination rules

It is not yet a full combat rewrite.

## 2. Current Data Flow

The current chain is:

1. battle guard logic enters `LongLiveBattleTraceRuntime.ShouldBlockPostDeathDamage(...)`
2. a `LongLiveBattleDamageSegmentContext` is created
3. `LongLiveBattleDamagePipeline` evaluates middleware in order
4. `LongLiveNativeDamageAdjudicationMiddleware` may call the Rust native core
5. the resulting `LongLiveBattleDamageSegmentDecision` is folded back into the guard logic

Relevant files:

- `src/LongLive.BepInEx/Plugin/Battle/LongLiveBattleDamagePipeline.cs`
- `src/LongLive.BepInEx/Plugin/Battle/LongLiveNativeDamageAdjudicationMiddleware.cs`
- `src/LongLive.BepInEx/Plugin/BattleTrace/LongLiveBattleTraceRuntime.cs`
- `src/LongLive.BepInEx/Native/LongLiveNativeBridge.cs`
- `src/LongLive.BepInEx/Native/LongLiveNativeService.cs`
- `native/longlive-native-core/src/lib.rs`

## 3. Current Native Contract

The native core currently accepts one damage-segment request:

- current HP
- incoming damage
- skill id
- damage type
- player-target flag
- multi-hit flag
- segment index

It currently returns:

- applied damage
- overflow damage
- predicted HP after the segment
- behavior flags

The exported Rust entry point is:

- `longlive_native_core_adjudicate_damage_segment`

## 4. Current Behavior

The current native implementation is intentionally conservative.

It currently handles:

- already-dead targets
- lethal segments that would push HP below zero
- overflow amount reporting
- skip-original-damage and skip-remaining-segments flags for lethal cases

It does not yet attempt to model broader combat semantics such as:

- buff-trigger side effects
- battle animation timing
- spell-loop termination nuances beyond the current guard integration
- game-specific damage modifiers that are not already present in the host-side context

## 5. Performance Note

The native bridge now caches loaded exports per resolved library path.

That means:

- the process no longer calls `LoadLibrary` and `GetProcAddress` on every adjudication call
- the plugin clears cached native handles during `LongLivePlugin.OnDestroy()`

This change is important because the damage-adjudication path can become high frequency once it is used inside active combat guard logic.

## 6. Current Limitations

The current native path still has several explicit limits:

- it remains opt-in through the existing host configuration and runtime availability checks
- it is only one middleware inside a larger host-side decision pipeline
- it does not yet own the final user-facing overflow presentation layer
- it does not yet synthesize battle-result UI changes on its own

## 7. Next Good Tests

The next useful validation targets are:

- verify native adjudication still behaves correctly during repeated lethal multi-hit cases
- compare guard behavior with and without the native library present
- confirm that export caching produces no lifecycle issues across plugin shutdown and reload
- identify whether the next real bottleneck is native adjudication itself or the surrounding game-side callback chain
