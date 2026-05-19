using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Resolvers;

/// <summary>
/// Resolves <see cref="SkillDamageKind.Misc"/> — pre-computed damage
/// without weapon/magic atk roll. Used by skills like Soul Strike whose
/// damage is hard-coded per level rather than derived from stats.
/// </summary>
public sealed class MiscSkillResolver : ISkillResolver
{
    public SkillDamageKind Kind => SkillDamageKind.Misc;

    private readonly IDamageService _damage;

    public MiscSkillResolver(IDamageService damage) { _damage = damage; }

    public void Resolve(Entity source, Entity target, SkillDefinition def, ushort lvl)
    {
        var amount = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 0;
        _damage.ApplyDamage(target, amount, source);
    }
}
