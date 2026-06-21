# Battle Trace Findings

This document records the current read-only battle tracing work for `LongLive-Lib` against `觅长生`.

The goal of this stage is not to change gameplay behavior. The goal is to identify the real runtime path that produces unstable combat results, especially negative non-player HP and repeated post-death damage processing.

## 1. Scope

Current tracing is limited to research and optimization-oriented observation.

It focuses on:

- battle entry
- round start and skill usage flow
- runtime HP writes on actual `KBEngine.Avatar` instances
- repeated damage application after a target has already entered negative HP
- buff-loop and spell tick re-entry that may continue after a target is already effectively dead

It does not yet attempt to:

- alter formulas
- clamp HP globally
- replace the game's combat engine
- re-route combat math into the Rust native core

## 2. Current Trace Layers

### Layer 1: Battle Flow

The first trace layer patches these methods:

- `Tools.startFight(int monstarID)`
- `FightPrepare.startFight()`
- `RoundManager.Awake()`
- `RoundManager.startRound(...)`
- `RoundManager.PlayerEndRound(...)`
- `RoundManager.UseSkill(...)`
- `RoundManager.endRound(...)`
- `CharacterSkillDeployer.DeploySkill(int)`
- `CharacterSkillDeployer.DeployWithAttacking(int)`

This layer proved the top-level flow is real and stable:

1. `Tools.startFight(...)`
2. `RoundManager.Awake()`
3. `RoundManager.startRound(...)`
4. `RoundManager.UseSkill(...)`
5. scene exit and fight finish

However, this layer alone was not enough to identify the real HP write path.

### Layer 2: Avatar HP Mutation

The second trace layer patches concrete `KBEngine.Avatar` methods:

- `KBEngine.Avatar.recvDamage(Entity, Entity, int, int, int)`
- `KBEngine.Avatar.recvDamage(int, int, int, int)`
- `KBEngine.Avatar.setHP(int)`
- `KBEngine.Avatar.onHPChanged(int oldValue)`
- `KBEngine.Avatar.AddHp(int)`

This is the first layer that exposed real enemy HP writes and negative HP persistence.

### Layer 3: Death and Victory Sequencing

The third trace layer patches these methods:

- `KBEngine.Avatar.setMonstarDeath()`
- `KBEngine.Avatar.die()`
- `KBEngine.Avatar.onStateChanged(sbyte oldValue)`
- `KBEngine.Avatar.onSubStateChanged(byte oldValue)`
- `Fight.FightResultMag.ShowVictory()`
- `Fight.FightVictory.SetVictory()`
- `Fungus.AvatarCheckDeath.OnEnter()`
- `Fungus.CheckNpcDeath.OnEnter()`
- `Fungus.AvatarDeath.OnEnter()`
- `NPCDeath.SetNpcDeath(...)`

This layer confirmed that fight victory presentation can begin before the final observed enemy death call completes.

### Layer 4: Buff and Spell Re-entry

The latest trace expansion now also covers these methods:

- `KBEngine.Buff.ListRealizeSeid71(int seid, Avatar avatar, List<int> buffInfo, List<int> flag)`
- `KBEngine.Buff.loopRealizeSeid(int seid, Entity avatar, List<int> buffInfo, List<int> flag)`
- `KBEngine.Buff.onLoopTrigger(Entity avatar, List<int> buffInfo, List<int> flag, BuffLoopData buffLoopData)`
- `KBEngine.Spell.onBuffTick(int index, List<int> flag, int type)`

This layer is specifically intended to explain why negative HP writes continue to appear after the first lethal transition.

## 3. Runtime Ownership Findings

The most important structural finding so far is:

- `RoundManager` is a battle flow controller
- actual combat HP state lives on `KBEngine.Avatar`

Relevant observed `KBEngine.Avatar` members include:

- field `HP`
- property `HP_Max`
- field `_HP_Max`
- method `setHP(int hp)`
- method `recvDamage(...)`
- method `onHPChanged(int oldValue)`

