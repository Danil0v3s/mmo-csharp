namespace Map.Server.Guild;

/// <summary>
/// rAthena guild_skill_tree.yml (GD_*). Read-only cache loaded from
/// <see cref="Core.Database.Repositories.Api.IGuildSkillTreeDbRepository"/>
/// at boot. Replaces the in-process <c>SkillMaxLevels</c> hardcoded
/// dictionary in <c>GuildService</c> with the YAML-true table.
///
/// DBR-1f: per-skill max + prereq lookups consult this service. The
/// service caches by skill aegis (GD_APPROVAL, GD_KAFRACONTRACT, …)
/// since the rAthena yml is keyed by aegis; the guild runtime uses
/// numeric ushort skill IDs, so callers translate via
/// <see cref="Map.Server.Skills.ISkillDb.Name2Id"/> or a hand-built
/// well-known table.
/// </summary>
public interface IGuildSkillTreeService
{
    /// <summary>rAthena <c>guild_skill_get_max</c> by aegis. 0 = unknown.</summary>
    ushort GetMaxLevel(string skillAegis);

    /// <summary>rAthena <c>guild_skill_get_max</c> by numeric skill id.</summary>
    ushort GetMaxLevel(ushort skillId);

    /// <summary>
    /// rAthena <c>guild_check_skill_require</c>. True if every prereq
    /// in the yml row is met by <paramref name="learnedSkillsByAegis"/>.
    /// Returns true when the skill has no prereq entries (vacuous).
    /// </summary>
    bool CheckRequirements(string skillAegis, System.Collections.Generic.IReadOnlyDictionary<string, int> learnedSkillsByAegis);

    /// <summary>
    /// rAthena <c>guild_check_skill_require</c> by numeric id —
    /// translates GD_* IDs to aegis internally.
    /// </summary>
    bool CheckRequirements(ushort skillId, System.Collections.Generic.IReadOnlyDictionary<ushort, int> learnedSkillsById);

    /// <summary>True iff any data was loaded.</summary>
    bool HasData { get; }
}
