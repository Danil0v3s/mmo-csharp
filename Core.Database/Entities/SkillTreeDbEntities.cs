namespace Core.Database.Entities;

/// <summary>
/// Per-job skill tree parent row (rAthena
/// <c>db/re/skill_tree.yml</c>). Each row is one job class; the
/// learnable skills hang off <see cref="SkillTreeEntryDbEntity"/>.
/// Inherits-from declarations live in
/// <see cref="SkillTreeInheritDbEntity"/>.
///
/// DB-8c wave replaces the prior <c>SkillTreeEntity :
/// PayloadStringKeyEntity</c> JSON blob.
/// </summary>
public class SkillTreeDbEntity
{
    /// <summary>Job Aegis name (e.g. "Novice", "Swordman", "Knight").</summary>
    public string JobAegis { get; set; } = string.Empty;
}

/// <summary>
/// Inheritance edge: child job inherits the skill tree of
/// <see cref="ParentJobAegis"/>. Composite key
/// (ChildJobAegis, ParentJobAegis). rAthena yml uses a per-row map
/// <c>Inherit: { Novice: true, Swordman: true }</c>; we flatten to
/// one row per child→parent edge.
/// </summary>
public class SkillTreeInheritDbEntity
{
    /// <summary>FK to <see cref="SkillTreeDbEntity.JobAegis"/> (the inheriting child).</summary>
    public string ChildJobAegis { get; set; } = string.Empty;
    /// <summary>The parent job whose skills are inherited.</summary>
    public string ParentJobAegis { get; set; } = string.Empty;
}

/// <summary>
/// One skill entry in a <see cref="SkillTreeDbEntity"/>. Composite
/// key (JobAegis, SkillAegis). Skill-level prerequisites live in
/// <see cref="SkillTreeRequirementDbEntity"/>.
/// </summary>
public class SkillTreeEntryDbEntity
{
    /// <summary>FK to <see cref="SkillTreeDbEntity.JobAegis"/>.</summary>
    public string JobAegis { get; set; } = string.Empty;
    /// <summary>Skill Aegis name (FK into skill_db).</summary>
    public string SkillAegis { get; set; } = string.Empty;
    /// <summary>Maximum skill level learnable by this job.</summary>
    public int MaxLevel { get; set; }
    /// <summary>
    /// rAthena <c>Exclude: true</c> — skill is *not* inherited by
    /// child classes (e.g. NV_TRICKDEAD is novice-only).
    /// </summary>
    public bool Exclude { get; set; }
}

/// <summary>
/// Skill prerequisite edge inside a <see cref="SkillTreeEntryDbEntity"/>.
/// Composite key (JobAegis, SkillAegis, RequiredSkillAegis). To learn
/// the parent skill, the player must have RequiredSkillAegis at level
/// ≥ <see cref="RequiredLevel"/>.
/// </summary>
public class SkillTreeRequirementDbEntity
{
    public string JobAegis { get; set; } = string.Empty;
    /// <summary>Skill whose prerequisite this row describes.</summary>
    public string SkillAegis { get; set; } = string.Empty;
    /// <summary>Required prerequisite skill Aegis.</summary>
    public string RequiredSkillAegis { get; set; } = string.Empty;
    /// <summary>Minimum level of the required skill.</summary>
    public int RequiredLevel { get; set; }
}

/// <summary>
/// Guild skill tree entry (rAthena <c>db/guild_skill_tree.yml</c>).
/// One row per guild skill (GD_*) with MaxLevel + optional Required[]
/// prerequisites in <see cref="GuildSkillTreeRequirementDbEntity"/>.
///
/// DB-8c wave replaces the prior <c>GuildSkillTreeEntity :
/// PayloadStringKeyEntity</c> JSON blob.
/// </summary>
public class GuildSkillTreeDbEntity
{
    /// <summary>Skill Aegis name (e.g. "GD_APPROVAL").</summary>
    public string SkillAegis { get; set; } = string.Empty;

    /// <summary>Maximum skill level learnable by the guild.</summary>
    public int MaxLevel { get; set; }
}

/// <summary>
/// Guild-skill prerequisite. Composite key
/// (SkillAegis, RequiredSkillAegis). Same semantics as
/// <see cref="SkillTreeRequirementDbEntity"/>.
/// </summary>
public class GuildSkillTreeRequirementDbEntity
{
    public string SkillAegis { get; set; } = string.Empty;
    public string RequiredSkillAegis { get; set; } = string.Empty;
    public int RequiredLevel { get; set; }
}
