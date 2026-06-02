using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_PHANTOMTHRUST — Rune Knight Phantom Thrust (skill.cpp:11216).
/// Ratio: <c>baseRatio + (-100 + 50*lv) + 10*pc_checkskill(KN_SPEARMASTERY)</c>.
/// After damage, pulls the target one cell toward the caster — rAthena
/// <c>skill_blown(src, target, 5, -1, BLOWN_NONE)</c>; we approximate
/// the rAthena <c>map_calc_dir</c> by stepping the target one cell
/// along the (target → src) unit vector via <see cref="IUnitOpsService.BlownBy"/>.
/// </summary>
public sealed class PhantomThrust : WeaponSkillImpl
{
    public PhantomThrust() : base(SkillIds.RK_PHANTOMTHRUST) { }

    // COMBAT-14: RK_PHANTOMTHRUST uses RE_LVL_DMOD(150) (battle.cpp:5217).
    protected override int ReLvlDivisor => 150;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var spearLv = src is PlayerEntity pc ? pc.LearnedSkills.GetValueOrDefault(SkillIds.KN_SPEARMASTERY) : 0;
        return baseRatio + (-100 + 50 * skillLevel) + 10 * spearLv;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
        // Pull target toward caster: direction from target → src.
        // rAthena 8-dir encoding (DIR_N=0, NE=1, E=2, SE=3, S=4, SW=5, W=6, NW=7).
        var dx = Math.Sign(src.X - target.X);
        var dy = Math.Sign(src.Y - target.Y);
        var dir = DirectionFromDelta(dx, dy);
        if (dir >= 0)
        {
            ctx.UnitOps?.BlownBy(target, dir, count: 1);
        }
    }

    private static int DirectionFromDelta(int dx, int dy) => (dx, dy) switch
    {
        (0, 1)   => 0, // N
        (1, 1)   => 1, // NE
        (1, 0)   => 2, // E
        (1, -1)  => 3, // SE
        (0, -1)  => 4, // S
        (-1, -1) => 5, // SW
        (-1, 0)  => 6, // W
        (-1, 1)  => 7, // NW
        _        => -1,
    };
}
