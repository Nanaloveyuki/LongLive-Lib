using HarmonyLib;
using KBEngine;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(Tools), "startFight")]
internal static class LongLiveBattleTraceToolsStartFightPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceToolsStartFightPatch));
    }

    public static void Prefix(int monstarID)
    {
        LongLiveBattleTraceRuntime.ResetBattleState();
        LongLiveBattleTraceRuntime.TrackBattlePhase("Tools.startFight");
        LongLiveBattleTraceRuntime.Log($"Tools.startFight prefix: monstarID={monstarID}, scene={LongLiveBattleTraceRuntime.ActiveSceneName()}");
    }
}

[HarmonyPatch(typeof(FightPrepare), "startFight")]
internal static class LongLiveBattleTraceFightPrepareStartFightPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceFightPrepareStartFightPatch));
    }

    public static void Prefix(FightPrepare __instance)
    {
        LongLiveBattleTraceRuntime.ResetBattleState();
        LongLiveBattleTraceRuntime.TrackBattlePhase("FightPrepare.startFight");
        LongLiveBattleTraceRuntime.Log($"FightPrepare.startFight prefix: scene={LongLiveBattleTraceRuntime.ActiveSceneName()}, instanceType={__instance.GetType().FullName}");
    }
}

[HarmonyPatch(typeof(RoundManager), "Awake")]
internal static class LongLiveBattleTraceRoundManagerAwakePatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceRoundManagerAwakePatch));
    }

    public static void Postfix(RoundManager __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.Awake");
        LongLiveBattleTraceRuntime.Log($"RoundManager.Awake postfix: {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
        LongLiveBattleTraceRuntime.LogRoundManagerTypeSnapshot(__instance);
    }
}

[HarmonyPatch(typeof(RoundManager), "startRound")]
internal static class LongLiveBattleTraceRoundManagerStartRoundPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceRoundManagerStartRoundPatch));
    }

    public static void Prefix(RoundManager __instance, object _avater)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.startRound.prefix");
        LongLiveBattleTraceRuntime.Log($"RoundManager.startRound prefix: actor={LongLiveBattleTraceRuntime.DescribeEntity(_avater)}, {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
    }

    public static void Postfix(RoundManager __instance, object _avater)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.startRound.postfix");
        LongLiveBattleTraceRuntime.Log($"RoundManager.startRound postfix: actor={LongLiveBattleTraceRuntime.DescribeEntity(_avater)}, {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
    }
}

[HarmonyPatch(typeof(RoundManager), "PlayerEndRound")]
internal static class LongLiveBattleTraceRoundManagerPlayerEndRoundPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceRoundManagerPlayerEndRoundPatch));
    }

    public static void Prefix(RoundManager __instance, bool canCancel)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.PlayerEndRound");
        LongLiveBattleTraceRuntime.Log($"RoundManager.PlayerEndRound prefix: canCancel={canCancel}, {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
    }
}

[HarmonyPatch(typeof(RoundManager), "UseSkill")]
internal static class LongLiveBattleTraceRoundManagerUseSkillPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceRoundManagerUseSkillPatch));
    }

    public static void Prefix(RoundManager __instance, string uuid, bool showTip)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.UseSkill.prefix");
        LongLiveBattleTraceRuntime.Log($"RoundManager.UseSkill prefix: uuid={uuid}, showTip={showTip}, {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
    }

    public static void Postfix(RoundManager __instance, string uuid, bool showTip, bool __result)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.UseSkill.postfix");
        LongLiveBattleTraceRuntime.Log($"RoundManager.UseSkill postfix: uuid={uuid}, showTip={showTip}, result={__result}, {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
    }
}

[HarmonyPatch(typeof(RoundManager), "endRound")]
internal static class LongLiveBattleTraceRoundManagerEndRoundPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceRoundManagerEndRoundPatch));
    }

    public static void Prefix(RoundManager __instance, object _avater)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.endRound.prefix");
        LongLiveBattleTraceRuntime.Log($"RoundManager.endRound prefix: actor={LongLiveBattleTraceRuntime.DescribeEntity(_avater)}, {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
    }

    public static void Postfix(RoundManager __instance, object _avater)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("RoundManager.endRound.postfix");
        LongLiveBattleTraceRuntime.Log($"RoundManager.endRound postfix: actor={LongLiveBattleTraceRuntime.DescribeEntity(_avater)}, {LongLiveBattleTraceRuntime.DescribeRoundManager(__instance)}");
    }
}

