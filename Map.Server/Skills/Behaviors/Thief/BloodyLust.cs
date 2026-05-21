using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_BLOODYLUST — Bloody Lust. Manual port of
/// <c>rathena-fork/src/map/skills/thief/bloodylust.cpp</c>.
/// Drops a Bloody Lust cell at the targeted tile.
/// </summary>
public sealed class BloodyLust : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public BloodyLust() : base(SkillIds.SC_BLOODYLUST) { }

    public BloodyLust(ISkillUnitService? units = null) : base(SkillIds.SC_BLOODYLUST)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
