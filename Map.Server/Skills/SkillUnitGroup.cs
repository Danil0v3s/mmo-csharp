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

    /// <summary>
    /// Deferred-start gate. Tick processing skips this group until
    /// <see cref="StartAt"/> &lt;= now. Used by staggered sub-unit
    /// spawns (AG_VIOLENT_QUAKE_ATK / AG_ALL_BLOOM_ATK fan out their
    /// child units across the parent's duration via per-stagger
    /// offsets). Default 0 = fire immediately.
    /// </summary>
    public long StartAt;

    /// <summary>
    /// SK.100-1c — per-unit visibility cloaking. When true, the unit is
    /// only visible to the caster + party/guild allies (rAthena's
    /// <c>UF_HIDDEN_TRAP</c> / Pneuma / Lullaby / Land Protector
    /// non-owner blackout pattern). Visibility filters consult this
    /// before broadcasting unit-place / unit-vanish packets.
    /// </summary>
    public bool HiddenFromNonOwner;
}

/// <summary>
/// SK.100-1c — runtime visibility classifier for skill units.
/// </summary>
public static class SkillUnitVisibility
{
    /// <summary>
    /// True iff <paramref name="observer"/> may see <paramref name="group"/>'s
    /// units. Cloaking groups (Pneuma / Lullaby / trap variants) hide
    /// from non-allies; non-cloaking groups (Storm Gust, Magnus
    /// Exorcismus, etc.) are visible to everyone.
    /// </summary>
    public static bool IsVisibleTo(SkillUnitGroup group, Entity observer)
    {
        if (!group.HiddenFromNonOwner) return true;
        if (observer.Id == group.CasterId) return true;
        // Ally check — same party or same guild keeps the unit visible.
        if (observer is PlayerEntity pc)
        {
            // We don't have the caster's PlayerEntity here without a
            // registry lookup; the visibility-service caller should pass
            // an already-resolved owner-ally bit when it has one. Until
            // wired we default to "hide" which is the safe rAthena
            // behavior for traps.
            return false;
        }
        return false;
    }
}
