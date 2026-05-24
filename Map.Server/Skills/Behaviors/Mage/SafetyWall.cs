using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_SAFETYWALL — Mage Safety Wall (skill.cpp:MG_SAFETYWALL arm).
/// Ground-target placement that drops a 1-cell SkillUnit blocking
/// incoming melee / short-range physical attacks. Per-cell hit
/// absorption + charge counter live on the SkillUnit engine
/// (configured by the MG_SAFETYWALL row in <c>skill_unit_db</c>).
///
/// <para>Land Protector overlap gate (<c>skill_cell_overlap</c>):
/// when the target cell already has a Land Protector unit, rAthena
/// refuses to place the wall (the ground spell is suppressed for the
/// caster's faction). We use <see cref="ISkillUnitService.GetUnitsInArea(uint,short,short,short,ushort)"/>
/// to detect the overlap and refuse the placement, matching rAthena.</para>
/// </summary>
public sealed class SafetyWall : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public SafetyWall() : base(SkillIds.MG_SAFETYWALL) { }

    public SafetyWall(ISkillUnitService? units = null) : base(SkillIds.MG_SAFETYWALL)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Land Protector overlap gate — rAthena suppresses ground spells
        // on cells claimed by an active SA_LANDPROTECTOR unit. Prefer
        // ctx.Units (DI-routed) but fall back to ctor-injected _units
        // for unit tests that don't drive the full pipeline.
        var unitSvc = ctx.Units ?? _units;
        if (unitSvc != null)
        {
            var overlap = unitSvc.GetUnitsInArea(src.MapId, x, y, radius: 0, SkillIds.SA_LANDPROTECTOR);
            if (overlap.Count > 0)
            {
                // Wall placement refused; rAthena also sets SKILL_NOCONSUME_REQ
                // so the Yellow Gemstone is refunded. The refund routes
                // through the skill-requirement consume hook (separate
                // service); the placement-side gate lands here.
                return;
            }
        }

        unitSvc?.Place(src, SkillId, skillLevel, x, y);
    }
}
