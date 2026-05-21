using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_ISSEN — Final Strike. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/finalstrike.cpp</c>.
/// Renewal: misc-type hit + drop caster HP to 1% of max + end SC_NEN
/// and SC_HIDING, then slide caster behind target. Slide / blown is
/// TODO (no skill_check_unit_movepos analog yet).
/// </summary>
public sealed class FinalStrike : WeaponSkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public FinalStrike() : base(SkillIds.NJ_ISSEN) { }

    public FinalStrike(ISkillAttackService? skillAttack = null) : base(SkillIds.NJ_ISSEN)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
        if (src is PlayerEntity p)
        {
            p.Hp = Math.Max(p.MaxHp / 100, 1);
        }
        ctx.Sc?.End(src, StatusType.Nen);
        ctx.Sc?.End(src, StatusType.Hiding);
        // TODO: slide caster behind target (skill_check_unit_movepos + clif_blown + clif_spiritball).
    }
}