This means future defensive work should not start by modifying `RoundManager.PlayerTempHp` or `RoundManager.NpcTempHp`. Those fields did not explain the observed broken states in the traced fights.

## 4. Confirmed Findings

### Finding A: Non-player targets can enter negative HP

This is now confirmed with direct trace evidence.

Examples:

- `墨蛟`: `HP 1810 -> setHP(-132810)`
- `翼虎`: `HP 280 -> setHP(-134427)`
- `三目妖猴`: `HP 280 -> setHP(-1690)`

These are not just attempted intermediate values. In multiple cases, `Avatar.setHP(...)` postfix logs show that the negative value actually became the avatar's current HP.

### Finding B: Post-death damage processing can continue

This is also confirmed.

The clearest example is `翼虎`:

1. the target first enters negative HP
2. later damage packets continue to target the same enemy
3. the trace still shows repeated `recvDamage(...)` and `setHP(...)` attempts against an already-negative target

Representative sequence:

- first negative assignment to `-134427`
- later attempts to write `-379xxx`
- later attempts to write `-624xxx`

This strongly suggests that a multi-hit or chained damage path is not short-circuiting once the target is already effectively dead.

### Finding C: Large damage chains amplify the problem

High-damage attacks do not just overkill the enemy by a small amount. They can push enemy HP deeply negative and then continue trying to process additional damage packets.

Observed examples include results around:

- `269240`
- `4904xx`

This is a strong candidate root cause for later instability such as repeated processing, invalid death-state transitions, or fight-end lag.

### Finding D: Player over-max HP is not automatically a bug

The traced player avatar can legitimately exceed ordinary `HP_Max` expectations because the game has skills and systems that temporarily or conditionally increase player HP.

Therefore:

- player over-max HP should not be treated as a blanket error condition
- any future safety guard must distinguish player and non-player targets

### Finding E: `Avatar.recvDamage(...)` is not owned by the final damaged target

The latest trace shows that `KBEngine.Avatar.recvDamage(Entity, Entity, int, int, int)` is not reliably invoked on the final victim avatar.

In the high-damage multi-hit trace:

- the method call for the player skill is entered on the player-side avatar context
- the actual HP mutation still lands on the enemy through later `setHP(...)` calls

This means future guard logic should not assume `__instance` is the damaged target.

The real target must be derived from:

- the explicit receiver parameter
- the later `setHP(...)` target
- or a lower-level damage write path

### Finding F: Fight victory can begin before the enemy death path fully finishes

The latest trace captured this order for a high-damage multi-hit skill:

1. enemy `setHP(...)` enters negative HP
2. `Fight.FightResultMag.ShowVictory()` fires
3. repeated additional damage packets still execute against the already-negative enemy
4. only later does `Fight.FightVictory.SetVictory()` fire
5. only then does `Avatar.die()` run on the enemy avatar

This is a very important sequencing result.

It shows that the runtime can start victory presentation before the enemy's local death path is complete, while chained damage processing is still active.

### Finding G: The first observed lethal negative write is currently strongest on the `Buff -> Spell` loop path

The latest multi-hit trace against `阴灵蟒` captured the first negative HP write together with a compact stack trace:

1. `KBEngine.Spell.onBuffTick(...)`
2. `KBEngine.Buff.onLoopTrigger(...)`
3. `KBEngine.Buff.loopRealizeSeid(...)`
4. `KBEngine.Buff.ListRealizeSeid71(...)`
5. `KBEngine.Avatar.setHP(...)`

Representative evidence:

- first lethal write request: `requestedHp=-134288`
- target state at first hit: `state=4, subState=0, deathType=0, LunDaoState=3, HP=380, HP_Max=380, _HP_Max=380, name=阴灵蟒`
- first compact stack: `Avatar.setHP <= Buff.ListRealizeSeid71 <= MonoMethod.Invoke <= MethodBase.Invoke <= Buff.loopRealizeSeid <= Buff.onLoopTrigger <= Spell.onBuffTick`

