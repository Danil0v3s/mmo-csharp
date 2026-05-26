using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_DARKILLUSION — Dark Illusion. Manual port of
/// <c>rathena-fork/src/map/skills/thief/darkillusion.cpp</c>.
/// Caster slides 2 cells in the direction of the target, swings, and
/// at <c>4*lv</c>% chains a GC_CROSSIMPACT follow-up. If the slide
/// fails the cast is a no-op.
/// </summary>
public sealed class DarkIllusion : WeaponSkillImpl
{
    private readonly IUnitOpsService? _unitOps;
    private readonly ISkillAttackService? _skillAttack;

    public DarkIllusion() : base(SkillIds.GC_DARKILLUSION) { }

    public DarkIllusion(IUnitOpsService? unitOps = null, ISkillAttackService? skillAttack = null)
        : base(SkillIds.GC_DARKILLUSION)
    {
        _unitOps = unitOps;
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_unitOps != null && !_unitOps.CheckUnitMovePos(src, target.X, target.Y, easy: 1))
            return;
        base.CastendDamageId(src, target, skillLevel, ctx);

        // Chain GC_CROSSIMPACT at 4 * skill_lv %.
        if (System.Random.Shared.Next(100) < 4 * skillLevel)
            _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target,
                SkillIds.GC_CROSSIMPACT, skillLevel);
    }
}
