using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AL_HOLYLIGHT (id 156) — Acolyte Holy Light. rAthena
/// <c>skill.cpp:case AL_HOLYLIGHT</c>: single-target Holy magic hit,
/// 125 % MATK base. The Holy element multiplier comes from the
/// element table (read by the Magic resolver via skill_db.Element);
/// plugin just routes through with a Holy-aware damage line.
///
/// Returns false so the generic Magic resolver runs — the only
/// rAthena specialization is "use the larger of magic/match level"
/// for casters in priest job stance, which we approximate by letting
/// the resolver use the skill's own DamageRate as the multiplier.
/// </summary>
public sealed class HolyLightBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AL_HOLYLIGHT;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // For now Holy Light defers entirely to the Magic resolver —
        // skill_db's DamageRate column carries the 125 % multiplier and
        // SkillDefinition.Element = Holy drives the element-fix lookup.
        // If a future client cast-time bonus needs different math the
        // plugin steps in here.
        return false;
    }
}