This does not yet prove that `seid 71` is the only problematic path in the game.

It does prove that the currently observed lethal transition is not just a plain direct-hit path. A buff-loop / spell-tick realization path is actively involved.

### Finding H: The severe lag and stacked skill audio are consistent with a post-death re-entry storm

The latest run did not only reproduce negative enemy HP.

It also matched the user's observed runtime symptoms:

- combat suddenly becomes very laggy after the high-damage multi-hit skill begins
- repeated skill audio stacks and overlaps in an obviously abnormal way

The trace now gives a concrete explanation for that behavior.

In the same fight, after the first lethal negative write:

- `Spell.onBuffTick(... flag=[..., 7074, 0, 0, 0])` appeared `3597` times
- `Buff.onLoopTrigger(... flag=[..., 7074, 0, 0, 0])` appeared `3593` times
- `Buff.loopRealizeSeid(... flag=[..., 7074, 0, 0, 0])` appeared `4470` times
- `dead-avatar reentry` appeared `2963` times
- `dead-target damage-attempt` appeared `85` times

That volume is easily large enough to explain both frame-time spikes and repeated audio triggering.

This strengthens the conclusion that the problem is not just visual overkill or a harmless negative HP edge case. It is a runaway post-death processing loop.

### Finding I: The validated practical bottleneck is later `Spell.onBuffTick(...)` fan-out, not only corpse-side HP writes

Later validation runs with the experimental guard enabled showed that simply blocking corpse-side `Buff` re-entry was not enough to remove lag completely.

Representative counts from a guarded run still showed:

- `Spell.onBuffTick(...)` after the first negative write: about `5997`
- `Buff.onLoopTrigger(...)` after the first negative write: about `5317`
- `Buff.loopRealizeSeid(...)` after the first negative write: about `7297`

This proved that the real runtime hotspot was higher in the chain than plain dead-target `setHP(...)` writes.

The most effective experimental mitigation therefore became:

1. mark the offending `skillId` once dead-target damage or dead-target re-entry is confirmed
2. short-circuit later `Spell.onBuffTick(...)` dispatch for the same marked skill path

After that wider short-circuit was enabled, the same high-damage multi-hit test became visibly smooth in-game.

That is the strongest current indication that the lag root cause is a post-lethal skill-tick dispatch storm rather than only negative HP storage.

### Finding J: Broad detection is now practical without skill-by-skill manual testing

The tracing layer now also emits automatic battle-level summaries at important combat checkpoints.

Current checkpoints include:

- `Fight.FightResultMag.ShowVictory()`
- `Fight.FightVictory.SetVictory()`
- `Avatar.die()`
- the next `ResetBattleState()` before a later fight begins

Each summary is intended to answer a broader question:

- which avatars reached repeated negative-HP writes most often
- which `skillId` values dominated damage attempts or `recvDamage(...)`
- which `skillId` values dominated `Spell.onBuffTick(...)`
- which `skillId` values were actually blocked by the experimental guard
- which `buffID` and `seid` values dominated `Buff.onLoopTrigger(...)`, `Buff.loopRealizeSeid(...)`, and `Buff.ListRealizeSeid71(...)`

This is important for the next validation stage.

The user does not currently have reliable manual familiarity with every in-game skill path that might trigger the problem.

So future validation should prefer:

- broader battle sampling
- automatic top-counter inspection
- identifying recurrent high-volume `skillId` / `buffID` / `seid` clusters from summary output

instead of assuming one manually reproduced skill is representative of the entire combat system.

### Finding K: Segment-level decision telemetry is now part of the trace surface

The current experimental guard is no longer only a binary block/no-block switch.

It now records segment-level damage decisions from the new battle pipeline layer, including:

- decision reason counts such as `already-dead` or `native-lethal`
- top `skillId` values that produced native lethal decisions
- top `skillId` values that produced overflow
- aggregate overflow amount grouped by `skillId`

This does not yet change the in-game floating damage text.