[HarmonyPatch(typeof(CharacterSkillDeployer), "DeploySkill", new[] { typeof(int) })]
internal static class LongLiveBattleTraceCharacterSkillDeployerDeploySkillPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceCharacterSkillDeployerDeploySkillPatch));
    }

    public static void Prefix(CharacterSkillDeployer __instance, int index)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("CharacterSkillDeployer.DeploySkill");
        LongLiveBattleTraceRuntime.Log($"CharacterSkillDeployer.DeploySkill prefix: index={index}, {LongLiveBattleTraceRuntime.DescribeCharacterSkillDeployer(__instance)}");
    }
}

[HarmonyPatch(typeof(CharacterSkillDeployer), "DeployWithAttacking", new[] { typeof(int) })]
internal static class LongLiveBattleTraceCharacterSkillDeployerDeployWithAttackingPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceCharacterSkillDeployerDeployWithAttackingPatch));
    }

    public static void Prefix(CharacterSkillDeployer __instance, int index)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("CharacterSkillDeployer.DeployWithAttacking");
        LongLiveBattleTraceRuntime.Log($"CharacterSkillDeployer.DeployWithAttacking prefix: index={index}, {LongLiveBattleTraceRuntime.DescribeCharacterSkillDeployer(__instance)}");
    }
}

[HarmonyPatch(typeof(Avatar), "recvDamage", new[] { typeof(Entity), typeof(Entity), typeof(int), typeof(int), typeof(int) })]
internal static class LongLiveBattleTraceAvatarRecvDamageEntityPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarRecvDamageEntityPatch));
    }

    public static bool Prefix(Avatar __instance, Entity _attaker, Entity _receiver, int skillId, int damage, int type, out int __state)
    {
        __state = __instance.HP;
        LongLiveBattleTraceRuntime.TrackDamageInvocation(skillId, "Avatar.recvDamage.entity");
        LongLiveBattleTraceRuntime.TrackDeadTargetDamageAttempt(_attaker, skillId, damage, "recvDamage.entity.attacker");
        LongLiveBattleTraceRuntime.TrackDeadTargetDamageAttempt(_receiver, skillId, damage, "recvDamage.entity.receiver");
        LongLiveBattleTraceRuntime.TrackDeadTargetDamageAttempt(__instance, skillId, damage, "recvDamage.entity.instance");

        if (LongLiveBattleTraceRuntime.ShouldBlockPostDeathDamage(_receiver, "recvDamage.entity.receiver", skillId, damage))
        {
            return false;
        }

        LongLiveBattleTraceRuntime.Log(
            $"Avatar.recvDamage(entity) prefix: skillId={skillId}, damage={damage}, type={type}, attacker={LongLiveBattleTraceRuntime.DescribeEntity(_attaker)}, receiver={LongLiveBattleTraceRuntime.DescribeEntity(_receiver)}, target={LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, __state)}");
        return true;
    }

    public static void Postfix(Avatar __instance, int skillId, int damage, int type, int __state, int __result)
    {
        LongLiveBattleTraceRuntime.Log(
            $"Avatar.recvDamage(entity) postfix: skillId={skillId}, damage={damage}, type={type}, result={__result}, {LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, __state)}");
    }
}

[HarmonyPatch(typeof(Avatar), "recvDamage", new[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
internal static class LongLiveBattleTraceAvatarRecvDamageSimplePatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarRecvDamageSimplePatch));
    }

    public static bool Prefix(Avatar __instance, int attackerID, int skillID, int damageType, int damage, out int __state)
    {
        __state = __instance.HP;
        LongLiveBattleTraceRuntime.TrackDamageInvocation(skillID, "Avatar.recvDamage.simple");
        LongLiveBattleTraceRuntime.TrackDeadTargetDamageAttempt(__instance, skillID, damage, "recvDamage.simple.instance");

        if (LongLiveBattleTraceRuntime.ShouldBlockPostDeathDamage(__instance, "recvDamage.simple.instance", skillID, damage))
        {
            return false;
        }

        LongLiveBattleTraceRuntime.Log(
            $"Avatar.recvDamage(simple) prefix: attackerID={attackerID}, skillID={skillID}, damageType={damageType}, damage={damage}, {LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, __state)}");
        return true;
    }
}

