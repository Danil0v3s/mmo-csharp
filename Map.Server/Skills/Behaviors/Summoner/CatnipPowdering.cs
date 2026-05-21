using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_CN_POWDERING — Summoner Catnip Powdering. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/catnippowdering.cpp</c>.
/// Drops the catnip-cell unit; SU_SPIRITOFLAND flee2 buff is TODO.
/// </summary>
public sealed class CatnipPowdering : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CatnipPowdering() : base(SkillIds.SU_CN_POWDERING) { }

    public CatnipPowdering(ISkillUnitService? units = null) : base(SkillIds.SU_CN_POWDERING)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
