using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_ANKLESNARE — Hunter Ankle Snare. Manual port of
/// <c>rathena-fork/src/map/skills/archer/anklesnare.cpp</c>.
/// Drops the Ankle Snare trap at the cast XY.
/// </summary>
public sealed class AnkleSnare : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public AnkleSnare() : base(SkillIds.HT_ANKLESNARE) { }

    public AnkleSnare(ISkillUnitService? units = null) : base(SkillIds.HT_ANKLESNARE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
