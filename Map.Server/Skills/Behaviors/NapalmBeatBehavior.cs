using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_NAPALMBEAT (id 11) — Mage Napalm Beat. rAthena
/// <c>skill.cpp:case MG_NAPALMBEAT</c>: Ghost-element magic AoE, 3×3
/// splash on the target cell. Per-hit damage = (70 + 10 * lv)% MATK,
/// split evenly across all victims in radius (rAthena "split damage"
/// flag — total stays constant, more victims means less per-target).
/// </summary>
public sealed class NapalmBeatBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_NAPALMBEAT;

    private const short SplashRadius = 1; // 3×3 area.

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var victims = ctx.Entities.ForEachInRange(
            target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        // Exclude the caster from victim count.
        var realVictims = victims.Where(v => v.Id != source.Id).ToList();
        if (realVictims.Count == 0) return true;

        var totalMatk = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        var rate = 70 + 10 * skillLevel;
        var totalDamage = Math.Max(1, totalMatk * rate / 100);
        var perVictim = Math.Max(1, totalDamage / realVictims.Count);
        foreach (var victim in realVictims)
        {
            ctx.Damage.ApplyDamage(victim, perVictim, source);
        }
        return true;
    }
}
