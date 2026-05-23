namespace Map.Server.Skills;

/// <summary>
/// rAthena per-job skill tree (db/skill_tree.yml). Read-only cache
/// loaded from <see cref="Core.Database.Repositories.Api.ISkillTreeDbRepository"/>
/// at boot. Drives <c>pc_calc_skilltree</c> per-job MaxLevel overrides,
/// inheritance walks (Inherit + Exclude), and <c>skill_check_requirement</c>
/// prerequisite checks.
///
/// DBR-1f: replaces the prior "consult skill_db.MaxLevel only" path in
/// <see cref="PlayerSkillService"/>. The C# port's skill_db carries
/// the *global* cap; this service overlays the per-job cap (often
/// lower or 0=not-learnable) and the inheritance walk so a Knight's
/// effective skill table is the union of Novice+Swordman+Knight
/// entries minus any Exclude-flagged rows.
/// </summary>
public interface ISkillTreeService
{
    /// <summary>
    /// rAthena <c>pc_skilltree_get_max</c>. Per-job effective max level
    /// for <paramref name="skillAegis"/>, walking the Inherit chain and
    /// honoring <c>Exclude: true</c>. Returns 0 when the job (or any
    /// parent in its chain) does not list the skill — caller falls back
    /// to <see cref="ISkillDb.GetMaxLevel"/> for global validation.
    /// </summary>
    int GetMaxLevel(string jobAegis, string skillAegis);

    /// <summary>
    /// rAthena <c>pc_skill_check_requirement</c>. True if every
    /// prerequisite of <paramref name="skillAegis"/> for this job is
    /// learned at the required level. Walks the Inherit chain so a
    /// child job sees its parents' prereq rows.
    /// </summary>
    bool CheckRequirements(string jobAegis, string skillAegis, System.Collections.Generic.IReadOnlyDictionary<string, int> learnedSkillsByAegis);

    /// <summary>True iff the job tree (post-inherit) lists this skill at all.</summary>
    bool IsLearnable(string jobAegis, string skillAegis);

    /// <summary>True iff any job tree was loaded from the DB.</summary>
    bool HasData { get; }
}
