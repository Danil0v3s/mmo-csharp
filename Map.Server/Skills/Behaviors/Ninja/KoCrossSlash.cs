using System;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_JYUMONJIKIRI — Cross Slash. Manual port of the rAthena KO_JYUMONJIKIRI arms.
/// <list type="bullet">
///   <item>ratio <c>+(-100 + 200*lv)</c> with RE_LVL_DMOD(120) (battle.cpp:5639);</item>
///   <item><c>+lv*srcBaseLv</c> ratio bonus (added AFTER the macro) when the target
///         already carries SC_JYUMONJIKIRI — COMBAT-57;</item>
///   <item>double-hit (skill_db <c>HitCount: -2</c> → div 2, via
///         <see cref="SkillHitCounts"/>, COMBAT-39);</item>
///   <item>position-shift: the caster slides next to the target before striking
///         (skill.cpp:7128) — COMBAT-57.</item>
/// </list>
/// </summary>
public sealed class KoCrossSlash : WeaponSkillImpl
{
    public KoCrossSlash() : base(SkillIds.KO_JYUMONJIKIRI) { }

    // COMBAT-35 — RE_LVL_DMOD(120) (battle.cpp:5639). ComputeSkillDamage applies
    // this divisor to the base ratio above base level 99.
    protected override int ReLvlDivisor => 120;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel);

    // COMBAT-57 — rAthena adds `skill_lv * status_get_lv(src)` to the ratio AFTER
    // RE_LVL_DMOD when the target already carries SC_JYUMONJIKIRI (a re-hit). Applied
    // via the post-DMOD hook so the bonus is NOT scaled by the macro.
    protected override int CalculateSkillRatioPostDmod(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext? ctx)
        => ctx?.Sc?.Get(target, StatusType.Jyumonjikiri) != null ? skillLevel * src.Level : 0;

    // COMBAT-57 — position shift: the caster repositions to a cell offset from the
    // target (by the caster→target direction), slide-broadcasts (clif_blown), then
    // strikes (skill.cpp:7128). If the move is blocked, no strike. With UnitOps
    // unwired (tests) the strike still lands in place.
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var dir = RaDir(src, target);
        short x = (dir > 0 && dir < 4) ? (short)2 : dir > 4 ? (short)-2 : (short)0;
        short y = (dir > 2 && dir < 6) ? (short)2 : (dir == 7 || dir < 2) ? (short)-2 : (short)0;

        var moved = ctx.UnitOps?.MovePos(src, (short)(target.X + x), (short)(target.Y + y), easy: 1, checkColl: true);
        if (moved == false) return; // rAthena: unit_movepos failed → no strike

        base.CastendDamageId(src, target, skillLevel, ctx);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Jyumonjikiri, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);

    /// <summary>rAthena <c>map_calc_dir</c> convention (path.hpp: 0=N, 2=W, 4=S, 6=E,
    /// counter-clockwise), sign-based — matches the offset switch above.</summary>
    private static int RaDir(Entity src, Entity target) =>
        (Math.Sign(target.X - src.X), Math.Sign(target.Y - src.Y)) switch
        {
            (0, 1) => 0,    // N
            (-1, 1) => 1,   // NW
            (-1, 0) => 2,   // W
            (-1, -1) => 3,  // SW
            (0, -1) => 4,   // S
            (1, -1) => 5,   // SE
            (1, 0) => 6,    // E
            (1, 1) => 7,    // NE
            _ => 0,         // same cell → treat as N
        };
}