[HarmonyPatch(typeof(Avatar), "setHP")]
internal static class LongLiveBattleTraceAvatarSetHPPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarSetHPPatch));
    }

    public static void Prefix(Avatar __instance, int hp, out int __state)
    {
        __state = __instance.HP;
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.setHP");
        LongLiveBattleTraceRuntime.TrackNegativeHpWrite(__instance, hp, "Avatar.setHP");
        LongLiveBattleTraceRuntime.Log($"Avatar.setHP prefix: requestedHp={hp}, {LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, __state, hp)}");
    }

    public static void Postfix(Avatar __instance, int hp, int __state)
    {
        LongLiveBattleTraceRuntime.Log($"Avatar.setHP postfix: requestedHp={hp}, {LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, __state)}");
    }
}

[HarmonyPatch(typeof(Avatar), "onHPChanged")]
internal static class LongLiveBattleTraceAvatarOnHPChangedPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarOnHPChangedPatch));
    }

    public static void Prefix(Avatar __instance, int oldValue)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.onHPChanged");
        LongLiveBattleTraceRuntime.Log($"Avatar.onHPChanged prefix: {LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, oldValue)}");
    }
}

[HarmonyPatch(typeof(Avatar), "AddHp")]
internal static class LongLiveBattleTraceAvatarAddHpPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarAddHpPatch));
    }

    public static void Prefix(Avatar __instance, int addNum, out int __state)
    {
        __state = __instance.HP;
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.AddHp");
        LongLiveBattleTraceRuntime.Log($"Avatar.AddHp prefix: addNum={addNum}, {LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, __state)}");
    }

    public static void Postfix(Avatar __instance, int addNum, int __state)
    {
        LongLiveBattleTraceRuntime.Log($"Avatar.AddHp postfix: addNum={addNum}, {LongLiveBattleTraceRuntime.DescribeHpTransition(__instance, __state)}");
    }
}

[HarmonyPatch(typeof(Avatar), "setMonstarDeath")]
internal static class LongLiveBattleTraceAvatarSetMonstarDeathPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarSetMonstarDeathPatch));
    }

    public static void Prefix(Avatar __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.setMonstarDeath.prefix");
        LongLiveBattleTraceRuntime.Log($"Avatar.setMonstarDeath prefix: {LongLiveBattleTraceRuntime.DescribeAvatarState(__instance)}");
    }

    public static void Postfix(Avatar __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.setMonstarDeath.postfix");
        LongLiveBattleTraceRuntime.Log($"Avatar.setMonstarDeath postfix: {LongLiveBattleTraceRuntime.DescribeAvatarState(__instance)}");
    }
}

[HarmonyPatch(typeof(Avatar), "die")]
internal static class LongLiveBattleTraceAvatarDiePatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarDiePatch));
    }

    public static void Prefix(Avatar __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.die.prefix");
        LongLiveBattleTraceRuntime.EmitBattleSummaryIfChanged("Avatar.die.prefix");
        LongLiveBattleTraceRuntime.Log($"Avatar.die prefix: {LongLiveBattleTraceRuntime.DescribeAvatarState(__instance)}");
    }

    public static void Postfix(Avatar __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.die.postfix");
        LongLiveBattleTraceRuntime.EmitBattleSummaryIfChanged("Avatar.die.postfix");
        LongLiveBattleTraceRuntime.Log($"Avatar.die postfix: {LongLiveBattleTraceRuntime.DescribeAvatarState(__instance)}");
    }
}

[HarmonyPatch(typeof(Avatar), "onStateChanged")]
internal static class LongLiveBattleTraceAvatarOnStateChangedPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarOnStateChangedPatch));
    }

    public static void Prefix(Avatar __instance, sbyte oldValue)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.onStateChanged");
        LongLiveBattleTraceRuntime.Log($"Avatar.onStateChanged prefix: oldState={oldValue}, {LongLiveBattleTraceRuntime.DescribeAvatarState(__instance)}");
    }
}

[HarmonyPatch(typeof(Avatar), "onSubStateChanged")]
internal static class LongLiveBattleTraceAvatarOnSubStateChangedPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceAvatarOnSubStateChangedPatch));
    }

    public static void Prefix(Avatar __instance, byte oldValue)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Avatar.onSubStateChanged");
        LongLiveBattleTraceRuntime.Log($"Avatar.onSubStateChanged prefix: oldSubState={oldValue}, {LongLiveBattleTraceRuntime.DescribeAvatarState(__instance)}");
    }
}

