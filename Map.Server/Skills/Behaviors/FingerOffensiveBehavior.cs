using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MO_FINGEROFFENSIVE (id 267) — Monk Finger Offensive. rAthena
/// <c>skill.cpp:case MO_FINGEROFFENSIVE</c>: ranged physical at
/// (150 + 50 * lv)% ATK per hit. Hit count = number of Spirit
/// Spheres the caster has when the cast starts (rAthena consumes
/// all spheres up to skill level cap).
///
/// We approximate hit count = skillLevel (Spirit Sphere tracking
/// lives on PlayerEntity and the cost pipeline handles consumption
/// separately). True formula plugs in once the spirit-sphere ctx
/// flows through.
/// </summary>
public sealed class FingerOffensiveBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MO_FINGEROFFENSIVE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hitCount = skillLevel;
        var rate = 150 + 50 * skillLevel;
        for (var hit = 0; hit < hitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(source, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, source);
        }
        return true;
    }
}
