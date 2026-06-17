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

## 5. Representative Trace Evidence

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

## 6. Current Interpretation

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

## 7. What Is Not Yet Proven

The current trace still does not fully prove:

- which exact method flips an avatar into the game's final death state
- whether the repeat processing is fully owned by buff logic, or whether later combat queues also re-enter the same target
- whether there are additional int32 overflow sites beyond plain negative HP writes
- whether `recvDamage(...)` itself owns the repeated chaining, or whether later systems re-enter it
- why `Fight.FightResultMag.ShowVictory()` can appear before the final observed `Avatar.die()` call

## 8. Recommended Next Trace Layer

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

## 9. Guard Strategy Implications

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