[HarmonyPatch(typeof(Fight.FightResultMag), "ShowVictory")]
internal static class LongLiveBattleTraceFightResultShowVictoryPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceFightResultShowVictoryPatch));
    }

    public static void Prefix()
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Fight.FightResultMag.ShowVictory");
        LongLiveBattleTraceRuntime.EmitBattleSummaryIfChanged("Fight.FightResultMag.ShowVictory");
        LongLiveBattleTraceRuntime.Log($"Fight.FightResultMag.ShowVictory prefix: scene={LongLiveBattleTraceRuntime.ActiveSceneName()}");
    }
}

[HarmonyPatch(typeof(Fight.FightVictory), "SetVictory")]
internal static class LongLiveBattleTraceFightVictorySetVictoryPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceFightVictorySetVictoryPatch));
    }

    public static void Prefix()
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Fight.FightVictory.SetVictory");
        LongLiveBattleTraceRuntime.EmitBattleSummaryIfChanged("Fight.FightVictory.SetVictory");
        LongLiveBattleTraceRuntime.Log($"Fight.FightVictory.SetVictory prefix: scene={LongLiveBattleTraceRuntime.ActiveSceneName()}");
    }
}

[HarmonyPatch(typeof(Buff), "ListRealizeSeid71")]
internal static class LongLiveBattleTraceBuffListRealizeSeid71Patch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceBuffListRealizeSeid71Patch));
    }

    public static bool Prefix(Buff __instance, int seid, Avatar avatar, List<int> buffInfo, List<int> flag)
    {
        LongLiveBattleTraceRuntime.TrackBuffListRealize(__instance, seid);
        LongLiveBattleTraceRuntime.TrackDeadAvatarReentry(avatar, "Buff.ListRealizeSeid71.avatar");
        if (LongLiveBattleTraceRuntime.ShouldBlockPostDeathBattleReentry(avatar, "Buff.ListRealizeSeid71"))
        {
            return false;
        }

        LongLiveBattleTraceRuntime.Log(
            $"Buff.ListRealizeSeid71 prefix: seid={seid}, buff={LongLiveBattleTraceRuntime.DescribeBuff(__instance)}, avatar={LongLiveBattleTraceRuntime.DescribeAvatarState(avatar)}, buffInfo={LongLiveBattleTraceRuntime.DescribeIntList(buffInfo)}, flag={LongLiveBattleTraceRuntime.DescribeIntList(flag)}");
        return true;
    }
}

[HarmonyPatch(typeof(Buff), "loopRealizeSeid")]
internal static class LongLiveBattleTraceBuffLoopRealizeSeidPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceBuffLoopRealizeSeidPatch));
    }

    public static bool Prefix(Buff __instance, int seid, Entity _avatar, List<int> buffInfo, List<int> flag)
    {
        LongLiveBattleTraceRuntime.TrackBuffLoopRealize(__instance, seid);
        LongLiveBattleTraceRuntime.TrackDeadAvatarReentry(_avatar, "Buff.loopRealizeSeid.avatar");
        if (LongLiveBattleTraceRuntime.ShouldBlockPostDeathBattleReentry(_avatar, "Buff.loopRealizeSeid"))
        {
            return false;
        }

        LongLiveBattleTraceRuntime.Log(
            $"Buff.loopRealizeSeid prefix: seid={seid}, buff={LongLiveBattleTraceRuntime.DescribeBuff(__instance)}, avatar={LongLiveBattleTraceRuntime.DescribeEntity(_avatar)}, buffInfo={LongLiveBattleTraceRuntime.DescribeIntList(buffInfo)}, flag={LongLiveBattleTraceRuntime.DescribeIntList(flag)}");
        return true;
    }
}

