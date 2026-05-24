using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_ISSEN — Final Strike. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/finalstrike.cpp</c>.
/// Renewal: misc-type hit + drop caster HP to 1% of max + end SC_NEN
/// and SC_HIDING, then slide caster behind target via
/// skill_check_unit_movepos.
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
        // Renewal order: slide first so the hit lands from the new position.
        // rAthena moves 2 cells past the target along the caster→target axis.
        var dx = Math.Sign(target.X - src.X) * 2;
        var dy = Math.Sign(target.Y - src.Y) * 2;
        ctx.UnitOps?.CheckUnitMovePos(src, (short)(target.X + dx), (short)(target.Y + dy), 1);
        (_skillAttack ?? ctx.SkillAttack)?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
        if (src is PlayerEntity p)
        {
            p.Hp = Math.Max(p.MaxHp / 100, 1);
        }
        ctx.Sc?.End(src, StatusType.Nen);
        ctx.Sc?.End(src, StatusType.Hiding);
    }
}
