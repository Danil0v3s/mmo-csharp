using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MO_TRIPLEATTACK (id 263) — Monk Triple Attack. rAthena
/// <c>skill.cpp:case MO_TRIPLEATTACK</c>: 3-hit physical, each
/// (110 + 30 * lv)% / 3 ATK. Combo starter for the rest of the Monk
/// chain (ChainCombo → ComboFinish → ExtremityFist) — those follow-ups
/// land as their own plugins.
/// </summary>
public sealed class TripleAttackBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MO_TRIPLEATTACK;

    private const int HitCount = 3;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var totalRate = 110 + 30 * skillLevel;
        var perHitRate = totalRate / HitCount;
        for (var hit = 0; hit < HitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(source, target);
            var dmg = (int)Math.Clamp(swing.Total * perHitRate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, source);
        }
        return true;
    }
}
