using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGMA_ERUPTION — Mechanic Magma Eruption. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/magmaeruption.cpp</c>.
/// Ground-targeted slam: ratio <c>+(350 + 50*lv)</c> on every enemy in
/// the splash radius (3 cells), with on-hit SC_STUN at 90 %. Two amotion
/// ticks later, places the NC_MAGMA_ERUPTION_DOTDAMAGE follow-up ground
/// unit (rAthena <c>skill_addtimerskill(tick + amotion*2)</c>); the unit
/// applies SC_BURNING in its tick path.
///
/// <para>NC_MAGMA_ERUPTION_DOTDAMAGE — 🚩 INFRA-DEFERRED: id 5015 is not
/// yet registered in <see cref="SkillIds"/> (additions to that file are
/// out of scope for this batch — touched by another agent in flight).
/// The eruption ground unit will arrive when SkillIds.NC_MAGMA_ERUPTION_DOTDAMAGE
/// lands.</para>
/// </summary>
public sealed class MagmaEruption : WeaponSkillImpl
{
    private const short SplashRange = 3;
    private readonly Random _rng;

    public MagmaEruption() : base(SkillIds.NC_MAGMA_ERUPTION) => _rng = Random.Shared;

    public MagmaEruption(Random? rng = null) : base(SkillIds.NC_MAGMA_ERUPTION) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 350 + 50 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 90)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // AoE slam damage on each char in range (deferred ground unit TODO above).
        var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, SplashRange,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            CastendDamageId(src, v, skillLevel, ctx);
        }
    }
}
