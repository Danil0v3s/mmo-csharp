using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_SANCTUARY — Priest Sanctuary. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/sanctuary.cpp</c>.
///
/// <para>Drops a healing-aura SkillUnit on the target cell. The
/// per-tick heal pulse + max-hit-count is configured in
/// <c>skill_unit_db</c> (interval ~1500 ms, lives until a victim
/// cap or duration limit). This entry just places the unit.</para>
/// </summary>
public sealed class Sanctuary : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Sanctuary() : base(SkillIds.PR_SANCTUARY) { }

    public Sanctuary(ISkillUnitService? units = null) : base(SkillIds.PR_SANCTUARY)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
        _units?.Place(src, SkillId, skillLevel, x, y);
    }
}
