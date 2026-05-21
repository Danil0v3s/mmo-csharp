using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_POEMOFNETHERWORLD — Minstrel/Wanderer Poem of the Netherworld.
/// Manual port of <c>rathena-fork/src/map/skills/archer/poemofthenetherworld.cpp</c>.
/// Drops the rooting ground unit.
/// </summary>
public sealed class PoemOfTheNetherworld : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PoemOfTheNetherworld() : base(SkillIds.WM_POEMOFNETHERWORLD) { }

    public PoemOfTheNetherworld(ISkillUnitService? units = null) : base(SkillIds.WM_POEMOFNETHERWORLD)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