[HarmonyPatch(typeof(Buff), "onLoopTrigger")]
internal static class LongLiveBattleTraceBuffOnLoopTriggerPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceBuffOnLoopTriggerPatch));
    }

    public static bool Prefix(Buff __instance, Entity _avatar, List<int> buffInfo, List<int> flag, BuffLoopData buffLoopData)
    {
        LongLiveBattleTraceRuntime.TrackBuffOnLoopTrigger(__instance, buffLoopData);
        LongLiveBattleTraceRuntime.TrackDeadAvatarReentry(_avatar, "Buff.onLoopTrigger.avatar");
        if (LongLiveBattleTraceRuntime.ShouldBlockPostDeathBattleReentry(_avatar, "Buff.onLoopTrigger"))
        {
            return false;
        }

        LongLiveBattleTraceRuntime.Log(
            $"Buff.onLoopTrigger prefix: buff={LongLiveBattleTraceRuntime.DescribeBuff(__instance)}, avatar={LongLiveBattleTraceRuntime.DescribeEntity(_avatar)}, buffInfo={LongLiveBattleTraceRuntime.DescribeIntList(buffInfo)}, flag={LongLiveBattleTraceRuntime.DescribeIntList(flag)}, loopData={LongLiveBattleTraceRuntime.DescribeBuffLoopData(buffLoopData)}");
        return true;
    }
}

[HarmonyPatch(typeof(Spell), "onBuffTick")]
internal static class LongLiveBattleTraceSpellOnBuffTickPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceSpellOnBuffTickPatch));
    }

    public static bool Prefix(Spell __instance, int index, List<int> flag, int type)
    {
        LongLiveBattleTraceRuntime.TrackSpellTick(flag);
        if (LongLiveBattleTraceRuntime.ShouldBlockSpellTick(flag, "Spell.onBuffTick"))
        {
            return false;
        }

        LongLiveBattleTraceRuntime.Log(
            $"Spell.onBuffTick prefix: index={index}, type={type}, spell={LongLiveBattleTraceRuntime.DescribeSpell(__instance)}, flag={LongLiveBattleTraceRuntime.DescribeIntList(flag)}");
        return true;
    }
}

[HarmonyPatch(typeof(Fungus.AvatarCheckDeath), "OnEnter")]
internal static class LongLiveBattleTraceFungusAvatarCheckDeathPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceFungusAvatarCheckDeathPatch));
    }

    public static void Prefix(Fungus.AvatarCheckDeath __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Fungus.AvatarCheckDeath.OnEnter");
        LongLiveBattleTraceRuntime.Log($"Fungus.AvatarCheckDeath.OnEnter prefix: commandType={__instance.GetType().FullName}, scene={LongLiveBattleTraceRuntime.ActiveSceneName()}");
    }
}

[HarmonyPatch(typeof(Fungus.CheckNpcDeath), "OnEnter")]
internal static class LongLiveBattleTraceFungusCheckNpcDeathPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceFungusCheckNpcDeathPatch));
    }

    public static void Prefix(Fungus.CheckNpcDeath __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Fungus.CheckNpcDeath.OnEnter");
        LongLiveBattleTraceRuntime.Log($"Fungus.CheckNpcDeath.OnEnter prefix: commandType={__instance.GetType().FullName}, scene={LongLiveBattleTraceRuntime.ActiveSceneName()}");
    }
}

[HarmonyPatch(typeof(Fungus.AvatarDeath), "OnEnter")]
internal static class LongLiveBattleTraceFungusAvatarDeathPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceFungusAvatarDeathPatch));
    }

    public static void Prefix(Fungus.AvatarDeath __instance)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("Fungus.AvatarDeath.OnEnter");
        LongLiveBattleTraceRuntime.Log($"Fungus.AvatarDeath.OnEnter prefix: commandType={__instance.GetType().FullName}, scene={LongLiveBattleTraceRuntime.ActiveSceneName()}");
    }
}

[HarmonyPatch(typeof(NPCDeath), "SetNpcDeath")]
internal static class LongLiveBattleTraceNpcDeathSetNpcDeathPatch
{
    public static bool Prepare()
    {
        return LongLiveBattleTraceRuntime.Prepare(nameof(LongLiveBattleTraceNpcDeathSetNpcDeathPatch));
    }

    public static void Prefix(int deathType, int npcId, int killNpcId, bool after)
    {
        LongLiveBattleTraceRuntime.TrackBattlePhase("NPCDeath.SetNpcDeath");
        LongLiveBattleTraceRuntime.EmitBattleSummaryIfChanged("NPCDeath.SetNpcDeath");
        LongLiveBattleTraceRuntime.Log($"NPCDeath.SetNpcDeath prefix: deathType={deathType}, npcId={npcId}, killNpcId={killNpcId}, after={after}, scene={LongLiveBattleTraceRuntime.ActiveSceneName()}");
    }
}
