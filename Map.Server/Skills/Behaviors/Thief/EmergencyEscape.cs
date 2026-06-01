using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_ESCAPE — Emergency Escape. Manual port of
/// <c>rathena-fork/src/map/skills/thief/emergencyescape.cpp</c>.
/// POS2 unit-set + caster backslide (BLOWN_IGNORE_NO_KNOCKBACK).
/// The slide reads the caster's facing direction so the cell trail
/// goes the way the caster's looking, matching
/// <c>unit_getdir(src)</c> on rAthena.
/// </summary>
public sealed class EmergencyEscape : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly IUnitOpsService? _unitOps;

    /// <summary>rAthena <c>skill_get_blewcount(SC_ESCAPE, lv)</c> — 7
    /// cells at every level.</summary>
    private const int BLEW_COUNT = 7;

    public EmergencyEscape() : base(SkillIds.SC_ESCAPE) { }

    public EmergencyEscape(ISkillUnitService? units = null, IUnitOpsService? unitOps = null)
        : base(SkillIds.SC_ESCAPE)
    {
        _units = units;
        _unitOps = unitOps;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _units?.Place(src, SkillId, skillLevel, x, y);
        // rAthena: skill_blown(src, src, blewcount, unit_getdir(src),
        //   BLOWN_IGNORE_NO_KNOCKBACK). The slide is on the caster,
        // not the target, and the IGNORE flag bypasses no-knockback
        // PvP / map flags.
        var dir = _unitOps?.GetDir(src) ?? 0;
        _unitOps?.BlownBy(src, dir, BLEW_COUNT);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
