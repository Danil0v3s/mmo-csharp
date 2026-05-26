using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CROSSIMPACT — Cross Impact. Manual port of
/// <c>rathena-fork/src/map/skills/thief/crossimpact.cpp</c>.
/// Ratio <c>+(-100 + 1400 + 150*lv)</c>. Before the swing the caster
/// teleports to the cell behind the target (rAthena
/// <c>skill_check_unit_movepos</c> + <c>clif_blown</c>); if no
/// walkable cell exists the cast fails silently and no damage is dealt.
/// </summary>
public sealed class CrossImpact : WeaponSkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    public CrossImpact() : base(SkillIds.GC_CROSSIMPACT) { }

    public CrossImpact(IUnitOpsService? unitOps = null) : base(SkillIds.GC_CROSSIMPACT)
    {
        _unitOps = unitOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1400 + 150 * skillLevel);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_unitOps != null && !_unitOps.CheckUnitMovePos(src, target.X, target.Y, easy: 1))
            return;
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
