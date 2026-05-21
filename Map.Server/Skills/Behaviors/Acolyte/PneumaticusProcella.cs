using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_PNEUMATICUS_PROCELLA — Cardinal Pneumaticus Procella. Manual
/// port of <c>rathena-fork/src/map/skills/acolyte/pneumaticusprocella.cpp</c>.
///
/// <para>Ground-placement Holy splash. Ratio:
/// <c>-100 + 150 + 2100*lv + 10*SPL</c>, +<c>50 + 150*lv</c> bonus
/// vs Undead/Demon. Fidus Animus mastery omitted.</para>
/// </summary>
public sealed class PneumaticusProcella : SkillImpl
{
    private readonly Map.Server.Skills.ISkillUnitService? _units;

    public PneumaticusProcella() : base(SkillIds.CD_PNEUMATICUS_PROCELLA) { }

    public PneumaticusProcella(Map.Server.Skills.ISkillUnitService? units = null) : base(SkillIds.CD_PNEUMATICUS_PROCELLA)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _units?.Place(src, SkillId, skillLevel, x, y);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 150 + 2100 * skillLevel) + 10 * src.Stats.Spl;
        if (target.Stats.Race == BattleRace.Undead || target.Stats.Race == BattleRace.Demon)
            ratio += 50 + 150 * skillLevel;
        return ratio;
    }
}
