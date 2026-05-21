using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_CN_METEOR — Summoner Catnip Meteor. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/catnipmeteor.cpp</c>.
/// Ratio <c>+(-100 + 200 + 100*lv)</c>, plus <c>5*INT</c> when BaseLv > 99.
/// Random-cell drop loop + CN_METEOR2 variant (if catnip fruit equipped)
/// are TODO.
/// </summary>
public sealed class CatnipMeteor : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CatnipMeteor() : base(SkillIds.SU_CN_METEOR) { }

    public CatnipMeteor(ISkillUnitService? units = null) : base(SkillIds.SU_CN_METEOR)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 200 + 100 * skillLevel);
        if (src.Level > 99)
            ratio += src.Stats.IntStat * 5;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
