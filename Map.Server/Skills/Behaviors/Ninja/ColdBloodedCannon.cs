using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_REIKETSUHOU — Cold Blooded Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/coldbloodedcannon.cpp</c>.
/// POS2 splash; ratio <c>+(-100 + 450 + 950*lv) + 5*spl</c> with
/// SS_ANTENPOU * 40 * lv per-skill bonus and +7000 when
/// SC_WATER_CHARM_POWER is active. AoE iteration is TODO.
/// </summary>
public sealed class ColdBloodedCannon : SkillImpl
{
    public ColdBloodedCannon() : base(SkillIds.SS_REIKETSUHOU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 450 + 950 * skillLevel);
        ratio += 5 * src.Stats.Spl;
        // TODO: SS_ANTENPOU * 40 * lv bonus and SC_WATER_CHARM_POWER +7000.
        return ratio;
    }
}
