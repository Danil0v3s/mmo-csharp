using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Manages ground-placed skill effects ("skill units" in rAthena —
/// <c>skill_unitsetting</c> / <c>skill_unit_onplace_timer</c>, skill.cpp).
/// First slice: a per-cell periodic-damage model that fits Magnus
/// Exorcismus / Storm Gust / Sanctuary cleanly. Defensive units
/// (Safety Wall, Pneuma) layer on the same lifecycle once the
/// damage-interception hook lands.
/// </summary>
public interface ISkillUnitService
{
    /// <summary>
    /// Create a group at <paramref name="centerX"/>/<paramref name="centerY"/>.
    /// The cells covered come from the skill's layout (square radius for
    /// the starter set).
    /// </summary>
    SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short centerX, short centerY);

    /// <summary>
    /// Deferred-start variant — same as <see cref="Place(Entity, ushort, ushort, short, short)"/>
    /// but the group's tick processing is gated until <paramref name="delayMs"/>
    /// elapses. Used by staggered sub-unit spawns
    /// (AG_VIOLENT_QUAKE_ATK / AG_ALL_BLOOM_ATK fan-out, where each
    /// child unit lands at <c>i * skill_get_unit_interval</c>).
    /// </summary>
    SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short centerX, short centerY, int delayMs)
        => Place(caster, skillId, skillLevel, centerX, centerY);

    /// <summary>Game-loop pump — fires periodic effects, expires groups.</summary>
    void Tick(long nowTick);

    /// <summary>
    /// rAthena <c>skill_unit_move</c> — re-bind every unit on a cell when
    /// the entity standing on the cell moved (re-trigger onplace timers).
    /// </summary>
    void UnitMove(Entity who, long tick, int flag);

    /// <summary>
    /// rAthena <c>skill_unit_move_unit</c> — relocate a single unit (e.g.
    /// units that follow the caster: Lullaby, Magnetic Earth).
    /// </summary>
    void UnitMoveUnit(SkillUnit unit, short newX, short newY);

    /// <summary>
    /// rAthena <c>skill_unit_move_unit_group</c> — relocate an entire group.
    /// </summary>
    void UnitMoveUnitGroup(SkillUnitGroup group, short newX, short newY);

    /// <summary>rAthena <c>skill_unit_onleft</c> — entity left the cell.</summary>
    void UnitOnLeft(SkillUnit unit, Entity who, long tick);

    /// <summary>rAthena <c>skill_unit_onout</c> — entity stepped out of the unit area.</summary>
    void UnitOnOut(SkillUnit unit, Entity who, long tick);

    /// <summary>
    /// rAthena <c>skill_unit_ondamaged</c> — the unit itself took damage
    /// (Ice Wall HP loss, trap detonate).
    /// </summary>
    void UnitOnDamaged(SkillUnit unit, long damage);

    /// <summary>rAthena <c>skill_clear_unitgroup</c> — drop all of a caster's groups.</summary>
    void ClearUnitGroup(EntityId casterId);

    /// <summary>rAthena <c>skill_delunit</c> — delete a single unit.</summary>
    void DelUnit(SkillUnit unit);

    /// <summary>rAthena <c>skill_delunitgroup_</c> — delete an entire group.</summary>
    void DelUnitGroup(SkillUnitGroup group);

    /// <summary>
    /// rAthena <c>map_foreachincell</c> over BL_SKILL — enumerate every
    /// active ground unit within Chebyshev distance
    /// <paramref name="radius"/> of (<paramref name="cx"/>, <paramref name="cy"/>)
    /// on the given map. Used by SpringTrap (trip one cell), RemoveTrap
    /// / Trample (dispel any traps in splash), Detonator / DominionImpulse
    /// (chain effect across a splash), SafetyWall (Land Protector
    /// overlap check), etc. Returns the live <see cref="SkillUnit"/>
    /// references; callers must not mutate them directly — use
    /// <see cref="DelUnit"/> / <see cref="DelUnitGroup"/> for removal.
    /// </summary>
    IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius);

    /// <summary>
    /// rAthena <c>map_find_skill_unit_oncell</c> — same as
    /// <see cref="GetUnitsInArea(uint,short,short,short)"/> but filtered to
    /// units whose parent group's <see cref="SkillUnitGroup.SkillId"/>
    /// matches <paramref name="skillId"/>. Used by the Land Protector
    /// overlap gate and the per-trap targeting (SpringTrap fires only
    /// trap-class units).
    /// </summary>
    IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius, ushort skillId);
}
