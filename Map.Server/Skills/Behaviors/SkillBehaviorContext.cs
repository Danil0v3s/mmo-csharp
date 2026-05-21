using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Carrier for the shared services a <see cref="SkillImpl"/>
/// subclass needs. Passed by <see cref="SkillCastService.ResolveSkill"/>
/// so each plugin avoids the boilerplate of constructor-injecting
/// the same handful of services.
///
/// Mirrors the role of the global function pointers + map_session
/// access used inside rAthena's switch arms (caster's <c>sd</c>,
/// <c>battle_calc_attack</c>, <c>status_damage</c>, <c>status_change_*</c>,
/// <c>map_foreachinrange</c>, the three <c>clif_skill_*</c> helpers).
/// </summary>
/// <param name="Entities">Spatial registry — lookup by id, splash iteration.</param>
/// <param name="Damage">HP-mutation pipeline (mirrors <c>status_damage</c>).</param>
/// <param name="Battle">Renewal damage formula entry point.</param>
/// <param name="Sc">Status-change service. Null in unit tests that
///     don't exercise SCs.</param>
/// <param name="Client">Skill-result broadcaster — wraps the three
///     <c>clif_skill_*</c> packets. Null in unit tests that don't
///     exercise the visibility layer.</param>
public sealed record SkillBehaviorContext(
    IEntityRegistry Entities,
    IDamageService Damage,
    IBattleCalculator Battle,
    IStatusChangeService? Sc,
    ISkillClientService? Client = null);