It does provide the first structured runtime surface needed for later work on:

- overflow presentation
- skill-chain termination after the first lethal segment
- middleware-based damage multipliers or rule modifiers

## 5. Session Note: 2026-06-18

Two additional local validation fights were recorded in the current workshop-driven BepInEx runtime.

### Run A: Ordinary one-shot kill

This run behaved like a fast lethal finish instead of a runaway post-death storm.

Observed summary highlights:

- checkpoint at `Fight.FightResultMag.ShowVictory()` with `trackedEvents=1961`
- target: `嗜焰蟒`
- top negative avatar count: `嗜焰蟒|180|180=1`
- top `recvDamage(...)` skill IDs: `10006=3`, `13805=1`
- top `Spell.onBuffTick(...)` skill IDs: `1=111`, `unknown=64`, `13805=23`, `10006=6`

Representative lethal sequence:

- first lethal negative write observed at about `requestedHp=-134598`
- later attempted write observed at about `requestedHp=-404155`
- `Avatar.setHP(...)` postfix did not continue changing the stored HP after the first lethal transition

This run still confirms that negative HP is possible.

However, it does not resemble the previously reproduced severe lag case.

### Run B: High-damage multi-hit case

This run again reproduced the known runaway multi-hit family centered on `skillId=7074`.

Observed summary highlights:

- checkpoint at `Fight.FightVictory.SetVictory()` with `trackedEvents=25568`
- target: `藤蛇`
- top negative avatar count: `藤蛇|1410|1200=82`
- top damage-attempt skill ID: `7074=80`
- top `recvDamage(...)` skill IDs: `7074=83`, `10006=8`
- top `Spell.onBuffTick(...)` skill IDs: `7074=4235`, `1=476`, `unknown=168`
- top blocked damage skill ID: `7074=80`
- top blocked spell-tick skill ID: `7074=4060`
- total negative-HP writes: `82`
- total blocked post-death damage calls: `80`

Representative runtime evidence:

- the guard marked `skillId=7074` for later spell short-circuit
- repeated blocked post-death damage continued to target `藤蛇`
- representative dead-target HP remained around `HP=-121119`

This run confirms that the previous practical bottleneck is still the correct one:

- later `Spell.onBuffTick(...)` fan-out for the same skill path
- plus repeated post-death damage attempts against an already-negative target

### Important deployment note for this session

These two runs were performed before the newly built host DLL containing the latest segment-decision summary keys was redeployed into the active game plugin directory.

As a result, the session log does not yet contain the newly added summary lines for:

- `battle summary decision reasons`
- `battle summary top native lethal skillId`
- `battle summary top overflow skillId`
- `battle summary top overflow amount by skillId`

After this was noticed, the latest host build was deployed into:

- `D:/Appdata/Steam/steamapps/workshop/content/1189490/2824349934/BepInEx/plugins/LongLive.BepInEx.dll`

That means the next validation run should be treated as the first real in-game check of the newer segment-decision telemetry layer.

### Follow-up verification after redeploy

After the updated host DLL was redeployed, another two-fight validation pass confirmed that the newer segment-decision telemetry is now live in the game runtime.

#### Run C: Ordinary one-shot kill with segment telemetry

Observed runtime evidence:

- `battle-guard observed native lethal candidate: skillId=13805`
- target: `泽蟒`
- representative overflow: `203910`
- representative predicted HP after the lethal segment: `0`

Observed summary highlights:

- `battle summary decision reasons: pass-through=2, native-lethal=1`
- `battle summary top native lethal skillId: 13805=1`
- `battle summary top overflow skillId: 13805=1`
- `battle summary top overflow amount by skillId: 13805=203910`

This is the first clear in-game confirmation that the new telemetry can detect an ordinary lethal segment without immediately classifying the rest of the fight as a runaway post-death storm.

In other words:

- the lethal segment was observed
- overflow was measured
- the fight still ended normally

That matches the intended near-term behavior much better than the earlier version that risked treating the first lethal segment itself as a block target.

