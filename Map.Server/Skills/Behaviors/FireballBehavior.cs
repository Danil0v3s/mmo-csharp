using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_FIREBALL (id 17) — Mage Fireball. rAthena
/// <c>skill.cpp:case MG_FIREBALL</c>: Fire-element magic, 2-cell
/// splash around the target. Full damage on the primary, half on
/// each splash victim. Damage rate = (50 + 70 * lv)% MATK.
/// </summary>
public sealed class FireballBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_FIREBALL;

    private const short SplashRadius = 2; // 5×5 area.

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 50 + 70 * skillLevel;
        var primaryDmg = Math.Max(1, matk * rate / 100);

        // Primary hit.
        ctx.Damage.ApplyDamage(target, primaryDmg, source);

        // Splash victims take half damage.
        var splashDmg = Math.Max(1, primaryDmg / 2);
        var victims = ctx.Entities.ForEachInRange(
            target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == source.Id || v.Id == target.Id) continue;
            ctx.Damage.ApplyDamage(v, splashDmg, source);
        }
        return true;
    }
}
