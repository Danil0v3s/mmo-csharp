using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AC_SHOWER (id 47) — Archer Arrow Shower. rAthena
/// <c>skill.cpp:case AC_SHOWER</c>: ground-targeted AoE physical at
/// the cast location, 3×3 splash radius. Every target in the splash
/// takes (75 + 5 * lv)% ATK ranged damage.
///
/// rAthena consumes one arrow per cast (handled by the requirement
/// check upstream; arrow consumption ports there).
/// </summary>
public sealed class ArrowShowerBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AC_SHOWER;

    private const short SplashRadius = 1; // 3×3 area.

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 75 + 5 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(
            target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);

        foreach (var victim in victims)
        {
            if (victim.Id == source.Id) continue;
            var swing = ctx.Battle.CalcWeaponAttack(source, victim);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(victim, dmg, source);
        }
        return true;
    }
}
