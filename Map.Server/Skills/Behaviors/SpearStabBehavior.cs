using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// KN_SPEARSTAB (id 58) — Knight Spear Stab. rAthena
/// <c>skill.cpp:case KN_SPEARSTAB</c>: single ranged physical hit
/// at (100 + 20 * lv)% ATK that knocks the target back. Damage flows
/// through the generic Weapon resolver via DamageRate; the
/// knockback line ports separately (movement-direction helper).
///
/// Plugin returns false so the standard Weapon resolver does the
/// hit; only the knockback specialization needs an override here
/// and that lands when the directional-knockback infra ports.
/// </summary>
public sealed class SpearStabBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.KN_SPEARSTAB;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
        => false; // generic Weapon resolver handles the hit.
}