#### Run D: High-damage multi-hit case with segment telemetry

Observed runtime evidence:

- `battle-guard observed native lethal candidate: skillId=7074`
- target: `岩鳄`
- representative first-segment overflow: `203843`
- later blocked damage entries switched to `reason=already-dead`
- the guard still marked `skillId=7074` for later spell short-circuit once the target had already entered a dead/negative state

Observed summary highlights:

- `battle summary decision reasons: already-dead=46, pass-through=5, native-lethal=1`
- `battle summary top native lethal skillId: 7074=1`
- `battle summary top overflow skillId: 7074=47`
- `battle summary top overflow amount by skillId: 7074=206104`

This is an important separation-of-concerns result.

The current trace now distinguishes:

1. the first lethal segment candidate
2. later post-death damage attempts on the same skill path

That is exactly the distinction needed for a safer future implementation where:

- the first lethal segment is allowed to resolve through the game’s ordinary death path
- later same-path segments are suppressed once the target is already dead or already below zero

The run also confirms that the practical post-death bottleneck remains the same family of behavior seen before:

- repeated `skillId=7074` damage attempts
- later `Spell.onBuffTick(...)` fan-out
- repeated corpse-target processing after negative HP has already been reached

### Follow-up verification after termination-token activation

Another validation pass was performed after skill-path termination was activated immediately on the first observed native lethal segment.

#### Run E: Ordinary one-shot kill with early skill termination

Observed runtime evidence:

- `battle-guard activated skill termination: skillId=13805, reason=native-lethal`
- immediate later `Spell.onBuffTick(...)` blocks for the same `skillId`
- representative overflow: `203950`

Observed summary highlights:

- first checkpoint at `Fight.FightResultMag.ShowVictory()` with `trackedEvents=1428`
- `Spell.onBuffTick=153`
- `battle-guard.blocked-spell-tick=7` at the earliest checkpoint
- later checkpoint at `Fight.FightVictory.SetVictory()` with `Spell.onBuffTick=176`
- `battle-guard.blocked-spell-tick=30`
- `battle summary decision reasons: pass-through=2, native-lethal=1`

This confirms that termination now activates before the target is already deep into repeated corpse-side processing.

It also shows that an ordinary lethal skill can now short-circuit later same-path spell ticks much earlier than before.

#### Run F: High-damage multi-hit case with early skill termination

Observed runtime evidence:

- `battle-guard activated skill termination: skillId=7074, reason=native-lethal`
- immediate later `Spell.onBuffTick(...)` blocks for the same `skillId`
- later blocked damage still appears with `reason=already-dead`, but now after termination was already active
- representative first-segment overflow: `203903`

Observed summary highlights at the first victory checkpoint:

- `trackedEvents=1554`
- `Spell.onBuffTick=168`
- `battle-guard.blocked-spell-tick=7`
- `battle summary decision reasons: pass-through=2, native-lethal=1`
- `battle summary top native lethal skillId: 7074=1`
- `battle summary top overflow amount by skillId: 7074=203903`

This is a major reduction compared with the earlier multi-hit runs that produced thousands of `Spell.onBuffTick(...)` calls before fight cleanup.

The current evidence therefore suggests:

1. the first lethal segment is now detected early enough to activate a skill-path termination token
2. later same-path spell ticks are being suppressed much earlier than in the older guard-only model
3. some later corpse-side damage attempts still exist, but the scale of the fan-out storm has been dramatically reduced

### Follow-up trace hygiene after early termination validation

After the in-game validation confirmed that the high-damage multi-hit case had already become visibly smooth, the next local refinement step shifted from raw mitigation to trace hygiene.

The main purpose of this refinement is not a new gameplay change.

It is to make later sampling easier to read and compare by reducing repeated log spam and by separating different guard outcomes more explicitly in battle summaries.

The trace layer now additionally tracks:

