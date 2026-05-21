using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_HAWKBOOMERANG — Wind Hawk Hawk Boomerang. Manual port of
/// <c>rathena-fork/src/map/skills/archer/hawkboomerang.cpp</c>.
/// Ratio: <c>+(-100 + 600*lv) + 10*CON</c>; WH_NATUREFRIENDLY passive
/// scale + Brute/Fish ×1.5 bonus (race scaled here, passive TODO).
/// </summary>
public sealed class HawkBoomerang : WeaponSkillImpl
{
    public HawkBoomerang() : base(SkillIds.WH_HAWKBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 600 * skillLevel) + 10 * src.Stats.Con;
        if (target.Stats.Race == BattleRace.Brute || target.Stats.Race == BattleRace.Fish)
            ratio += ratio * 50 / 100;
        return ratio;
    }
}
