using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using LongLive.BepInEx.Plugin;
using JSONClass;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveBattleTraceRuntime
{
    private static readonly HashSet<string> ReportedPatchNames = new HashSet<string>(StringComparer.Ordinal);
    private static readonly HashSet<string> ReportedTypeSnapshots = new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> NegativeHpHitCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> NegativeHpHitCountsByAvatar = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly HashSet<string> NegativeHpFirstSeen = new HashSet<string>(StringComparer.Ordinal);
    private static readonly HashSet<int> GuardedSkillIds = new HashSet<int>();
    private static readonly HashSet<int> ReportedGuardedSkillIds = new HashSet<int>();
    private static readonly HashSet<int> LethalCandidateSkillIds = new HashSet<int>();
    private static readonly Dictionary<int, string> TerminatedSkillReasons = new Dictionary<int, string>();
    private static readonly Dictionary<string, int> GuardedSkillSegmentIndexByAvatar = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> BattleEventCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> DeadAvatarReentryCountsBySource = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<int, int> DamageAttemptCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> DamageInvocationCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> SpellTickCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> SpellOnBuffTickCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> SpellOnBuffTickByTypeCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> SpellOnBuffTickByTypeCountsByType = new Dictionary<int, int>();
    private static readonly Dictionary<string, int> SpellOnBuffTickByTypeCountsBySkillType = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<int, int> SpellOnBuffTickBridgeCountsByType = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> GuardBlockedDamageCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> GuardBlockedSpellTickCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<string, int> GuardBlockedSpellTickCountsBySource = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<int, int> GuardBlockedSpellTickCountsByType = new Dictionary<int, int>();
    private static readonly Dictionary<string, int> GuardBlockedSpellTickCountsBySkillType = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> GuardBlockedDamageReasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> GuardBlockedSpellTickReasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> DamageDecisionReasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> SkillTerminationReasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<int, int> NativeLethalCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> NativeDecisionCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> NativeFallbackCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> OverflowCountsBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> OverflowAmountBySkillId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> ReentryCountsByBuffId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> ReentryCountsBySeid = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> LoopRealizeCountsByBuffId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> LoopRealizeCountsBySeid = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> ListRealizeCountsByBuffId = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> ListRealizeCountsBySeid = new Dictionary<int, int>();
    private static readonly Dictionary<string, int> BlockedDamageLogCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> BlockedSpellTickLogCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly HashSet<string> ReportedSpellSkillIdMismatches = new HashSet<string>(StringComparer.Ordinal);
    private static int BattleSequence;
    private static int TotalTrackedEvents;
    private static int LastSummaryTrackedEvents;
    private static LongLiveBattleDamagePipeline? BattleDamagePipeline;
    private static readonly string[] EntityMembers =
    {
        "HP", "Hp", "HP_Max", "_HP_Max", "MP", "Shield", "LingQi", "LingQiMax", "Dead", "IsDead", "UUID", "uuid", "Name", "name"
    };

    private static readonly string[] SkillMembers =
    {
        "skill_ID", "Skill_ID", "Id", "ID", "itemId", "uuid", "UUID", "name", "Name", "Skill_Lv", "SkillLv"
    };

    private static readonly string[] FightTempMembers =
    {
        "damage", "Damage", "hp", "HP", "mp", "MP", "shield", "Shield", "buffs", "Buffs"
    };

    private static readonly string[] StateMembers =
    {
        "state", "subState", "deathType", "LunDaoState", "HP", "HP_Max", "_HP_Max", "name", "Name"
    };

    private static readonly string[] BuffMembers =
    {
        "buffID", "seid", "NowBuffInfo", "_loopTime", "_NeiShangLoopCount"
    };

    private static readonly string[] SpellMembers =
    {
        "UseSkillLateDict"
    };

    private static readonly string[] BuffLoopDataMembers =
    {
        "loopNum", "LoopNum", "loopTime", "LoopTime", "buffID", "BuffID", "seid", "Seid", "type", "Type"
    };

    public static bool IsEnabled => LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true
        && LongLivePlugin.Instance?.Options.EnableBattleTrace.Value == true;

    public static bool IsVerbose => LongLivePlugin.Instance?.Options.EnableBattleTraceVerbose.Value == true;

    public static bool IsExperimentalGuardEnabled => LongLivePlugin.Instance?.Options.EnableExperimentalBattleGuard.Value == true;

    private static ManualLogSource? Logger => LongLivePlugin.LogSource;

    public static bool Prepare(string patchName)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (ReportedPatchNames.Add(patchName))
        {
            Log($"patch prepared: {patchName}");
        }

        return true;
    }

    public static void Log(string message)
    {
        Logger?.LogInfo($"[BattleTrace] {message}");
    }

    public static void ResetBattleState()
    {
        EmitBattleSummaryIfChanged("battle-reset");
        NegativeHpHitCounts.Clear();
        NegativeHpHitCountsByAvatar.Clear();
        NegativeHpFirstSeen.Clear();
        GuardedSkillIds.Clear();
        ReportedGuardedSkillIds.Clear();
        LethalCandidateSkillIds.Clear();
        TerminatedSkillReasons.Clear();
        GuardedSkillSegmentIndexByAvatar.Clear();
        BattleEventCounts.Clear();
        DeadAvatarReentryCountsBySource.Clear();
        DamageAttemptCountsBySkillId.Clear();
        DamageInvocationCountsBySkillId.Clear();
        SpellTickCountsBySkillId.Clear();
        SpellOnBuffTickCountsBySkillId.Clear();
        SpellOnBuffTickByTypeCountsBySkillId.Clear();
        SpellOnBuffTickByTypeCountsByType.Clear();
        SpellOnBuffTickByTypeCountsBySkillType.Clear();
        SpellOnBuffTickBridgeCountsByType.Clear();
        GuardBlockedDamageCountsBySkillId.Clear();
        GuardBlockedSpellTickCountsBySkillId.Clear();
        GuardBlockedSpellTickCountsBySource.Clear();
        GuardBlockedSpellTickCountsByType.Clear();
        GuardBlockedSpellTickCountsBySkillType.Clear();
        GuardBlockedDamageReasonCounts.Clear();
        GuardBlockedSpellTickReasonCounts.Clear();
        DamageDecisionReasonCounts.Clear();
        SkillTerminationReasonCounts.Clear();
        NativeLethalCountsBySkillId.Clear();
        NativeDecisionCountsBySkillId.Clear();
        NativeFallbackCountsBySkillId.Clear();
        OverflowCountsBySkillId.Clear();
        OverflowAmountBySkillId.Clear();
        ReentryCountsByBuffId.Clear();
        ReentryCountsBySeid.Clear();
        LoopRealizeCountsByBuffId.Clear();
        LoopRealizeCountsBySeid.Clear();
        ListRealizeCountsByBuffId.Clear();
        ListRealizeCountsBySeid.Clear();
        BlockedDamageLogCounts.Clear();
        BlockedSpellTickLogCounts.Clear();
        ReportedSpellSkillIdMismatches.Clear();
        TotalTrackedEvents = 0;
        LastSummaryTrackedEvents = 0;
        BattleSequence++;
        Log($"battle state reset: sequence={BattleSequence}, scene={ActiveSceneName()}");
    }

    public static void EmitBattleSummaryIfChanged(string reason)
    {
        if (TotalTrackedEvents == 0 || TotalTrackedEvents == LastSummaryTrackedEvents)
        {
            return;
        }

        Log($"battle summary checkpoint: reason={reason}, sequence={BattleSequence}, scene={ActiveSceneName()}, trackedEvents={TotalTrackedEvents}");
        Log($"battle summary totals: {FormatBattleEventTotals()}");

        LogTopSummary("battle summary top negative avatars", NegativeHpHitCountsByAvatar);
        LogTopSummary("battle summary top damage attempts by skillId", DamageAttemptCountsBySkillId);
        LogTopSummary("battle summary top recvDamage by skillId", DamageInvocationCountsBySkillId);
        LogTopSummary("battle summary top spell ticks by skillId", SpellTickCountsBySkillId);
        LogTopSummary("battle summary top Spell.onBuffTick skillId", SpellOnBuffTickCountsBySkillId);
        LogTopSummary("battle summary top Spell.onBuffTickByType skillId", SpellOnBuffTickByTypeCountsBySkillId);
        LogTopSummary("battle summary top Spell.onBuffTickByType type", SpellOnBuffTickByTypeCountsByType);
        LogTopSummary("battle summary top Spell.onBuffTickByType skill-type", SpellOnBuffTickByTypeCountsBySkillType);
        LogTopSummary("battle summary top Spell.ONBuffTick type", SpellOnBuffTickBridgeCountsByType);
        LogTopSummary("battle summary top blocked damage by skillId", GuardBlockedDamageCountsBySkillId);
        LogTopSummary("battle summary top blocked spell ticks by skillId", GuardBlockedSpellTickCountsBySkillId);
        LogTopSummary("battle summary blocked spell tick sources", GuardBlockedSpellTickCountsBySource);
        LogTopSummary("battle summary blocked spell tick types", GuardBlockedSpellTickCountsByType);
        LogTopSummary("battle summary blocked spell tick skill-type", GuardBlockedSpellTickCountsBySkillType);
        LogTopSummary("battle summary blocked damage reasons", GuardBlockedDamageReasonCounts);
        LogTopSummary("battle summary blocked spell tick reasons", GuardBlockedSpellTickReasonCounts);
        LogTopSummary("battle summary decision reasons", DamageDecisionReasonCounts);
        LogTopSummary("battle summary skill termination reasons", SkillTerminationReasonCounts);
        LogSkillTypeBreakdownForTopBlockedSkill("battle summary Spell.onBuffTickByType type breakdown for top blocked skillId", SpellOnBuffTickByTypeCountsBySkillType, GuardBlockedSpellTickCountsBySkillId);
        LogSkillTypeBreakdownForTopBlockedSkill("battle summary blocked spell tick type breakdown for top blocked skillId", GuardBlockedSpellTickCountsBySkillType, GuardBlockedSpellTickCountsBySkillId);
        LogTopSummary("battle summary top native lethal skillId", NativeLethalCountsBySkillId);
        LogTopSummary("battle summary top native decision skillId", NativeDecisionCountsBySkillId);
        LogTopSummary("battle summary top native fallback skillId", NativeFallbackCountsBySkillId);
        LogTopSummary("battle summary top overflow skillId", OverflowCountsBySkillId);
        LogTopSummary("battle summary top overflow amount by skillId", OverflowAmountBySkillId);
        LogTopSummary("battle summary top dead-avatar reentry sources", DeadAvatarReentryCountsBySource);
        LogTopSummary("battle summary top Buff.onLoopTrigger buffId", ReentryCountsByBuffId);
        LogTopSummary("battle summary top Buff.onLoopTrigger seid", ReentryCountsBySeid);
        LogTopSummary("battle summary top Buff.loopRealizeSeid buffId", LoopRealizeCountsByBuffId);
        LogTopSummary("battle summary top Buff.loopRealizeSeid seid", LoopRealizeCountsBySeid);
        LogTopSummary("battle summary top Buff.ListRealizeSeid71 buffId", ListRealizeCountsByBuffId);
        LogTopSummary("battle summary top Buff.ListRealizeSeid71 seid", ListRealizeCountsBySeid);

        LastSummaryTrackedEvents = TotalTrackedEvents;
    }

    public static void LogVerbose(string message)
    {
        if (!IsVerbose)
        {
            return;
        }

        Log(message);
    }

    public static string ActiveSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public static string DescribeRoundManager(RoundManager? manager)
    {
        if (manager == null)
        {
            return "roundManager=null";
        }

        return string.Format(
            "scene={0}, round={1}, playerTempHp={2}, npcTempHp={3}, virtualSkillDamage={4}, curSkill={5}, choiceSkill={6}, playerSkills={7}, npcSkills={8}",
            ActiveSceneName(),
            manager.StaticRoundNum,
            manager.PlayerTempHp,
            manager.NpcTempHp,
            manager.VirtualSkillDamage,
            DescribeObject(manager.CurSkill, SkillMembers),
            DescribeObject(manager.ChoiceSkill, SkillMembers),
            CountEnumerable(manager.PlayerUseSkillList),
            CountEnumerable(manager.NpcUseSkillList));
    }

    public static string DescribeEntity(object? entity)
    {
        return DescribeObject(entity, EntityMembers);
    }

    public static string DescribeSkill(object? skill)
    {
        return DescribeObject(skill, SkillMembers);
    }

    public static string DescribeCharacterSkillDeployer(CharacterSkillDeployer? deployer)
    {
        if (deployer == null)
        {
            return "characterSkillDeployer=null";
        }

        return string.Format(
            "type={0}, indexSkill={1}, attackingSkill={2}, skillSlots={3}",
            deployer.GetType().FullName,
            deployer.indexSkill,
            ReadFieldValue(deployer, "attackingSkill"),
            ReadFieldValue(deployer, "Skill"));
    }

    public static string DescribeAvatar(object? avatar)
    {
        return DescribeObject(avatar, EntityMembers);
    }

    public static string DescribeFightTemp(object? fightTemp)
    {
        return DescribeObject(fightTemp, FightTempMembers);
    }

    public static string DescribeAvatarState(object? avatar)
    {
        return DescribeObject(avatar, StateMembers);
    }

    public static string DescribeBuff(object? buff)
    {
        return DescribeObject(buff, BuffMembers);
    }

    public static string DescribeSpell(object? spell)
    {
        return DescribeObject(spell, SpellMembers);
    }

    public static string DescribeBuffLoopData(object? buffLoopData)
    {
        return DescribeObject(buffLoopData, BuffLoopDataMembers);
    }

    public static string DescribeIntList(IReadOnlyList<int>? values)
    {
        if (values == null)
        {
            return "null";
        }

        const int previewCount = 8;
        var take = Math.Min(values.Count, previewCount);
        var parts = new List<string>(take);
        for (var index = 0; index < take; index++)
        {
            parts.Add(values[index].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var suffix = values.Count > previewCount ? ", ..." : string.Empty;
        return '[' + string.Join(", ", parts) + suffix + $"] (count={values.Count})";
    }

    public static void TrackNegativeHpWrite(object? avatar, int requestedHp, string source)
    {
        if (requestedHp >= 0 || avatar == null)
        {
            return;
        }

        var avatarKey = BuildAvatarKey(avatar);
        IncrementCounter(BattleEventCounts, "negative-hp.write");
        IncrementCounter(NegativeHpHitCountsByAvatar, avatarKey);
        if (!NegativeHpHitCounts.TryGetValue(avatarKey, out var count))
        {
            count = 0;
        }

        count++;
        NegativeHpHitCounts[avatarKey] = count;

        if (NegativeHpFirstSeen.Add(avatarKey))
        {
            Log($"negative-hp first-hit: source={source}, count={count}, requestedHp={requestedHp}, {DescribeAvatarState(avatar)}");
            Log($"negative-hp stack: {CaptureCompactStackTrace()}\n");
            return;
        }

        if (count <= 5 || count % 10 == 0)
        {
            Log($"negative-hp repeated-hit: source={source}, count={count}, requestedHp={requestedHp}, {DescribeAvatarState(avatar)}");
        }
    }

    public static void TrackDeadTargetDamageAttempt(object? avatar, int skillId, int damage, string source)
    {
        if (avatar == null)
        {
            return;
        }

        var hp = TryGetIntMember(avatar, "HP");
        if (!hp.HasValue || hp.Value > 0)
        {
            return;
        }

        var avatarKey = BuildAvatarKey(avatar);
        NegativeHpHitCounts.TryGetValue(avatarKey, out var count);
        IncrementCounter(BattleEventCounts, "dead-target.damage-attempt");
        IncrementCounter(DamageAttemptCountsBySkillId, NormalizePositiveKey(skillId));
        Log($"dead-target damage-attempt: source={source}, skillId={skillId}, damage={damage}, priorNegativeHits={count}, {DescribeAvatarState(avatar)}");
    }

    public static void TrackDeadAvatarReentry(object? avatar, string source)
    {
        if (avatar == null)
        {
            return;
        }

        var hp = TryGetIntMember(avatar, "HP");
        if (!hp.HasValue || hp.Value > 0)
        {
            return;
        }

        NegativeHpHitCounts.TryGetValue(BuildAvatarKey(avatar), out var count);
        IncrementCounter(BattleEventCounts, "dead-avatar.reentry");
        IncrementCounter(DeadAvatarReentryCountsBySource, source);
        Log($"dead-avatar reentry: source={source}, priorNegativeHits={count}, {DescribeAvatarState(avatar)}");
    }

    public static void TrackBattlePhase(string source)
    {
        IncrementCounter(BattleEventCounts, source);
    }

    public static void TrackDamageInvocation(int skillId, string source)
    {
        IncrementCounter(BattleEventCounts, source);
        IncrementCounter(DamageInvocationCountsBySkillId, NormalizePositiveKey(skillId));
    }

    public static void TrackBuffListRealize(object? buff, int seid)
    {
        IncrementCounter(BattleEventCounts, "Buff.ListRealizeSeid71");
        IncrementCounter(ListRealizeCountsByBuffId, NormalizePositiveKey(TryReadBuffId(buff)));
        IncrementCounter(ListRealizeCountsBySeid, NormalizePositiveKey(seid));
    }

    public static void TrackBuffLoopRealize(object? buff, int seid)
    {
        IncrementCounter(BattleEventCounts, "Buff.loopRealizeSeid");
        IncrementCounter(LoopRealizeCountsByBuffId, NormalizePositiveKey(TryReadBuffId(buff)));
        IncrementCounter(LoopRealizeCountsBySeid, NormalizePositiveKey(seid));
    }

    public static void TrackBuffOnLoopTrigger(object? buff, object? buffLoopData)
    {
        IncrementCounter(BattleEventCounts, "Buff.onLoopTrigger");
        IncrementCounter(ReentryCountsByBuffId, NormalizePositiveKey(TryReadBuffId(buff)));
        IncrementCounter(ReentryCountsBySeid, NormalizePositiveKey(TryReadLoopSeid(buffLoopData) ?? TryReadBuffSeid(buff)));
    }

    public static void TrackSpellTick(IReadOnlyList<int>? flag)
    {
        IncrementCounter(BattleEventCounts, "Spell.onBuffTick");
        var normalizedSkillId = NormalizePositiveKey(TryExtractFlagSkillId(flag));
        IncrementCounter(SpellTickCountsBySkillId, normalizedSkillId);
        IncrementCounter(SpellOnBuffTickCountsBySkillId, normalizedSkillId);
    }

    public static void TrackSpellTickByType(int type, IReadOnlyList<int>? flag)
    {
        IncrementCounter(BattleEventCounts, "Spell.onBuffTickByType");
        var normalizedSkillId = NormalizePositiveKey(TryExtractFlagSkillId(flag));
        IncrementCounter(SpellTickCountsBySkillId, normalizedSkillId);
        IncrementCounter(SpellOnBuffTickByTypeCountsBySkillId, normalizedSkillId);
        var normalizedType = NormalizePositiveKey(type);
        IncrementCounter(SpellOnBuffTickByTypeCountsByType, normalizedType);
        IncrementRawCounter(SpellOnBuffTickByTypeCountsBySkillType, BuildSkillTypeKey(normalizedSkillId, normalizedType));
    }

    public static void TrackSpellOnBuffTickBridge(int type, int buffindex)
    {
        IncrementCounter(BattleEventCounts, "Spell.ONBuffTick");
        IncrementCounter(SpellOnBuffTickBridgeCountsByType, NormalizePositiveKey(type));
    }

    public static bool ShouldBlockPostDeathBattleReentry(object? avatar, string source)
    {
        if (!IsExperimentalGuardEnabled || avatar is not KBEngine.Avatar typedAvatar)
        {
            return false;
        }

        if (typedAvatar.isPlayer())
        {
            return false;
        }

        var hp = typedAvatar.HP;
        if (hp > 0)
        {
            return false;
        }

        IncrementCounter(BattleEventCounts, "battle-guard.blocked-reentry");
        Log($"battle-guard blocked post-death reentry: source={source}, {DescribeAvatarState(typedAvatar)}");
        return true;
    }

    public static bool ShouldBlockPostDeathDamage(object? avatar, string source, int skillId, int damage)
    {
        if (!IsExperimentalGuardEnabled || avatar is not KBEngine.Avatar typedAvatar)
        {
            return false;
        }

        var currentHp = typedAvatar.HP;
        var context = new LongLiveBattleDamageSegmentContext(
            typedAvatar,
            source,
            skillId,
            damage,
            0,
            currentHp,
            typedAvatar.isPlayer(),
            NextSegmentIndex(typedAvatar),
            true,
            skillId > 0 && GuardedSkillIds.Contains(skillId));

        var decision = GetBattleDamagePipeline().Evaluate(context);
        TrackDamageDecision(skillId, decision);

        if (decision.MarkSkillAsLethalCandidate && skillId > 0)
        {
            MarkLethalCandidateSkillId(skillId, source, typedAvatar, decision);
        }

        if (!decision.SkipOriginalDamageInvocation)
        {
            return false;
        }

        IncrementCounter(BattleEventCounts, "battle-guard.blocked-damage");
        IncrementCounter(GuardBlockedDamageCountsBySkillId, NormalizePositiveKey(skillId));
        var blockReason = string.IsNullOrWhiteSpace(decision.Reason) ? "unknown" : decision.Reason;
        IncrementRawCounter(GuardBlockedDamageReasonCounts, blockReason);
        if (decision.MarkSkillAsGuarded && skillId > 0)
        {
            MarkGuardedSkillId(skillId, source, typedAvatar);
        }

        if (ShouldEmitGuardLog(BlockedDamageLogCounts, BuildGuardLogKey(source, skillId, blockReason), out var damageLogCount))
        {
            Log($"battle-guard blocked post-death damage: count={damageLogCount}, source={source}, skillId={skillId}, damage={damage}, reason={blockReason}, lethal={decision.IsLethal}, overflow={decision.OverflowDamage}, predictedHp={decision.PredictedHpAfterSegment}, native={decision.NativeDecisionApplied}, {DescribeAvatarState(typedAvatar)}");
        }

        return true;
    }

    public static bool ShouldBlockSpellTick(object? spell, IReadOnlyList<int>? flag, string source, int? tickType = null)
    {
        if (!IsExperimentalGuardEnabled)
        {
            return false;
        }

        var skillId = TryResolveSpellSkillId(spell, flag, source);
        if (!skillId.HasValue)
        {
            return false;
        }

        if (!GuardedSkillIds.Contains(skillId.Value))
        {
            return false;
        }

        IncrementCounter(BattleEventCounts, "battle-guard.blocked-spell-tick");
        IncrementCounter(GuardBlockedSpellTickCountsBySkillId, NormalizePositiveKey(skillId));
        IncrementRawCounter(GuardBlockedSpellTickCountsBySource, source);
        var reason = TryGetTerminationReason(skillId.Value) ?? "post-death";
        IncrementRawCounter(GuardBlockedSpellTickReasonCounts, reason);
        if (ShouldEmitGuardLog(BlockedSpellTickLogCounts, BuildGuardLogKey(source, skillId.Value, reason), out var spellLogCount))
        {
            var typeSuffix = tickType.HasValue ? ", type=" + tickType.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
            Log($"battle-guard blocked spell tick: count={spellLogCount}, source={source}, skillId={skillId.Value}, reason={reason}{typeSuffix}, flag={DescribeIntList(flag)}");
        }

        return true;
    }

    public static bool ShouldBlockSpellTickByType(object? spell, int type, IReadOnlyList<int>? flag, string source)
    {
        var blocked = ShouldBlockSpellTick(spell, flag, source, type);
        if (!blocked)
        {
            return false;
        }

        var normalizedType = NormalizePositiveKey(type);
        IncrementRawCounter(GuardBlockedSpellTickCountsByType, normalizedType);

        var normalizedSkillId = NormalizePositiveKey(TryResolveSpellSkillId(spell, flag, source));
        IncrementRawCounter(GuardBlockedSpellTickCountsBySkillType, BuildSkillTypeKey(normalizedSkillId, normalizedType));
        return true;
    }

    public static void EnsureQuestKillProgressForCurrentBattleTarget(string source, KBEngine.Avatar? deadAvatar = null)
    {
        if (!IsExperimentalGuardEnabled)
        {
            return;
        }

        try
        {
            var plugin = LongLivePlugin.Instance;
            if (plugin == null || Tools.instance == null || PlayerEx.Player == null)
            {
                return;
            }

            if (deadAvatar != null && deadAvatar.isPlayer())
            {
                return;
            }

            var battleTargetId = Tools.instance.MonstarID;
            var expectedTargetId = GlobalValue.Get(401, "Avatar.die");
            var taskId = GlobalValue.Get(402, "NomelTaskMag.AutoNTaskSetKillAvatar 委托任务ID临时变量");
            var currentChildIndex = PlayerEx.Player.nomelTaskMag.nowChildNTask(taskId);
            var currentStepType = -1;
            var currentChildValue = -1;

            if (taskId > 0 && currentChildIndex >= 0)
            {
                var currentStep = PlayerEx.Player.nomelTaskMag.GetXiangXi(taskId, currentChildIndex);
                currentStepType = currentStep["type"].I;
                var currentChildId = PlayerEx.Player.NomelTaskJson[taskId.ToString()]["TaskChild"][currentChildIndex].I;
                currentChildValue = jsonData.instance.NTaskSuiJI[currentChildId.ToString()]["Value"].I;
            }

            if (plugin.Options.EnableDebugLogging.Value)
            {
                Log($"battle-guard quest sync probe: source={source}, battleTargetId={battleTargetId}, questTargetId={expectedTargetId}, global402={taskId}, currentChildIndex={currentChildIndex}, currentStepType={currentStepType}, currentChildValue={currentChildValue}, deadAvatar={(deadAvatar == null ? "null" : DescribeAvatarState(deadAvatar))}");
            }

            if (battleTargetId <= 0 || taskId <= 0 || currentChildIndex < 0 || currentStepType != 2 || currentChildValue <= 0)
            {
                return;
            }

            if (currentChildValue != battleTargetId && currentChildValue != expectedTargetId)
            {
                if (plugin.Options.EnableDebugLogging.Value)
                {
                    Log($"battle-guard quest sync skipped: source={source}, taskId={taskId}, battleTargetId={battleTargetId}, questTargetId={expectedTargetId}, currentChildValue={currentChildValue}, reason=child-target-mismatch");
                }

                return;
            }

            if (HasQuestKillProgress(taskId, currentChildValue))
            {
                if (plugin.Options.EnableDebugLogging.Value)
                {
                    Log($"battle-guard quest sync skipped: source={source}, taskId={taskId}, currentChildValue={currentChildValue}, battleTargetId={battleTargetId}, reason=already-recorded");
                }

                return;
            }

            PlayerEx.Player.nomelTaskMag.AutoNTaskSetKillAvatar(currentChildValue);

            if (!HasQuestKillProgress(taskId, currentChildValue))
            {
                ForceQuestKillProgress(taskId, currentChildValue);
            }

            if (plugin.Options.EnableDebugLogging.Value)
            {
                var hasProgressAfterSync = HasQuestKillProgress(taskId, currentChildValue);
                currentChildIndex = PlayerEx.Player.nomelTaskMag.nowChildNTask(taskId);
                var canFinishTask = PlayerEx.Player.nomelTaskMag.IsNTaskCanFinish(taskId);
                var taskDetail = PlayerEx.Player.nomelTaskMag.GetNTaskXiangXiData(taskId);
                var deliveryType = taskDetail?.JiaoFuType ?? -1;
                Log($"battle-guard synced quest kill progress for current battle target: source={source}, taskId={taskId}, currentChildValue={currentChildValue}, battleTargetId={battleTargetId}, questTargetId={expectedTargetId}, hasProgressAfterSync={hasProgressAfterSync}, nowChildIndex={currentChildIndex}, canFinishTask={canFinishTask}, deliveryType={deliveryType}");
                LogQuestTaskState(taskId, "battle-guard quest task-state after sync");
            }

            if (plugin.Options.EnableDebugLogging.Value && PlayerEx.Player.nomelTaskMag.IsNTaskCanFinish(taskId))
            {
                var taskDetail = PlayerEx.Player.nomelTaskMag.GetNTaskXiangXiData(taskId);
                var deliveryType = taskDetail?.JiaoFuType ?? -1;
                Log($"battle-guard quest sync completed, deferring final completion to vanilla flow: source={source}, taskId={taskId}, deliveryType={deliveryType}");
            }
        }
        catch (Exception exception)
        {
            if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
            {
                Log($"battle-guard quest kill-progress sync failed: source={source}, reason={exception.GetType().Name}: {exception.Message}");
            }
        }
    }

    public static void ForceQuestKillProgress(int taskId, int avatarId)
    {
        if (taskId <= 0 || avatarId <= 0)
        {
            return;
        }

        try
        {
            var player = PlayerEx.Player;
            var taskFlagRoot = player?.NomelTaskFlag;
            if (taskFlagRoot == null)
            {
                return;
            }

            var taskKey = taskId.ToString();
            if (!taskFlagRoot.HasField(taskKey))
            {
                taskFlagRoot[taskKey] = new JSONObject(JSONObject.Type.OBJECT);
            }

            if (!taskFlagRoot[taskKey].HasField("killAvatar"))
            {
                taskFlagRoot[taskKey]["killAvatar"] = new JSONObject(JSONObject.Type.ARRAY);
            }

            if (!taskFlagRoot[taskKey]["killAvatar"].HasItem(avatarId))
            {
                taskFlagRoot[taskKey]["killAvatar"].Add(avatarId);
            }

            if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
            {
                Log($"battle-guard force-wrote quest kill progress: taskId={taskId}, avatarId={avatarId}");
            }
        }
        catch (Exception exception)
        {
            if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
            {
                Log($"battle-guard force-write quest kill progress failed: taskId={taskId}, avatarId={avatarId}, reason={exception.GetType().Name}: {exception.Message}");
            }
        }
    }

    public static bool HasQuestKillProgress(int taskId, int avatarId)
    {
        if (taskId <= 0 || avatarId <= 0)
        {
            return false;
        }

        try
        {
            var player = PlayerEx.Player;
            var taskFlagRoot = player?.NomelTaskFlag;
            if (taskFlagRoot == null)
            {
                return false;
            }

            var taskKey = taskId.ToString();
            return taskFlagRoot.HasField(taskKey)
                && taskFlagRoot[taskKey].HasField("killAvatar")
                && taskFlagRoot[taskKey]["killAvatar"].HasItem(avatarId);
        }
        catch (Exception exception)
        {
            if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
            {
                Log($"battle-guard quest kill-progress probe failed: taskId={taskId}, avatarId={avatarId}, reason={exception.GetType().Name}: {exception.Message}");
            }

            return false;
        }
    }

    public static void LogQuestTaskState(int taskId, string source)
    {
        try
        {
            var player = PlayerEx.Player;
            if (player == null || taskId <= 0 || !player.NomelTaskJson.HasField(taskId.ToString()))
            {
                return;
            }

            var taskJson = player.NomelTaskJson[taskId.ToString()];
            var taskDetail = player.nomelTaskMag.GetNTaskXiangXiData(taskId);
            Log($"{source}: taskId={taskId}, taskTypeId={taskJson["TaskID"].I}, isStart={taskJson["IsStart"].b}, deliveryType={(taskDetail?.JiaoFuType ?? -1)}, taskName={(taskDetail?.name ?? "unknown")}");

            var taskSteps = player.nomelTaskMag.GetNTaskXiangXiList(taskId);
            for (var index = 0; index < taskSteps.Count; index++)
            {
                var step = taskSteps[index];
                var childId = player.NomelTaskJson[taskId.ToString()]["TaskChild"][index].I;
                var childJson = jsonData.instance.NTaskSuiJI[childId.ToString()];
                var childValue = childJson["Value"].I;
                var childName = childJson["name"].Str;
                var isEnded = player.nomelTaskMag.XiangXiTaskIsEnd(step, taskId, index);
                var whereChildId = -1;
                var whereChildName = "none";
                if (player.NomelTaskJson[taskId.ToString()].HasField("TaskWhereChild") && index < player.NomelTaskJson[taskId.ToString()]["TaskWhereChild"].Count)
                {
                    whereChildId = player.NomelTaskJson[taskId.ToString()]["TaskWhereChild"][index].I;
                    if (whereChildId > 0 && jsonData.instance.NTaskSuiJI.HasField(whereChildId.ToString()))
                    {
                        whereChildName = jsonData.instance.NTaskSuiJI[whereChildId.ToString()]["name"].Str;
                    }
                }

                Log($"{source}: stepIndex={index}, stepType={step["type"].I}, ended={isEnded}, childId={childId}, childValue={childValue}, childName={childName}, whereChildId={whereChildId}, whereChildName={whereChildName}, desc={step["desc"].Str}, talkId={step["talkID"].Str}, place={step["Place"].Str}");
            }

        }
        catch (Exception exception)
        {
            if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
            {
                Log($"quest task-state logging failed: taskId={taskId}, source={source}, reason={exception.GetType().Name}: {exception.Message}");
            }
        }
    }

    public static int? TryGetIntMember(object? instance, string memberName)
    {
        if (instance == null)
        {
            return null;
        }

        if (!TryReadMember(instance, memberName, out var value) || value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    public static int? TryExtractFlagSkillId(IReadOnlyList<int>? flag)
    {
        if (flag == null || flag.Count < 2)
        {
            return null;
        }

        var skillId = flag[1];
        return skillId > 0 ? skillId : null;
    }

    public static int? TryResolveSpellSkillId(object? spell, IReadOnlyList<int>? flag, string? source = null)
    {
        var flagSkillId = TryExtractFlagSkillId(flag);
        var spellSkillId = TryReadSpellSkillId(spell);

        if (flagSkillId.HasValue && spellSkillId.HasValue && flagSkillId.Value != spellSkillId.Value)
        {
            var mismatchKey = string.Concat(
                source ?? "<unknown>",
                "|flag=",
                flagSkillId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "|spell=",
                spellSkillId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (ReportedSpellSkillIdMismatches.Add(mismatchKey))
            {
                LogVerbose($"spell skillId mismatch: source={source ?? "<unknown>"}, flagSkillId={flagSkillId.Value}, spellSkillId={spellSkillId.Value}, flag={DescribeIntList(flag)}, spell={DescribeSpell(spell)}");
            }
        }

        return flagSkillId ?? spellSkillId;
    }

    public static string DescribeHpTransition(object? avatar, int? oldValue = null, int? explicitNewValue = null)
    {
        var currentHp = explicitNewValue ?? TryGetIntMember(avatar, "HP");
        var hpMax = TryGetIntMember(avatar, "HP_Max");
        var baseHpMax = TryGetIntMember(avatar, "_HP_Max");
        var name = ReadPreferredMember(avatar, "name", "Name") ?? "<unknown>";

        var delta = oldValue.HasValue && currentHp.HasValue ? currentHp.Value - oldValue.Value : (int?)null;
        return string.Format(
            "avatar={0}, hpOld={1}, hpNew={2}, hpDelta={3}, hpMax={4}, baseHpMax={5}",
            name,
            oldValue?.ToString() ?? "?",
            currentHp?.ToString() ?? "?",
            delta?.ToString() ?? "?",
            hpMax?.ToString() ?? "?",
            baseHpMax?.ToString() ?? "?");
    }

    public static void LogRoundManagerTypeSnapshot(RoundManager? manager)
    {
        if (manager == null)
        {
            return;
        }

        const string snapshotKey = "RoundManager";
        if (!ReportedTypeSnapshots.Add(snapshotKey))
        {
            return;
        }

        Log($"runtime type snapshot: roundManager={manager.GetType().FullName}, playerFightProcessor={manager.PlayerFightEventProcessor?.GetType().FullName ?? "null"}, npcFightManager={manager.newNpcFightManager?.GetType().FullName ?? "null"}, curSkillType={manager.CurSkill?.GetType().FullName ?? "null"}, choiceSkillType={manager.ChoiceSkill?.GetType().FullName ?? "null"}");

        var fields = AccessTools.GetDeclaredFields(typeof(RoundManager));
        foreach (var field in fields)
        {
            if (!IsInterestingName(field.Name))
            {
                continue;
            }

            object? value;
            try
            {
                value = field.GetValue(manager);
            }
            catch (Exception exception)
            {
                value = $"<error:{exception.GetType().Name}>";
            }

            LogVerbose($"roundManager field {field.FieldType.Name} {field.Name} = {ShortValue(value)}");
        }
    }

    private static string DescribeObject(object? value, IReadOnlyList<string> members)
    {
        if (value == null)
        {
            return "null";
        }

        var type = value.GetType();
        var parts = new List<string>();
        foreach (var memberName in members)
        {
            if (!TryReadMember(value, memberName, out var memberValue))
            {
                continue;
            }

            parts.Add(memberName + '=' + ShortValue(memberValue));
        }

        if (parts.Count == 0)
        {
            return type.FullName ?? type.Name;
        }

        return (type.FullName ?? type.Name) + '{' + string.Join(", ", parts) + '}';
    }

    private static bool TryReadMember(object instance, string memberName, out object? value)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var field = instance.GetType().GetField(memberName, Flags);
        if (field != null)
        {
            value = field.GetValue(instance);
            return true;
        }

        var property = instance.GetType().GetProperty(memberName, Flags);
        if (property != null && property.GetIndexParameters().Length == 0)
        {
            try
            {
                value = property.GetValue(instance, null);
                return true;
            }
            catch
            {
            }
        }

        value = null;
        return false;
    }

    private static int? TryReadSpellSkillId(object? spell)
    {
        if (spell == null)
        {
            return null;
        }

        return TryGetIntMember(spell, "skill_ID")
            ?? TryGetIntMember(spell, "Skill_ID")
            ?? TryGetIntMember(spell, "Id")
            ?? TryGetIntMember(spell, "ID")
            ?? TryGetIntMember(spell, "itemId");
    }

    private static string? ReadPreferredMember(object? instance, params string[] memberNames)
    {
        if (instance == null)
        {
            return null;
        }

        foreach (var memberName in memberNames)
        {
            if (TryReadMember(instance, memberName, out var value) && value != null)
            {
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static string ReadFieldValue(object instance, string fieldName)
    {
        var field = AccessTools.Field(instance.GetType(), fieldName);
        if (field == null)
        {
            return "<missing>";
        }

        try
        {
            return ShortValue(field.GetValue(instance));
        }
        catch (Exception exception)
        {
            return "<error:" + exception.GetType().Name + ">";
        }
    }

    private static string ShortValue(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string text)
        {
            return text;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            return value.GetType().Name + "(count=" + CountEnumerable(enumerable) + ')';
        }

        if (value is UnityEngine.Object unityObject)
        {
            return unityObject.name;
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? value.GetType().Name;
    }

    private static int CountEnumerable(IEnumerable? enumerable)
    {
        if (enumerable == null)
        {
            return 0;
        }

        if (enumerable is ICollection collection)
        {
            return collection.Count;
        }

        var count = 0;
        foreach (var _ in enumerable)
        {
            count++;
        }

        return count;
    }

    private static bool IsInterestingName(string name)
    {
        return name.IndexOf("hp", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("skill", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("buff", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("round", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("npc", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("fight", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string BuildAvatarKey(object avatar)
    {
        var name = ReadPreferredMember(avatar, "name", "Name") ?? avatar.GetType().Name;
        var hpMax = TryGetIntMember(avatar, "HP_Max")?.ToString() ?? "?";
        var baseHpMax = TryGetIntMember(avatar, "_HP_Max")?.ToString() ?? "?";
        return name + '|' + hpMax + '|' + baseHpMax;
    }

    private static LongLiveBattleDamagePipeline GetBattleDamagePipeline()
    {
        if (BattleDamagePipeline != null)
        {
            return BattleDamagePipeline;
        }

        BattleDamagePipeline = new LongLiveBattleDamagePipeline(new ILongLiveBattleDamageMiddleware[]
        {
            new LongLiveTerminatedSkillPathMiddleware(),
            new LongLiveAlreadyDeadDamageMiddleware(),
            new LongLiveNativeDamageAdjudicationMiddleware()
        });

        return BattleDamagePipeline;
    }

    private static int NextSegmentIndex(object avatar)
    {
        var avatarKey = BuildAvatarKey(avatar);
        if (!GuardedSkillSegmentIndexByAvatar.TryGetValue(avatarKey, out var segmentIndex))
        {
            segmentIndex = 0;
        }

        GuardedSkillSegmentIndexByAvatar[avatarKey] = segmentIndex + 1;
        return segmentIndex;
    }

    private static void MarkGuardedSkillId(int skillId, string source, KBEngine.Avatar typedAvatar)
    {
        GuardedSkillIds.Add(skillId);
        if (ReportedGuardedSkillIds.Add(skillId))
        {
            Log($"battle-guard marked skillId for spell short-circuit: skillId={skillId}, source={source}, {DescribeAvatarState(typedAvatar)}");
        }
    }

    private static void MarkLethalCandidateSkillId(int skillId, string source, KBEngine.Avatar typedAvatar, LongLiveBattleDamageSegmentDecision decision)
    {
        if (!LethalCandidateSkillIds.Add(skillId))
        {
            return;
        }

        ActivateSkillTermination(skillId, "native-lethal", source, typedAvatar, decision);

        Log($"battle-guard observed native lethal candidate: skillId={skillId}, source={source}, overflow={decision.OverflowDamage}, predictedHp={decision.PredictedHpAfterSegment}, {DescribeAvatarState(typedAvatar)}");
    }

    private static void ActivateSkillTermination(int skillId, string reason, string source, KBEngine.Avatar typedAvatar, LongLiveBattleDamageSegmentDecision decision)
    {
        GuardedSkillIds.Add(skillId);
        TerminatedSkillReasons[skillId] = reason;
        IncrementRawCounter(SkillTerminationReasonCounts, reason);

        if (!ReportedGuardedSkillIds.Add(skillId))
        {
            return;
        }

        Log($"battle-guard activated skill termination: skillId={skillId}, reason={reason}, source={source}, overflow={decision.OverflowDamage}, predictedHp={decision.PredictedHpAfterSegment}, {DescribeAvatarState(typedAvatar)}");
    }

    private static string? TryGetTerminationReason(int skillId)
    {
        if (TerminatedSkillReasons.TryGetValue(skillId, out var reason))
        {
            return reason;
        }

        return null;
    }

    private static void TrackDamageDecision(int skillId, LongLiveBattleDamageSegmentDecision decision)
    {
        IncrementCounter(BattleEventCounts, "battle-guard.decision");

        var reason = string.IsNullOrWhiteSpace(decision.Reason) ? "unknown" : decision.Reason;
        IncrementCounter(DamageDecisionReasonCounts, reason);

        var normalizedSkillId = NormalizePositiveKey(skillId);
        if (decision.NativeDecisionApplied)
        {
            IncrementCounter(BattleEventCounts, "battle-guard.native-decision");
            IncrementCounter(NativeDecisionCountsBySkillId, normalizedSkillId);
        }

        if (decision.NativeDecisionApplied && decision.IsLethal)
        {
            IncrementCounter(BattleEventCounts, "battle-guard.native-lethal");
            IncrementCounter(NativeLethalCountsBySkillId, normalizedSkillId);
        }

        if (decision.OverflowDamage > 0)
        {
            IncrementCounter(BattleEventCounts, "battle-guard.overflow");
            IncrementCounter(OverflowCountsBySkillId, normalizedSkillId);
            AddCounterValue(OverflowAmountBySkillId, normalizedSkillId, decision.OverflowDamage);
        }
    }

    public static void TrackNativeDecisionFallback(int skillId, string source, string reason)
    {
        IncrementCounter(BattleEventCounts, "battle-guard.native-fallback");
        IncrementCounter(NativeFallbackCountsBySkillId, NormalizePositiveKey(skillId));

        if (!IsVerbose)
        {
            return;
        }

        Log($"battle-guard native fallback: skillId={skillId}, source={source}, reason={reason}");
    }

    private static int NormalizePositiveKey(int? value)
    {
        return value.HasValue && value.Value > 0 ? value.Value : 0;
    }

    private static string BuildGuardLogKey(string source, int skillId, string reason)
    {
        return source + '|' + skillId.ToString(System.Globalization.CultureInfo.InvariantCulture) + '|' + reason;
    }

    private static string BuildSkillTypeKey(int skillId, int type)
    {
        return skillId.ToString(System.Globalization.CultureInfo.InvariantCulture) + '@' + type.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int? TryReadBuffId(object? buff)
    {
        return TryGetIntMember(buff, "buffID") ?? TryGetIntMember(buff, "BuffID");
    }

    private static int? TryReadBuffSeid(object? buff)
    {
        return TryGetIntMember(buff, "seid") ?? TryGetIntMember(buff, "Seid");
    }

    private static int? TryReadLoopSeid(object? buffLoopData)
    {
        return TryGetIntMember(buffLoopData, "seid") ?? TryGetIntMember(buffLoopData, "Seid");
    }

    private static void IncrementCounter(Dictionary<string, int> counts, string key)
    {
        TotalTrackedEvents++;
        if (!counts.TryGetValue(key, out var count))
        {
            count = 0;
        }

        counts[key] = count + 1;
    }

    private static void IncrementCounter(Dictionary<int, int> counts, int key)
    {
        TotalTrackedEvents++;
        if (!counts.TryGetValue(key, out var count))
        {
            count = 0;
        }

        counts[key] = count + 1;
    }

    private static void AddCounterValue(Dictionary<int, int> counts, int key, int value)
    {
        TotalTrackedEvents++;
        if (!counts.TryGetValue(key, out var count))
        {
            count = 0;
        }

        counts[key] = checked(count + value);
    }

    private static int IncrementRawCounter(Dictionary<string, int> counts, string key)
    {
        if (!counts.TryGetValue(key, out var count))
        {
            count = 0;
        }

        count++;
        counts[key] = count;
        return count;
    }

    private static int IncrementRawCounter(Dictionary<int, int> counts, int key)
    {
        if (!counts.TryGetValue(key, out var count))
        {
            count = 0;
        }

        count++;
        counts[key] = count;
        return count;
    }

    private static bool ShouldEmitGuardLog(Dictionary<string, int> counts, string key, out int count)
    {
        count = IncrementRawCounter(counts, key);
        if (count <= 3)
        {
            return true;
        }

        return count == 5 || count == 10 || count == 25 || count % 50 == 0;
    }

    private static string FormatBattleEventTotals()
    {
        var ordered = new List<KeyValuePair<string, int>>(BattleEventCounts);
        ordered.Sort((left, right) =>
        {
            var countCompare = right.Value.CompareTo(left.Value);
            return countCompare != 0 ? countCompare : string.CompareOrdinal(left.Key, right.Key);
        });

        const int maxItems = 12;
        var take = Math.Min(ordered.Count, maxItems);
        var parts = new List<string>(take);
        for (var index = 0; index < take; index++)
        {
            var pair = ordered[index];
            parts.Add(pair.Key + '=' + pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (parts.Count == 0)
        {
            return "none";
        }

        if (ordered.Count > maxItems)
        {
            parts.Add("...");
        }

        return string.Join(", ", parts);
    }

    private static void LogTopSummary(string label, Dictionary<string, int> counts)
    {
        var summary = FormatTopSummary(counts);
        if (summary == null)
        {
            return;
        }

        Log(label + ": " + summary);
    }

    private static void LogTopSummary(string label, Dictionary<int, int> counts)
    {
        var summary = FormatTopSummary(counts);
        if (summary == null)
        {
            return;
        }

        Log(label + ": " + summary);
    }

    private static void LogSkillTypeBreakdownForTopBlockedSkill(string label, Dictionary<string, int> skillTypeCounts, Dictionary<int, int> blockedSkillCounts)
    {
        if (skillTypeCounts.Count == 0 || blockedSkillCounts.Count == 0)
        {
            return;
        }

        var topSkillId = blockedSkillCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => pair.Key)
            .FirstOrDefault();

        if (topSkillId <= 0)
        {
            return;
        }

        var prefix = topSkillId.ToString(System.Globalization.CultureInfo.InvariantCulture) + '@';
        var filtered = new Dictionary<int, int>();
        foreach (var pair in skillTypeCounts)
        {
            if (!pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var typePart = pair.Key.Substring(prefix.Length);
            if (!int.TryParse(typePart, out var type))
            {
                continue;
            }

            filtered[type] = pair.Value;
        }

        var summary = FormatTopSummary(filtered, 16);
        if (summary == null)
        {
            return;
        }

        Log(label + $": skillId={topSkillId}, " + summary);
    }

    private static string? FormatTopSummary(Dictionary<string, int> counts)
    {
        if (counts.Count == 0)
        {
            return null;
        }

        var ordered = new List<KeyValuePair<string, int>>(counts);
        ordered.Sort((left, right) =>
        {
            var countCompare = right.Value.CompareTo(left.Value);
            return countCompare != 0 ? countCompare : string.CompareOrdinal(left.Key, right.Key);
        });

        const int maxItems = 5;
        var take = Math.Min(ordered.Count, maxItems);
        var parts = new List<string>(take);
        for (var index = 0; index < take; index++)
        {
            var pair = ordered[index];
            parts.Add(pair.Key + '=' + pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (ordered.Count > maxItems)
        {
            parts.Add("...");
        }

        return string.Join(", ", parts);
    }

    private static string? FormatTopSummary(Dictionary<int, int> counts, int maxItems = 5)
    {
        if (counts.Count == 0)
        {
            return null;
        }

        var ordered = new List<KeyValuePair<int, int>>(counts);
        ordered.Sort((left, right) =>
        {
            var countCompare = right.Value.CompareTo(left.Value);
            return countCompare != 0 ? countCompare : left.Key.CompareTo(right.Key);
        });

        var take = Math.Min(ordered.Count, maxItems);
        var parts = new List<string>(take);
        for (var index = 0; index < take; index++)
        {
            var pair = ordered[index];
            var keyLabel = pair.Key == 0 ? "unknown" : pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture);
            parts.Add(keyLabel + '=' + pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (ordered.Count > maxItems)
        {
            parts.Add("...");
        }

        return string.Join(", ", parts);
    }

    private static string CaptureCompactStackTrace()
    {
        var trace = new StackTrace(2, false);
        var frames = trace.GetFrames();
        if (frames == null || frames.Length == 0)
        {
            return "<no-stack>";
        }

        var parts = new List<string>();
        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method == null)
            {
                continue;
            }

            var declaringType = method.DeclaringType?.FullName ?? "<global>";
            if (declaringType.StartsWith("LongLive.", StringComparison.Ordinal))
            {
                continue;
            }

            parts.Add(declaringType + '.' + method.Name);
            if (parts.Count >= 8)
            {
                break;
            }
        }

        return parts.Count == 0 ? "<filtered-stack>" : string.Join(" <= ", parts);
    }
}