- blocked damage reasons grouped independently from blocked damage `skillId` counts
- blocked spell-tick reasons grouped independently from blocked spell-tick `skillId` counts
- skill termination activation reasons grouped across the fight

Repeated per-event guard logs are also now throttled by repetition count instead of printing every identical blocked spell tick or blocked corpse-side damage event.

This refinement matters because the current bottleneck is no longer obvious frame collapse in the reproduced test.

The harder question now is narrower:

- how much repeated `Spell.onBuffTick(...)` traffic still enters the patched prefix after termination is already active
- how much later `recvDamage(...)` traffic is still reaching the corpse-side guard path
- whether the remaining behavior is mostly harmless residual dispatch or a sign that an even earlier upstream stop-point is still worth implementing

The next validation run should therefore prefer summary interpretation over raw line-count scrolling.

In particular, these summary lines should now be treated as first-line indicators:

- `battle summary blocked damage reasons`
- `battle summary blocked spell tick reasons`
- `battle summary skill termination reasons`

### Follow-up verification after `Spell.onBuffTickByType(...)` source separation

Another trace refinement pass promoted the spell-side summary into two distinct layers:

- `Spell.onBuffTick(...)`
- `Spell.onBuffTickByType(...)`

This produced a much clearer result in the high-damage multi-hit path.

#### Run G: High-damage multi-hit with `onBuffTickByType` source separation

Observed summary highlights:

- `Spell.onBuffTickByType=1511`
- `Spell.onBuffTick=370`
- `battle-guard.blocked-spell-tick=441`
- `battle summary blocked spell tick sources: Spell.onBuffTickByType=441`
- `battle summary blocked damage reasons: terminated-skill-path=27`

This is the first direct proof that the surviving post-lethal spell storm is no longer reaching the older `Spell.onBuffTick(...)` guard surface as its primary block point.

Instead, the dominant surviving dispatch layer is now `Spell.onBuffTickByType(...)` itself.

That matters because it narrows the next optimization question from:

- where is the spell storm happening at all

to:

- whether specific `type` branches inside `Spell.onBuffTickByType(...)` can be short-circuited safely once a skill path has already been terminated

#### Run H: High-damage multi-hit with skill-type breakdown

The next refinement pass added `skillId@type` grouping for `Spell.onBuffTickByType(...)` and for blocked spell ticks.

Observed summary highlights:

- top general background traffic still heavily favored normal-looking combinations such as `1@11`, `1@36`, and `1@38`
- blocked spell ticks remained entirely sourced from `Spell.onBuffTickByType(...)`
- the dominant blocked combinations for the reproduced `7074` case were not those broad background pairs
- instead, repeated blocked combinations clustered around a stable family including:
  `7074@7`, `7074@8`, `7074@9`, `7074@28`, `7074@29`, `7074@30`, `7074@31`, `7074@33`, `7074@37`, `7074@40`, `7074@42`, `7074@46`, `7074@47`, `7074@50`, `7074@51`, `7074@54`

Representative summary evidence:

- `battle summary blocked spell tick skill-type: 7074@28=29, 7074@31=29, 7074@33=29, 7074@40=29, 7074@42=29, ...`
- `battle summary blocked spell tick type breakdown for top blocked skillId: skillId=7074, 7=29, 8=29, 28=29, 31=29, 33=29, 40=29, 42=29, 50=29, 51=29, 54=29, 9=28, 29=28, 37=28, 46=28, 47=28, 30=27`

This is a stronger constraint than the earlier broad type histogram alone.

It shows that the confirmed unstable chain is not simply:

- all `type=38`
- all `type=11`
- all `type=36`

Those broad type groups still carry large volumes of apparently normal background spell traffic in ordinary fights.

The practical implication is therefore more conservative and more precise:

- future earlier short-circuit work should key off terminated `skillId` plus specific `type` combinations
- a blanket block by broad `type` alone would be much riskier than a terminated-skill-aware `skillId@type` filter

#### Run I: Validation after spell-context reconciliation and hot-path log demotion

Another in-game validation pass was performed after two trace-only refinements:

