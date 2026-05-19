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

    /// <summary>
    /// rAthena <c>pc_calc_skilltree</c> (pc.cpp:1611) — walks the
    /// job-tree.yml dependency graph and populates the PC's known
    /// skill list. First slice is a no-op (skill_tree.yml not loaded);
    /// canonical entry stays here so callers don't drift to direct
    /// LearnedSkills mutation.
    /// </summary>
    void CalcSkillTree(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_clean_skilltree</c> (pc.cpp:1716) — wipes every
    /// learned skill flagged PERMANENT_GRANTED (tree-issued). First
    /// slice no-op; needs skill_flag tracking.
    /// </summary>
    void CleanSkillTree(PlayerEntity pc);

    /// <summary>rAthena <c>pc_skill_plagiarism</c> — Stalker copy.</summary>
    bool TryPlagiarize(PlayerEntity pc, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>pc_skill_plagiarism_reset</c>.</summary>
    void PlagiarismReset(PlayerEntity pc, byte type);

    /// <summary>rAthena <c>pc_validate_skill</c> — id + level bounds check.</summary>
    bool Validate(PlayerEntity pc, ushort skillId, int level);

    /// <summary>rAthena <c>pc_checkskill_imperial_guard</c>.</summary>
    int CheckImperialGuard(PlayerEntity pc, ushort skillId);

    /// <summary>rAthena <c>pc_checkskill_summoner</c>.</summary>
    int CheckSummoner(PlayerEntity pc, ushort skillType);
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
