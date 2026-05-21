using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_BASIC_GRENADE — Night Watch Basic Grenade. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/basicgrenade.cpp</c>.
/// Ratio <c>+(-100 + 1500 + 2100*lv) + 5*CON</c>. Grenade Mastery
/// bonus is TODO. Splash dispatch is TODO; we land on the target.
/// </summary>
public sealed class BasicGrenade : WeaponSkillImpl
{
    public BasicGrenade() : base(SkillIds.NW_BASIC_GRENADE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1500 + 2100 * skillLevel) + 5 * src.Stats.Con;
}
