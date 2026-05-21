using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_CALLPARENT — Call Parent ground unit. Manual port of
/// <c>rathena-fork/src/map/skills/other/callparent.cpp</c>.
/// </summary>
public sealed class CallParent : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CallParent() : base(SkillIds.WE_CALLPARENT) { }

    public CallParent(ISkillUnitService? units = null) : base(SkillIds.WE_CALLPARENT)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
