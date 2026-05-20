using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// HT_BLITZBEAT (id 129) — Hunter Blitz Beat. rAthena
/// <c>skill.cpp:case HT_BLITZBEAT</c>: falcon-driven ranged Wind hit.
/// Hit count = <c>min(skill_lv, 5)</c> (5 hits at lv5+), each hit
/// deals MATK-like damage scaled by INT/DEX.
///
/// Requires the caster to have a falcon equipped (rAthena gates at
/// requirement check — the C# port reads it from the falcon state on
/// PlayerEntity once that hook ports).
/// </summary>
public sealed class BlitzBeatBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.HT_BLITZBEAT;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Hit count: 1 / 1 / 2 / 2 / 3 / 3 / 4 / 4 / 5 / 5 in pre-renewal.
        // Renewal: min(lv, 5).
        var hitCount = Math.Min((int)skillLevel, 5);

        // Damage per hit: rAthena uses (caster.Dex * caster.Int) / 10 +
        // (caster.Lv + caster.Int) per hit. Simplified mirror; element
        // fix applied via the standard damage pipeline.
        var perHit = (source.Stats.Dex * source.Stats.IntStat) / 10
                     + source.Level + source.Stats.IntStat;
        for (var hit = 0; hit < hitCount; hit++)
        {
            ctx.Damage.ApplyDamage(target, Math.Max(1, perHit), source);
        }
        return true;
    }
}
