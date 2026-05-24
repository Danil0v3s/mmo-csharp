using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_HAWKRUSH — Wind Hawk Hawk Rush. Manual port of
/// <c>rathena-fork/src/map/skills/archer/hawkrush.cpp</c>.
/// Ratio: <c>+(-100 + 500*lv) + 5*CON</c>. WH_NATUREFRIENDLY scale
/// deferred — skill id not yet registered.
/// </summary>
public sealed class HawkRush : WeaponSkillImpl
{
    public HawkRush() : base(SkillIds.WH_HAWKRUSH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 500 * skillLevel) + 5 * src.Stats.Con;
    }
}
