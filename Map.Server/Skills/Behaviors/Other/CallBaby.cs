using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_CALLBABY — Call Baby ground unit. Manual port of
/// <c>rathena-fork/src/map/skills/other/callbaby.cpp</c>.
/// </summary>
public sealed class CallBaby : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CallBaby() : base(SkillIds.WE_CALLBABY) { }

    public CallBaby(ISkillUnitService? units = null) : base(SkillIds.WE_CALLBABY)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
