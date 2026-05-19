using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// One ground-placed skill effect (e.g. one cell of Storm Gust, one
/// tile of Magnus Exorcismus). Mirrors rAthena <c>struct skill_unit</c>
/// (skill.hpp). Owned by its parent <see cref="SkillUnitGroup"/>.
/// </summary>
public sealed class SkillUnit
{
    public required SkillUnitGroup Group;
    public required short X;
    public required short Y;

    /// <summary>Tick at which this individual cell's next periodic fires. Same as group's interval cadence.</summary>
    public long NextTick;

    /// <summary>True after the unit has been removed from the world; the group reaper sweeps it.</summary>
    public bool Removed;
}

/// <summary>
/// All cells of a single skill cast bundled together — when the group
/// expires every member <see cref="SkillUnit"/> is removed and the
/// group lifecycle ends. Mirrors rAthena <c>struct skill_unit_group</c>.
/// </summary>
public sealed class SkillUnitGroup
{
    public required ushort SkillId;
    public required ushort SkillLevel;
    public required EntityId CasterId;
    public required uint MapId;
    public required long ExpiresAt;
    public required int IntervalMs;
    public List<SkillUnit> Units = new();
}
