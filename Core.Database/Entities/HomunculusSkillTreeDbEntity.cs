namespace Core.Database.Entities;

/// <summary>
/// Per-homunculus-class skill tree (rAthena <c>homunculus_db.yml</c>
/// nested <c>SkillTree:</c> array). Composite key on (ClassAegis,
/// SkillId). DB-1..6 ported the top-level homun_db row but missed
/// this nested table; AT-F adds it so
/// <c>IHomunculusService.SkillTreeGetMax / CalcSkillTree</c> read
/// from real data instead of an inline bake.
/// </summary>
public class HomunculusSkillTreeDbEntity
{
    /// <summary>FK to <see cref="HomunculusDbEntity.ClassAegis"/>.</summary>
    public string ClassAegis { get; set; } = string.Empty;
    /// <summary>rAthena skill id (numeric resolution of the Aegis name).</summary>
    public ushort SkillId { get; set; }
    /// <summary>Skill Aegis name from the YAML (e.g. "HLIF_HEAL").</summary>
    public string SkillAegis { get; set; } = string.Empty;
    public ushort MaxLevel { get; set; }
    public ushort RequiredLevel { get; set; }
    public ushort RequiredIntimacy { get; set; }
    public bool RequireEvolution { get; set; }
}
