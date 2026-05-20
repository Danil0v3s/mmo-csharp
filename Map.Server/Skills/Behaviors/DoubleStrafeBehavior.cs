using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AC_DOUBLE (id 46) — Archer Double Strafe. rAthena
/// <c>skill.cpp:case AC_DOUBLE</c>: 2-hit ranged physical, each hit
/// (90 + 10 * lv)% ATK. Plugin runs both swings and writes the total
/// damage; we skip the generic Weapon resolver since it would only
/// fire once.
/// </summary>
public sealed class DoubleStrafeBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AC_DOUBLE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 90 + 10 * skillLevel;
        for (var hit = 0; hit < 2; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(source, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, source);
        }
        return true;
    }
}
