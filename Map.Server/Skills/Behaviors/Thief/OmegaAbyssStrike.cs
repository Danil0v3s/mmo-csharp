using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_ABYSS_STRIKE — Omega Abyss Strike. Manual port of
/// <c>rathena-fork/src/map/skills/thief/omegaabyssstrike.cpp</c>.
/// POS2 unit placement; ratio <c>+(-100 + 2650*lv) + 10*spl</c>;
/// +200*lv vs Demon / Angel.
/// </summary>
public sealed class OmegaAbyssStrike : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public OmegaAbyssStrike() : base(SkillIds.ABC_ABYSS_STRIKE) { }

    public OmegaAbyssStrike(ISkillUnitService? units = null) : base(SkillIds.ABC_ABYSS_STRIKE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 2650 * skillLevel) + 10 * src.Stats.Spl;
        if (target.Stats.Race == BattleRace.Demon || target.Stats.Race == BattleRace.Angel)
            ratio += 200 * skillLevel;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
