using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_SPEARSTAB — Knight Spear Stab. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/spearstab.cpp</c>.
/// Ratio <c>+20*lv</c>. Hits the line of enemies between caster and
/// target (knockback after the last hit). Line splash + knockback are
/// TODO.
/// </summary>
public sealed class SpearStab : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SpearStab() : base(SkillIds.KN_SPEARSTAB) { }

    public SpearStab(ISkillAttackService? skillAttack = null) : base(SkillIds.KN_SPEARSTAB)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 20 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
