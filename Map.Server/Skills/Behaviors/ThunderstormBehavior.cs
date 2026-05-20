using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_THUNDERSTORM (id 21) — Mage Thunderstorm. rAthena
/// <c>skill.cpp:case MG_THUNDERSTORM</c>: 3-hit Wind magic AoE on
/// the target cell, 3×3 splash. Each hit deals (80 + 20 * lv)% MATK
/// to every victim in the splash.
/// </summary>
public sealed class ThunderstormBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_THUNDERSTORM;

    private const short SplashRadius = 1; // 3×3 area.
    private const int HitCount = 3;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 80 + 20 * skillLevel;
        var perHit = Math.Max(1, matk * rate / 100);

        var victims = ctx.Entities.ForEachInRange(
            target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        var realVictims = victims.Where(v => v.Id != source.Id).ToList();

        for (var hit = 0; hit < HitCount; hit++)
        {
            foreach (var v in realVictims)
            {
                ctx.Damage.ApplyDamage(v, perHit, source);
            }
        }
        return true;
    }
}
