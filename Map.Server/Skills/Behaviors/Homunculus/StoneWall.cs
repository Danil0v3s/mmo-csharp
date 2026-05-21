using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_STEINWAND — Homunculus Steinwand (Stone Wall). Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_stonewall.cpp</c>.
/// Drops the wall at (x, y).
/// </summary>
public sealed class StoneWall : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public StoneWall() : base(SkillIds.MH_STEINWAND) { }

    public StoneWall(ISkillUnitService? units = null) : base(SkillIds.MH_STEINWAND)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
