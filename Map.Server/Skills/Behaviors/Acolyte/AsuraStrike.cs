using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_EXTREMITYFIST — Monk/Champion Asura Strike. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/asurastrike.cpp</c>.
///
/// <para>The "all-in" weapon hit. Standard weapon damage pipeline,
/// then the post-hit ritual:</para>
/// <list type="number">
///   <item>Set caster SP to 0.</item>
///   <item>Apply <see cref="StatusType.Extremityfist"/> on the caster
///         (blocks SP regen for the SC's duration).</item>
///   <item>End <see cref="StatusType.Explosionspirits"/> and
///         <see cref="StatusType.Bladestop"/> on the caster.</item>
///   <item>Move the caster 3 cells past the target along the attack
///         vector, broadcasting the slide.</item>
/// </list>
///
/// <para>Ratio: <c>+700 + 10 * casterSP</c>, capped at 500 000 to
/// avoid integer overflow. Renewal: +100 % if the caster had more
/// than 5 spirit spheres at cast time (flag bit set upstream).</para>
/// </summary>
public sealed class AsuraStrike : WeaponSkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    public AsuraStrike() : base(SkillIds.MO_EXTREMITYFIST) { }

    public AsuraStrike(IUnitOpsService? unitOps = null) : base(SkillIds.MO_EXTREMITYFIST)
    {
        _unitOps = unitOps;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // COMBAT-13: rAthena sets wd->miscflag bit 1 when the caster had MORE
        // than 5 spirit spheres at cast time (skill.cpp Asura handling reads
        // sd->spiritball_old > 5); the renewal ratio then doubles. Spirit
        // spheres aren't consumed by the C# cast path yet (SkillRequirementService
        // spends only HP/SP/AP — spirit-ball consumption is SKILL-19), so the
        // live SpiritBall count IS the pre-cast value. Thread it as the miscflag
        // so the ratio override applies the ×2 before the 500000 cap.
        var spheres = src is PlayerEntity sphereOwner ? sphereOwner.SpiritBall : 0;
        int miscflag = spheres > 5 ? 1 : 0;

        // Standard weapon damage pipeline (miscflag carries the >5-sphere bit;
        // SP is still the pre-cast value here — it is zeroed below, after the
        // damage/ratio has been resolved).
        base.CastendDamageId(src, target, skillLevel, ctx, miscflag);

        // rAthena: status_set_sp(src, 0, 0);
        if (src is PlayerEntity sd) sd.Sp = 0;

        // rAthena: sc_start(SC_EXTREMITYFIST, ..., skill_get_time(...))
        // Duration baseline 60s per skill_db Duration1.
        ctx.Sc?.Start(src, StatusType.Extremityfist,
            val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);

        // rAthena: end SC_EXPLOSIONSPIRITS and SC_BLADESTOP on caster.
        ctx.Sc?.End(src, StatusType.Explosionspirits);
        ctx.Sc?.End(src, StatusType.Bladestop);

        // Direction → 3-cell shove. rAthena uses map_calc_dir + dirx/diry;
        // we approximate by stepping toward the target's position vector.
        int dx = Math.Sign(target.X - src.X);
        int dy = Math.Sign(target.Y - src.Y);
        var moved = _unitOps?.MovePos(src,
            (short)(src.X + dx * 3), (short)(src.Y + dy * 3),
            easy: 1, checkColl: true) ?? false;
        if (moved)
        {
            // The slide broadcast happens inside MovePos via clif_blown in
            // rAthena; our UnitOpsService.MovePos doesn't yet emit ZC_HIGHJUMP
            // — the dash-slide broadcast is tracked in SKILL-18.
        }
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        // No-ctx / funnel path: no >5-sphere miscflag available, so no ×2.
        => ComputeAsuraRatio(baseRatio, src, miscflag: 0);

    /// <summary>
    /// COMBAT-13 — miscflag-aware ratio. Bit 1 of <paramref name="miscflag"/>
    /// is the renewal "&gt;5 spirit spheres at cast" flag (set by
    /// <see cref="CastendDamageId"/>); when present the whole ratio doubles
    /// before the 500000 cap (battle.cpp:4843).
    /// </summary>
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
        => ComputeAsuraRatio(baseRatio, src, miscflag);

    private static int ComputeAsuraRatio(int baseRatio, Entity src, int miscflag)
    {
        // rAthena: base_skillratio += 700 + sstatus->sp * 10;
        var sp = src is PlayerEntity sd ? sd.Sp : (int)src.Stats.MaxSp;
        long ratio = baseRatio + 700 + (long)sp * 10;
        // RENEWAL (battle.cpp:4845): if (wd->miscflag & 1) skillratio *= 2;
        // — the caster had more than 5 spirit spheres at cast time.
        if ((miscflag & 1) != 0) ratio *= 2;
        // rAthena cap: min(500000, ratio) — applied AFTER the ×2.
        if (ratio > 500_000) ratio = 500_000;
        return (int)ratio;
    }

    /// <summary>
    /// rAthena <c>battle_calc_skill_constant_addition</c> MO_EXTREMITYFIST arm
    /// (battle.cpp:6616): a flat <c>250 + 150*skill_lv</c> added after the
    /// ratio. Previously missing — Asura damage was short by exactly this term.
    /// (The renewal ×2-when-&gt;5-spirit-spheres ratio bump is COMBAT-13.)
    /// </summary>
    public override long CalculateSkillConstantAddition(Entity src, Entity target, ushort skillLevel)
        => 250 + 150 * skillLevel;
}
