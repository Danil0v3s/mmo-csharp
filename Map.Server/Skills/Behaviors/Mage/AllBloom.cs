using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_ALL_BLOOM — Arch Mage All Bloom. Manual port of
/// <c>rathena-fork/src/map/skills/mage/allbloom.cpp</c>.
///
/// <para>Drops a Flower Garden ground unit then spawns rose-bud
/// sub-units (AG_ALL_BLOOM_ATK) at random splash cells, staggered by
/// the unit interval. SC_CLIMAX modulation:
/// <list type="bullet">
///   <item>climax_lv = 1 — buds spawn at double speed (½ interval, ½ duration).</item>
///   <item>climax_lv = 2 — spawn a second bud per stagger.</item>
///   <item>climax_lv = 4 — inflict SC on enemies in range instead of damaging.</item>
///   <item>climax_lv = 5 — extra finishing-hit (AG_ALL_BLOOM_ATK2 unit).</item>
/// </list>
/// </para>
/// </summary>
public sealed class AllBloom : SkillImpl
{
    public AllBloom() : base(SkillIds.AG_ALL_BLOOM) { }

    private const short DefaultArea = 2;
    private const int UnitTimeMs = 6_000;
    private const int UnitIntervalMs = 500;
    private const int ClimaxBloomDurationMs = 30_000;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena allbloom.cpp:14 — sc_start(SC_CLIMAX_BLOOM).
        ctx.Sc?.Start(target, StatusType.ClimaxBloom, val1: skillLevel, 0, 0, 0, durationMs: ClimaxBloomDurationMs, src);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var climaxLv = 0;
        var climaxSce = ctx.Sc?.Get(src, StatusType.Climax);
        if (climaxSce != null) climaxLv = climaxSce.Val1;

        var area = DefaultArea;
        var unitTime = UnitTimeMs;
        var unitInterval = UnitIntervalMs;

        // climax_lv 1 — rose buds spawn at double the speed (½ duration + ½ interval).
        if (climaxLv == 1)
        {
            unitTime /= 2;
            unitInterval /= 2;
        }

        // Primary flower-garden ground unit (visual + tick base).
        ctx.Units?.Place(src, SkillId, skillLevel, x, y);

        // climax_lv 4 — no damage, inflict SC on enemies in splash.
        if (climaxLv == 4)
        {
            if (ctx.Sc != null)
            {
                var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, area,
                    EntityType.Mob | EntityType.Pc);
                foreach (var v in victims)
                {
                    if (v.Id == src.Id) continue;
                    // SC_BLOOMING — climax-4 status. Stun fallback until
                    // the dedicated enum row lands.
                    ctx.Sc.Start(v, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
                }
            }
            return;
        }

        var iterations = unitTime / unitInterval;
        var rng = Random.Shared;
        for (var i = 1; i <= iterations; i++)
        {
            SpawnBud(src, x, y, skillLevel, area, ctx, i, rng, unitInterval, SkillIds.AG_ALL_BLOOM_ATK);
            if (climaxLv == 2)
            {
                // climax lv 2 — spawn a second rose bud alongside the first.
                SpawnBud(src, x, y, skillLevel, area, ctx, i, rng, unitInterval, SkillIds.AG_ALL_BLOOM_ATK);
            }
        }

        // climax_lv 5 — drop a finishing AG_ALL_BLOOM_ATK2 hit at the
        // end of the cycle on the cast center cell.
        if (climaxLv == 5)
        {
            ctx.Units?.Place(src, SkillIds.AG_ALL_BLOOM_ATK2, skillLevel, x, y,
                delayMs: unitTime);
        }
    }

    private static void SpawnBud(Entity src, short cx, short cy, ushort skillLevel,
        short area, SkillBehaviorContext ctx, int stagger, Random rng, int interval, ushort subSkillId)
    {
        var tmpX = (short)(cx - area + rng.Next(area * 2 + 1));
        var tmpY = (short)(cy - area + rng.Next(area * 2 + 1));
        ctx.Units?.Place(src, subSkillId, skillLevel, tmpX, tmpY,
            delayMs: stagger * interval);
    }
}
