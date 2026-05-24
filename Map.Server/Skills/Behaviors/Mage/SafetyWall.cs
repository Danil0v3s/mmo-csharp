using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_SAFETYWALL — Mage Safety Wall. Manual port of
/// <c>rathena-fork/src/map/skills/mage/safetywall.cpp</c>.
///
/// <para>Ground-target placement that drops a 1-cell SkillUnit
/// blocking incoming melee/short-range physical attacks. The
/// per-cell hit absorption + charge counter live on the SkillUnit
/// engine (configured by the MG_SAFETYWALL entry in skill_unit_db).</para>
///
/// <para>The Land Protector overlap check (<c>skill_cell_overlap</c>)
/// is preserved as a TODO: when the target cell already has a Land
/// Protector unit, rAthena still places the wall (returning early
/// to skip the ammo-consume flag). The C# port doesn't yet have
/// per-cell unit overlap queries on <see cref="ISkillUnitService"/> —
/// placement happens unconditionally for now; the overlap branch
/// only affected gem consumption gating, which our Item requirement
/// service handles separately.</para>
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
        // rAthena: skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
        // The per-cell hit absorption is driven by the SkillUnit engine
        // off the MG_SAFETYWALL skill_unit_db entry (max hits = level-dependent).
        _units?.Place(src, SkillId, skillLevel, x, y);

        // Deferred: Land Protector overlap check (skill_cell_overlap). When the target
        // cell already has a Land Protector unit, rAthena sets SKILL_NOCONSUME_REQ to refund
        // the Yellow Gemstone. Needs ISkillUnitService.HasUnitAt(skillId, x, y) query.
    }
}
