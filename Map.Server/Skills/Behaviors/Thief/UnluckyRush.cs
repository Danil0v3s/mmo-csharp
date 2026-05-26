using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_UNLUCKY_RUSH — Unlucky Rush. Manual port of
/// <c>rathena-fork/src/map/skills/thief/unluckyrush.cpp</c>.
/// Caster slides to the target cell then swings. Ratio
/// <c>+(-100 + 100 + 300*lv) + 5*pow</c>; <c>+2500*lv</c> under
/// SC_CHASING. On hit applies SC_HANDICAPSTATE_MISFORTUNE at
/// <c>30 + 10*lv</c>%.
/// </summary>
public sealed class UnluckyRush : WeaponSkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    public UnluckyRush() : base(SkillIds.ABC_UNLUCKY_RUSH) { }

    public UnluckyRush(IUnitOpsService? unitOps = null) : base(SkillIds.ABC_UNLUCKY_RUSH)
    {
        _unitOps = unitOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 100 + 300 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.Chasing) != null)
            ratio += 2500 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_check_unit_movepos(5, src, target->x, target->y, 0, 1)
        // → slide to the target's cell; on success skill_blown 1 step
        // in the opposite direction. CheckUnitMovePos handles the
        // walkable-cell pick; we drop the second 1-step blow since
        // the slide already places the caster adjacent.
        _unitOps?.CheckUnitMovePos(src, target.X, target.Y, easy: 0);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start(src, target, SC_HANDICAPSTATE_MISFORTUNE,
        //   30 + 10 * skill_lv, skill_lv, skill_get_time(...)).
        if (System.Random.Shared.Next(100) < 30 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.HandicapstateMisfortune,
                val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
