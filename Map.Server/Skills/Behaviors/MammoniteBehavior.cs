using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MC_MAMMONITE (id 42) — Merchant Mammonite. rAthena
/// <c>skill.cpp:case MC_MAMMONITE</c>: single physical hit at
/// (100 + 50 * lv)% ATK. Pays <c>100 * lv</c> zeny on top of SP cost
/// (rAthena: gated at the requirement check, refunds on miss).
///
/// Zeny deduction is the canonical Merchant identity — the cost
/// pipeline (skill_db.ZenyCost column) handles it upstream of resolve,
/// so the plugin just runs the damage.
/// </summary>
public sealed class MammoniteBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MC_MAMMONITE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 100 + 50 * skillLevel;
        var swing = ctx.Battle.CalcWeaponAttack(source, target);
        var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, dmg, source);
        return true;
    }
}
