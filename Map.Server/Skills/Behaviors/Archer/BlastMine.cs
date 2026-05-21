using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_BLASTMINE — Hunter Blast Mine. Manual port of
/// <c>rathena-fork/src/map/skills/archer/blastmine.cpp</c>.
/// Drops the Blast Mine trap at the cast XY.
/// </summary>
public sealed class BlastMine : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public BlastMine() : base(SkillIds.HT_BLASTMINE) { }

    public BlastMine(ISkillUnitService? units = null) : base(SkillIds.HT_BLASTMINE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
