using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveBattleTraceRuntime
{
    private static readonly HashSet<string> ReportedPatchNames = new HashSet<string>(StringComparer.Ordinal);
    private static readonly HashSet<string> ReportedTypeSnapshots = new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> NegativeHpHitCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly HashSet<string> NegativeHpFirstSeen = new HashSet<string>(StringComparer.Ordinal);
    private static readonly HashSet<int> GuardedSkillIds = new HashSet<int>();
    private static readonly HashSet<int> ReportedGuardedSkillIds = new HashSet<int>();
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
        NegativeHpHitCounts.Clear();
        NegativeHpFirstSeen.Clear();
        GuardedSkillIds.Clear();
        ReportedGuardedSkillIds.Clear();
        Log("battle state reset");
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
        Log($"dead-avatar reentry: source={source}, priorNegativeHits={count}, {DescribeAvatarState(avatar)}");
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

        Log($"battle-guard blocked post-death reentry: source={source}, {DescribeAvatarState(typedAvatar)}");
        return true;
    }

    public static bool ShouldBlockPostDeathDamage(object? avatar, string source, int skillId, int damage)
    {
        if (!IsExperimentalGuardEnabled || avatar is not KBEngine.Avatar typedAvatar)
        {
            return false;
        }

        if (typedAvatar.isPlayer() || typedAvatar.HP > 0)
        {
            return false;
        }

        if (skillId > 0)
        {
            GuardedSkillIds.Add(skillId);
            if (ReportedGuardedSkillIds.Add(skillId))
            {
                Log($"battle-guard marked skillId for spell short-circuit: skillId={skillId}, source={source}, {DescribeAvatarState(typedAvatar)}");
            }
        }

        Log($"battle-guard blocked post-death damage: source={source}, skillId={skillId}, damage={damage}, {DescribeAvatarState(typedAvatar)}");
        return true;
    }

    public static bool ShouldBlockSpellTick(IReadOnlyList<int>? flag, string source)
    {
        if (!IsExperimentalGuardEnabled)
        {
            return false;
        }

        var skillId = TryExtractFlagSkillId(flag);
        if (!skillId.HasValue || !GuardedSkillIds.Contains(skillId.Value))
        {
            return false;
        }

        Log($"battle-guard blocked spell tick: source={source}, skillId={skillId.Value}, flag={DescribeIntList(flag)}");
        return true;
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
