using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Post-hit / pre-hit effect hooks. Canonical entry points for
/// rAthena <c>skill_additional_effect</c> + <c>skill_counter_additional_effect</c>
/// (skill.cpp). The "additional" chain runs procs caused by the
/// attacker on a successful hit (status from a card / passive proc);
/// the "counter" chain runs procs caused by the target reacting to
/// a hit (Auto Guard, reflect roll, Cursing Rod). Both are heavily
/// data-driven by the equip bonus aggregator — we keep the entry
/// points here so consumer code reads canonical, and flag the data-
/// pending side until the aggregator surfaces.
/// </summary>
public interface ISkillEffectService
{
    /// <summary>
    /// rAthena <c>skill_additional_effect</c> — after a successful
    /// hit, apply attacker-side procs (poison from carded weapon,
    /// stun from Bash, sleep from Sleeper Card).
    /// </summary>
    void AdditionalEffect(Entity src, Entity target, ushort skillId, ushort skillLevel, int attackType, long damage);

    /// <summary>
    /// rAthena <c>skill_counter_additional_effect</c> — defender-side
    /// procs (reflect, Cursing Rod, Maya Card, Magic Mirror).
    /// </summary>
    void CounterAdditionalEffect(Entity src, Entity target, ushort skillId, ushort skillLevel, int attackType, long damage);

    /// <summary>rAthena <c>skill_onskillusage</c> — fires the OnUseSkill bonus script.</summary>
    void OnSkillUsage(Entity src, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_block_check</c> — pre-damage no-damage gate.</summary>
    bool BlockCheck(Entity src, Entity target, ushort skillId);
}