- spell-side guard lookup now attempts to reconcile `flag`-derived and `Spell`-derived skill identity before making a block decision
- the hottest buff/spell prefix logs were demoted to verbose-only output to reduce avoidable logging overhead during sampling

Observed summary highlights at the late-fight checkpoint:

- `Spell.onBuffTickByType=1379`
- `Spell.onBuffTick=408`
- `battle-guard.blocked-spell-tick=473`
- `battle summary blocked spell tick sources: Spell.onBuffTickByType=473`
- `battle summary blocked spell tick skill-type: 7074@28=30, 7074@31=30, 7074@33=30, 7074@40=30, 7074@42=30, ...`
- `battle summary blocked damage reasons: terminated-skill-path=29`
- `battle summary blocked spell tick reasons: native-lethal=473`

Representative blocked-type breakdown for the top blocked skill remained:

- `7=30`
- `8=30`
- `28=30`
- `31=30`
- `33=30`
- `40=30`
- `42=30`
- `50=30`
- `51=30`
- `54=30`
- `9=29`
- `29=29`
- `37=29`
- `46=29`
- `47=29`
- `30=28`

This run adds two useful constraints.

First, no `spell skillId mismatch` line was emitted during the sampled fight.

That means the current reproduced `7074` post-lethal storm is not explained by a newly discovered disagreement between:

- the `flag`-derived skill context
- and the reflected `Spell` instance context

In other words, the active `7074` guard hit is still anchored to a stable skill-side identity in the observed storm path.

Second, the blocked `skillId@type` family stayed materially consistent with the earlier runs.

That consistency strengthens the earlier interpretation:

- the core problem is still later upstream spell dispatch continuing after lethal termination has already been activated for `7074`
- the current bottleneck is not primarily a misidentified skill context
- broad background traffic such as `1@11`, `1@36`, and `1@38` remains separate from the confirmed unstable `7074` family

The practical next step is therefore not a broad new block rule.

It is to keep any further earlier stop-point work tightly constrained to:

- terminated skills only
- and, if needed, the stable reproduced `7074`-style `skillId@type` family rather than broad type-wide suppression

## 6. Representative Trace Evidence

### Case 1: High-damage skill against `墨蛟`

Observed path:

- `RoundManager.UseSkill(...)`
- `Avatar.recvDamage(...)` for skill `13805`
- enemy-side `Avatar.setHP(...)` enters negative HP

Representative values:

- incoming damage: `204210`
- enemy HP before hit: `1810`
- first negative write: `-132810`
- later attempted write: `-402050`

### Case 2: High-damage repeated processing against `翼虎`

Observed path:

- `RoundManager.UseSkill(...)`
- repeated `Avatar.recvDamage(...)` for skill `7074`
- enemy remains at a previously negative HP value while later damage packets continue to arrive

Representative values:

- enemy base HP: `280`
- first negative write: `-134427`
- later attempted writes: `-379xxx`, `-624xxx`
- later damage applications can return `0` once a previously-negative state is reached, but the repeated attempts still happen

### Case 3: Another non-player target `三目妖猴`

Observed path:

- enemy HP before hit: `280`
- write target: `-1690`

This confirms the issue is not limited to one specific monster template.

### Case 4: `阴灵蟒` buff-loop lethal re-entry

Observed sequence:

1. `Avatar.recvDamage(entity)` for skill `7074`
2. first negative write at `Avatar.setHP(requestedHp=-134288)`
3. stack trace points into `Buff.ListRealizeSeid71 -> Buff.loopRealizeSeid -> Buff.onLoopTrigger -> Spell.onBuffTick`
4. `Fight.FightResultMag.ShowVictory()` fires almost immediately afterward
5. repeated dead-target damage attempts continue, with `priorNegativeHits` eventually reaching at least `166`
6. only later do `Fight.FightVictory.SetVictory()` and `Avatar.die()` appear

Representative values:

