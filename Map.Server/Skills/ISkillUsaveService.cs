using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// rAthena <c>skill_usave_*</c> — per-character "last cast" save /
/// trigger used by SC_DOUBLECAST and similar (skill.cpp). Each PC has
/// a small ring of recent casts; the trigger replays one.
/// </summary>
public interface ISkillUsaveService
{
    /// <summary>rAthena <c>skill_usave_add</c> — record a cast for later replay.</summary>
    void UsaveAdd(PlayerEntity caster, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_usave_trigger</c> — replay the saved cast.</summary>
    bool UsaveTrigger(PlayerEntity caster);
}

/// <summary>
/// rAthena <c>skill_init_unit_layout</c> + <c>skill_init_nounit_layout</c>
/// boot-time layout matrix. The matrix maps a layout-type id (Storm Gust,
/// Heaven's Drive, Earth Spike, …) to the per-cell offset list that
/// <see cref="ISkillUnitService.Place"/> reads to know which cells to
/// instantiate. Until it ports, ground units fall back to the square
/// radius in <see cref="SkillUnitService.SpecFor"/>.
/// </summary>
public interface ISkillLayoutService
{
    /// <summary>Return the cell offsets for the requested layout type.</summary>
    IReadOnlyList<(short dx, short dy)> GetLayout(int layoutType);

    /// <summary>
    /// SK.100-1b/d — per-skill / per-level layout. Returns the named
    /// shape for skills with non-square unit footprints (FireWall row,
    /// IceWall cross, WallOfThorn ring, FireBall plus, trap squares).
    /// Falls back to a square <paramref name="defaultRadius"/> for
    /// skills without a named layout — preserves the legacy
    /// <see cref="SkillUnitService.Place"/> behavior.
    /// </summary>
    IReadOnlyList<(short Dx, short Dy)> GetLayoutForSkill(
        ushort skillId, ushort skillLevel, short defaultRadius, short casterDir = 0);
}
