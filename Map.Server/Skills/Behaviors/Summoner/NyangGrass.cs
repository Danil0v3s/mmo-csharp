using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_NYANGGRASS — Summoner Nyang Grass. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/nyanggrass.cpp</c>.
/// Drops the unit at the cast XY. SU_SPIRITOFLAND MATK buff is TODO.
/// </summary>
public sealed class NyangGrass : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public NyangGrass() : base(SkillIds.SU_NYANGGRASS) { }

    public NyangGrass(ISkillUnitService? units = null) : base(SkillIds.SU_NYANGGRASS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
