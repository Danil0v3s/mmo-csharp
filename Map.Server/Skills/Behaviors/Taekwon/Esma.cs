using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SL_SMA — Esma. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/esma.cpp</c>.
/// Ratio <c>+(-60 + srcLv)</c>. Ends SC_SMA on cast. Player-target
/// gating + SC_STUN punish are TODO.
/// </summary>
public sealed class Esma : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Esma() : base(SkillIds.SL_SMA) { }

    public Esma(ISkillAttackService? skillAttack = null) : base(SkillIds.SL_SMA)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-60 + src.Level);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
