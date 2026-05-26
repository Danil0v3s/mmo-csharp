using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_HAWKBOOMERANG — Wind Hawk Hawk Boomerang. Manual port of
/// <c>rathena-fork/src/map/skills/archer/hawkboomerang.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 600*lv + 10*CON)</c>; the running ratio is
/// scaled by <c>ratio * WH_NATUREFRIENDLY / 10</c>, and Brute / Fish
/// targets add another <c>×1.5</c>.</para>
/// </summary>
public sealed class HawkBoomerang : WeaponSkillImpl
{
    public HawkBoomerang() : base(SkillIds.WH_HAWKBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 600 * skillLevel) + 10 * src.Stats.Con;
        if (src is PlayerEntity pc)
        {
            var nature = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WH_NATUREFRIENDLY) ?? 0;
            ratio += ratio * nature / 10;
        }
        if (target.Stats.Race == BattleRace.Brute || target.Stats.Race == BattleRace.Fish)
            ratio += ratio * 50 / 100;
        return ratio;
    }
}
