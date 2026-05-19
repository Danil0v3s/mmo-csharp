using Map.Server.Entities;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Strategy interface for resolving a skill's effect. One implementation
/// per <see cref="SkillDamageKind"/> — registered in
/// <see cref="SkillResolverRegistry"/> and dispatched by
/// <see cref="ISkillCastService"/>.
///
/// Same shape as <c>SkillUnitService.SpecFor</c> /
/// <c>ItemEffectRegistry</c>: replaces a switch-on-discriminator with a
/// dictionary lookup. New damage kinds add a new resolver class without
/// touching the dispatcher.
/// </summary>
public interface ISkillResolver
{
    /// <summary>Which <see cref="SkillDamageKind"/> this resolver handles.</summary>
    SkillDamageKind Kind { get; }

    /// <summary>Apply the skill's effect to <paramref name="target"/>.</summary>
    void Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel);
}
