using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_SHIMIRU — Infiltrate. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/infiltrate.cpp</c>.
/// Ratio <c>+(-100 + 700*lv) + 5*con</c>. Slide-behind + skill-unit
/// relocation logic is TODO; this only resolves the hit + self SC.
/// </summary>
public sealed class Infiltrate : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Infiltrate() : base(SkillIds.SS_SHIMIRU) { }

    public Infiltrate(ISkillAttackService? skillAttack = null) : base(SkillIds.SS_SHIMIRU)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 700 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
