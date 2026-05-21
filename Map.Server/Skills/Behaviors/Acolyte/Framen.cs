using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_FRAMEN — Cardinal Framen. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/framen.cpp</c>.
///
/// <para>Holy splash. Ratio: <c>-100 + 1300*lv + 5*SPL</c> (+50*lv
/// vs Undead/Demon). Fidus Animus mastery bonus omitted.</para>
/// </summary>
public sealed class Framen : RecursiveDamageSplashSkillImpl
{
    public Framen() : base(SkillIds.CD_FRAMEN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 1300 * skillLevel) + 5 * src.Stats.Spl;
        if (target.Stats.Race == BattleRace.Undead || target.Stats.Race == BattleRace.Demon)
            ratio += 50 * skillLevel;
        return ratio;
    }
}
