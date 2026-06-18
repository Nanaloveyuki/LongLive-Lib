namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveBattleDamageSegmentContext
{
    public LongLiveBattleDamageSegmentContext(
        object targetAvatar,
        string source,
        int skillId,
        int incomingDamage,
        int damageType,
        int currentHp,
        bool isPlayerTarget,
        int segmentIndex,
        bool isMultiHit,
        bool isSkillPathTerminated)
    {
        TargetAvatar = targetAvatar;
        Source = source;
        SkillId = skillId;
        IncomingDamage = incomingDamage;
        DamageType = damageType;
        CurrentHp = currentHp;
        IsPlayerTarget = isPlayerTarget;
        SegmentIndex = segmentIndex;
        IsMultiHit = isMultiHit;
        IsSkillPathTerminated = isSkillPathTerminated;
    }

    public object TargetAvatar { get; }

    public string Source { get; }

    public int SkillId { get; }

    public int IncomingDamage { get; }

    public int DamageType { get; }

    public int CurrentHp { get; }

    public bool IsPlayerTarget { get; }

    public int SegmentIndex { get; }

    public bool IsMultiHit { get; }

    public bool IsSkillPathTerminated { get; }
}
