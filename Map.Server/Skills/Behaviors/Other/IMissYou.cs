using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_CALLPARTNER — Wedding partner-recall ground unit. Manual port of
/// <c>rathena-fork/src/map/skills/other/imissyou.cpp</c>.
/// </summary>
public sealed class IMissYou : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public IMissYou() : base(SkillIds.WE_CALLPARTNER) { }

    public IMissYou(ISkillUnitService? units = null) : base(SkillIds.WE_CALLPARTNER)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
