using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// WZ_HEAVENDRIVE (id 91) — Wizard Heaven's Drive. rAthena
/// <c>skill.cpp:case WZ_HEAVENDRIVE</c>: ground-target Earth magic
/// AoE, 5×5 splash. Per-hit damage = (125 + 25 * lv)% MATK Earth.
/// Reveals hidden targets in the splash area (rAthena: end hiding
/// SC on victims).
/// </summary>
public sealed class HeavenDriveBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.WZ_HEAVENDRIVE;

    private const short SplashRadius = 2; // 5×5 area.

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (source.Stats.MatkMin + source.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 125 + 25 * skillLevel;
        var perVictim = Math.Max(1, matk * rate / 100);

        var victims = ctx.Entities.ForEachInRange(
            target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == source.Id) continue;
            // Reveal hidden / cloaked targets (end the SC first).
            ctx.Sc?.End(v, Status.StatusType.Hiding);
            ctx.Sc?.End(v, Status.StatusType.Cloaking);
            ctx.Damage.ApplyDamage(v, perVictim, source);
        }
        return true;
    }
}
