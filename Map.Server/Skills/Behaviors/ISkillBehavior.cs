using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Per-skill behavior plugin. Mirrors a single <c>case</c> arm of
/// rAthena's <c>skill_castend_damage_id</c> / <c>skill_castend_nodamage_id</c>
/// switch (skill.cpp). Plugins fire <i>before</i> the generic
/// <see cref="Resolvers.ISkillResolver"/> dispatch — if <see cref="Resolve"/>
/// returns true the resolver is skipped; returning false (or not registering
/// at all) falls through to the generic
/// <see cref="SkillDamageKind"/>-keyed resolver.
///
/// <para>The split lets the registry stay small: most skills delegate
/// entirely to the generic resolvers (Weapon × DamageRate, Magic ×
/// element, Heal formula, …). Only skills with idiosyncratic mechanics
/// (Magnum Break splash + SC_FIREWEAPON, Bowling Bash hit-count
/// scaling, Sonic Blow chain, etc.) need a plugin.</para>
/// </summary>
public interface ISkillBehavior
{
    /// <summary>
    /// rAthena skill id this plugin handles (constants in
    /// <see cref="SkillIds"/>). Used by
    /// <see cref="SkillBehaviorRegistry"/> as the dispatch key.
    /// </summary>
    ushort SkillId { get; }

    /// <summary>
    /// Run the per-skill cast logic. Receives the dispatch context so
    /// the plugin can use shared services (damage, calc, SC, entity
    /// enumeration) without each plugin re-declaring its dependency
    /// surface.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the plugin fully handled the cast — the generic
    /// resolver is skipped. <c>false</c> to fall back to the standard
    /// <see cref="SkillDamageKind"/> dispatch (e.g. plugin only wants
    /// to add a side-effect like an SC, and the base damage should
    /// still flow through the Weapon resolver).
    /// </returns>
    bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx);
}

/// <summary>
/// Carrier for the shared services a skill behavior plugin needs.
/// Passed by <see cref="SkillCastService.ResolveSkill"/> so each
/// plugin avoids the boilerplate of constructor-injecting the same
/// five services.
/// </summary>
public sealed record SkillBehaviorContext(
    IEntityRegistry Entities,
    IDamageService Damage,
    IBattleCalculator Battle,
    IStatusChangeService? Sc);
