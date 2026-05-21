using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_WEAPONCRUSH — Weapon Crush. Manual port of
/// <c>rathena-fork/src/map/skills/thief/weaponcrush.cpp</c>.
/// Strips the target's weapon. Strip service is TODO.
/// </summary>
public sealed class WeaponCrush : WeaponSkillImpl
{
    public WeaponCrush() : base(SkillIds.GC_WEAPONCRUSH) { }
}
