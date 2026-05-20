using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Carrier for the shared services a <see cref="SkillImpl"/>
/// subclass needs. Passed by <see cref="SkillCastService.ResolveSkill"/>
/// so each plugin avoids the boilerplate of constructor-injecting
/// the same five services.
///
/// Mirrors the role of the global function pointers + map_session
/// access used inside rAthena's switch arms (caster's <c>sd</c>,
/// <c>battle_calc_attack</c>, <c>status_damage</c>, <c>status_change_*</c>,
/// <c>map_foreachinrange</c>).
/// </summary>
public sealed record SkillBehaviorContext(
    IEntityRegistry Entities,
    IDamageService Damage,
    IBattleCalculator Battle,
    IStatusChangeService? Sc);
