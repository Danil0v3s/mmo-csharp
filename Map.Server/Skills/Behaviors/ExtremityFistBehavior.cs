using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MO_EXTREMITYFIST (id 271) — Monk Asura Strike. rAthena
/// <c>skill.cpp:case MO_EXTREMITYFIST</c>: massive single-target hit
/// that consumes ALL SP (sets to 0) and the caster's Spirit Spheres.
/// Damage = (8 * lv * (10 + spheres))% × ATK + (current SP × 8) raw.
///
/// Requires combo-mode entry from MO_COMBOFINISH or stance prereqs
/// (gated upstream; the plugin focuses on the damage formula).
///
/// rAthena drops caster Sp to 0 + applies SC_EXPLOSIONSPIRITS / etc.
/// post-hit; we approximate the SP drain inline so the cost is
/// observable in tests.
/// </summary>
public sealed class ExtremityFistBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MO_EXTREMITYFIST;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var spBefore = source.Stats.Sp;
        // Damage formula: rough rAthena approximation.
        var swing = ctx.Battle.CalcWeaponAttack(source, target);
        var dmg = (int)Math.Clamp(swing.Total * (8 * skillLevel + 100) / 100 + spBefore * 8,
            0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, Math.Max(1, dmg), source);

        // Consume all SP.
        source.Stats.Sp = 0;
        return true;
    }
}
