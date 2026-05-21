using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_ESCAPE — Emergency Escape. Manual port of
/// <c>rathena-fork/src/map/skills/thief/emergencyescape.cpp</c>.
/// POS2 unit-set + caster backslide (BLOWN_IGNORE_NO_KNOCKBACK).
/// Self-knockback is TODO.
/// </summary>
public sealed class EmergencyEscape : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public EmergencyEscape() : base(SkillIds.SC_ESCAPE) { }

    public EmergencyEscape(ISkillUnitService? units = null) : base(SkillIds.SC_ESCAPE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _units?.Place(src, SkillId, skillLevel, x, y);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