- target name: `阴灵蟒`
- target HP before first lethal write: `380`
- first lethal request: `-134288`
- later attempted writes: `-379xxx`, `-624xxx`
- repeated follow-up damage packets: around `2041xx` to `2043xx`

## 7. Current Interpretation

The current evidence suggests the combat instability is not just a balance problem.

It is at least partly a state-management problem:

- enemy HP is allowed to go negative
- death does not fully stop later chained damage steps
- subsequent damage packets can continue to target dead enemies
- a buff-tick / loop-realize path can continue participating in lethal and post-lethal processing

That is a better explanation for fight-side lag or pathological behavior than a simple “damage number too high” narrative.

The damage number matters, but the structural issue is that the battle pipeline continues to process dead targets.

The latest trace refines that statement further:

- the enemy can enter negative HP first
- the fight can already start moving toward victory
- chained damage can still continue before `Avatar.die()` is finally observed
- the first observed lethal transition can arrive through `Spell.onBuffTick(...)` and `Buff.*` re-entry rather than a single isolated direct damage write

That is a sequencing bug or sequencing hazard, not only a numeric overflow symptom.

## 8. What Is Not Yet Proven

The current trace still does not fully prove:

- which exact method flips an avatar into the game's final death state
- whether the repeat processing is fully owned by buff logic, or whether later combat queues also re-enter the same target
- whether there are additional int32 overflow sites beyond plain negative HP writes
- whether `recvDamage(...)` itself owns the repeated chaining, or whether later systems re-enter it
- why `Fight.FightResultMag.ShowVictory()` can appear before the final observed `Avatar.die()` call

## 9. Recommended Next Trace Layer

The next read-only trace layer should focus on death-state transition and post-death re-entry.

Priority goals:

- identify the concrete death-state method(s) on non-player avatars
- detect whether a dead target is still considered valid by later damage segments
- log whether the same target instance is re-entered after `HP <= 0`

Candidate targets include:

- `setMonstarDeath()` on `KBEngine.Avatar`
- other death or death-check methods reachable from the avatar or combat UI path
- additional fight event processors if runtime inspection shows they own death resolution
- `Fungus.AvatarCheckDeath`
- `Fungus.CheckNpcDeath`
- any combat-side method that transitions from negative HP to final cleanup

## 10. Guard Strategy Implications

The current findings argue against a naive global clamp.

The safer long-term approach is likely:

1. keep tracing until the death transition boundary is explicit
2. add an optional experimental guard for non-player battle targets only
3. short-circuit repeated damage processing once a target is already dead or already below zero

This should be evaluated before any broader HP clamping strategy.

The current experimental direction inside `LongLive-Lib` is even narrower than that:

- do not touch player avatars
- do not rewrite damage formulas
- do not clamp all `setHP(...)` writes
- only block confirmed non-player post-death damage and re-entry on paths already proven unstable
- once a skill path is confirmed unstable, short-circuit later `Spell.onBuffTick(...)` dispatch for that marked skill path

This is intended as a minimal runtime safety brake for the confirmed storm path, not as a general combat rebalance.

## 10. Practical Conclusion

The repo now has enough evidence to justify a future experimental combat safety guard.

The most defensible first protection target is not “all HP everywhere”.

It is:

- non-player combat targets
- in-fight negative HP entry
- repeated post-death damage application
- later post-death `Spell.onBuffTick(...)` fan-out for the same unstable skill path

That boundary matches the observed failures and avoids breaking legitimate player-side HP expansion mechanics.

## 11. Current Validation Status

The current experimental guard is no longer only theoretical.

It has already been validated in live in-game testing against the high-damage multi-hit scenario that previously caused:

- severe frame-time drops
- obvious audio stacking
- repeated corpse-side re-entry traces

With the wider skill-path short-circuit in place, the same scenario became visibly smooth according to direct user validation.

This does not yet prove that every problematic combat path in the game is solved.

It does prove that `LongLive-Lib` now has a working experimental mitigation for the confirmed `7074`-style post-lethal tick storm.
