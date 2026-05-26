using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_GALESTORM — Wind Hawk Gale Storm. Manual port of
/// <c>rathena-fork/src/map/skills/archer/galestorm.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 1350*lv) + 10*CON</c>; SC_CALAMITYGALE on
/// the caster adds <c>×1.5</c> against Brute / Fish targets.</para>
/// </summary>
public sealed class GaleStorm : RecursiveDamageSplashSkillImpl
{
    public GaleStorm() : base(SkillIds.WH_GALESTORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 1350 * skillLevel) + 10 * src.Stats.Con;
        if (ctx.Sc != null && ctx.Sc.Get(src, StatusType.Calamitygale) != null
            && (target.Stats.Race == BattleRace.Brute || target.Stats.Race == BattleRace.Fish))
        {
            ratio += ratio * 50 / 100;
        }
        return ratio;
    }
}
