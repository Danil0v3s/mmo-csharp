using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// WZ_FROSTNOVA (id 88) — Wizard Frost Nova. rAthena
/// <c>skill.cpp:case WZ_FROSTNOVA</c>: caster-centered Water magic
/// AoE — every enemy within radius takes damage and a freeze proc.
/// Damage = (100 + 100 * lv)% MATK; freeze chance scales with level.
///
/// Radius = 5 cells around the caster.
/// </summary>
public sealed class FrostNovaBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.WZ_FROSTNOVA;

    private const short Radius = 5;

    private readonly Random _rng;
    public FrostNovaBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 100 + 100 * skillLevel;
        var perVictim = Math.Max(1, matk * rate / 100);

        // Freeze chance = 20 + 10*lv % (rAthena).
        var freezeChance = 20 + 10 * skillLevel;

        var victims = ctx.Entities.ForEachInRange(
            source.MapId, source.X, source.Y, Radius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == source.Id) continue;
            ctx.Damage.ApplyDamage(v, perVictim, source);
            if (ctx.Sc != null && _rng.Next(100) < freezeChance)
            {
                ctx.Sc.Start(v, StatusType.Freeze, val1: 1, 0, 0, 0,
                    durationMs: 3_000, source);
            }
        }
        return true;
    }
}
