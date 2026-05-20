using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Auxiliary skill effects that don't fit the resolver-per-DamageKind
/// model — they hook onto either the cast (autospell), the on-hit
/// path (break equip / strip), or post-resolution (heal rate). The
/// rAthena entries are: <c>skill_calc_heal</c> (skill.cpp:4738),
/// <c>skill_autospell</c> (skill.cpp:5800), <c>skill_break_equip</c>
/// (skill.cpp:5847), <c>skill_strip_equip</c> (skill.cpp:5970).
///
/// <c>skill_calc_heal</c> is already real inside
/// <see cref="Resolvers.HealSkillResolver"/>; the canonical entry
/// point lives here for callers that need the formula without
/// running the full resolver dispatch (e.g. Sanctuary, Potion Pitcher).
/// </summary>
public interface ISkillSideEffectService
{
    /// <summary>rAthena <c>skill_calc_heal</c> — return the heal amount this skill would apply.</summary>
    int CalcHeal(Entity caster, Entity target, ushort skillId, ushort skillLevel, bool heal);

    /// <summary>rAthena <c>skill_autospell</c> — Sage AutoSpell skill grant.</summary>
    bool AutoSpell(Entity caster, ushort grantedSkillId);

    /// <summary>rAthena <c>skill_break_equip</c> — Acid Demonstration / Bash equip-break proc.</summary>
    bool BreakEquip(Entity src, Entity target, int equipMask, int rate);

    /// <summary>rAthena <c>skill_strip_equip</c> — Rogue Strip — Helmet / Armor / Shield / Weapon.</summary>
    bool StripEquip(Entity src, Entity target, int equipMask, int durationMs);
}
