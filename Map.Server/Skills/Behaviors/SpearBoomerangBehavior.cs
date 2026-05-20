using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// KN_SPEARBOOMERANG (id 59) — Knight Spear Boomerang. rAthena
/// <c>skill.cpp:case KN_SPEARBOOMERANG</c>: long-range thrown spear,
/// single hit at (100 + 50 * lv)% ATK. Range increases per level
/// (7-cell base, +1 per level beyond 1).
///
/// Damage flows through the standard Weapon resolver — skill_db's
/// DamageRate column handles the scaling. The range/ranged property
/// is read from skill_db too; no plugin specialization needed today.
/// </summary>
public sealed class SpearBoomerangBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.KN_SPEARBOOMERANG;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
        => false; // defer to Weapon resolver.
}
