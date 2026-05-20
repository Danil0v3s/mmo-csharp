using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Warlock;

/// <summary>
/// WL_CHAINLIGHTNING — Warlock Chain Lightning. Mirrors
/// <c>rathena-fork/src/map/skills/warlock/chainlightning.cpp</c>.
///
/// Wind magic that bounces from target to nearby enemies for
/// up to 7 hits total. Each hit deals (100 + 100*lv)% MATK; later
/// hits in the chain take a small damage falloff (10 % per bounce
/// in rAthena).
///
/// First-port approximation: hits the primary target then
/// enumerates radius-5 nearby victims (up to 7 total) with
/// decreasing damage per bounce.
/// </summary>
public sealed class ChainLightning : SkillImpl
{
    private const int MaxChainHits = 7;
    private const short ChainRadius = 5;

    public ChainLightning() : base(SkillIds.WL_CHAINLIGHTNING) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 100 + 100 * skillLevel;
        var baseDamage = Math.Max(1, matk * rate / 100);

        // Primary hit at full damage.
        ctx.Damage.ApplyDamage(target, baseDamage, src);

        // Bounce chain — enumerate radius-5 victims around the primary
        // target; each successive hit falls off by 10 %.
        var nearby = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            ChainRadius, EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id && v.Id != target.Id)
            .Take(MaxChainHits - 1)
            .ToList();
        var current = baseDamage;
        foreach (var v in nearby)
        {
            current = Math.Max(1, current * 90 / 100);
            ctx.Damage.ApplyDamage(v, current, src);
        }
    }
}
