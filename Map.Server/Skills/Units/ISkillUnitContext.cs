using Map.Server.Combat;
using Map.Server.Status;

namespace Map.Server.Skills.Units;

/// <summary>
/// Service bundle handed to every <see cref="ISkillUnitTickHandler"/>
/// callback. Mirrors the loose grab-bag rAthena hands handlers in
/// <c>src/map/skill.cpp</c> (battle pipeline + status pipeline +
/// clif broadcast). Concentrating it here means handler classes only
/// take a single ctor parameter and tests can mock the whole bundle.
/// </summary>
public interface ISkillUnitContext
{
    /// <summary>HP-mutation entry point. <see cref="IDamageService.ApplyDamage"/>.</summary>
    IDamageService Damage { get; }

    /// <summary>SC start/end pipeline. Null in environments where status_change.cpp isn't wired.</summary>
    IStatusChangeService? Sc { get; }

    /// <summary>Skill-result packet broadcaster (clif_skill_nodamage / clif_skill_damage).</summary>
    ISkillClientService? Client { get; }
}
