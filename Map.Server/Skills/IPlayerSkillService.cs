using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// rAthena <c>pc_skill</c> (pc.cpp:3091) — adds / replaces / removes a
/// skill on the PC's <see cref="PlayerEntity.LearnedSkills"/> table.
/// Wraps the dictionary mutation + the per-skill recalc trigger so
/// every caller (NPC script, @allskill, levelup grant, plagiarism)
/// goes through the same broadcast pipe.
/// </summary>
public interface IPlayerSkillService
{
    /// <summary>
    /// Apply <paramref name="skillId"/> at <paramref name="level"/> per
    /// <paramref name="kind"/>. Returns true on success; false on
    /// validation failure (unknown skill / level out of range).
    /// </summary>
    bool Grant(PlayerEntity pc, ushort skillId, int level, GrantKind kind = GrantKind.Permanent);

    /// <summary>Convenience — sets the level to 0 (rAthena Permanent with lv=0).</summary>
    void Revoke(PlayerEntity pc, ushort skillId);
}

/// <summary>
/// Mirror of rAthena <c>enum e_addskill_type</c> — controls how the
/// caller composes with any existing skill entry.
/// </summary>
public enum GrantKind
{
    /// <summary>Overwrite the slot at <c>level</c>.</summary>
    Permanent,
    /// <summary>Add <c>level</c> to the existing level, clamped to <c>MaxLevel</c>.</summary>
    TemporaryAdd,
    /// <summary>Quest-skill grant (rAthena ADDSKILL_PERMANENT_QUEST).</summary>
    PermanentQuest,
}
