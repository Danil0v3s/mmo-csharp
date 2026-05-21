using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_GRAND_JUDGEMENT — Imperial Guard Grand Judgement. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/grandjudgement.cpp</c>.
/// Ratio <c>+(-100 + 250 + 1500*lv) + 10*POW</c>; <c>+(100 + 150*lv)</c>
/// against Plant / Insect. Imperial-guard checkskill bonus is TODO.
/// </summary>
public sealed class GrandJudgement : RecursiveDamageSplashSkillImpl
{
    public GrandJudgement() : base(SkillIds.IG_GRAND_JUDGEMENT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 250 + 1500 * skillLevel) + 10 * src.Stats.Pow;
        if (target.Stats.Race == BattleRace.Plant || target.Stats.Race == BattleRace.Insect)
            ratio += 100 + 150 * skillLevel;
        return ratio;
    }
}
