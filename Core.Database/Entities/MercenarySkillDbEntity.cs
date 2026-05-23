namespace Core.Database.Entities;

/// <summary>
/// Per-mercenary-class skill grant (rAthena <c>mercenary_db.yml</c>
/// nested <c>Skills:</c> array). Composite key on (MercId, SkillId).
/// DB-1..6 ported the top-level merc_db row but missed this nested
/// table; AT-F adds it so <c>IMercenaryService.CheckSkill</c> reads
/// from real data instead of an inline bake.
/// </summary>
public class MercenarySkillDbEntity
{
    /// <summary>FK to <see cref="MercenaryDbEntity.MercId"/>.</summary>
    public uint MercId { get; set; }
    /// <summary>rAthena skill id (numeric resolution of the Aegis name).</summary>
    public ushort SkillId { get; set; }
    /// <summary>Skill Aegis name from the YAML (e.g. "MER_QUICKEN").</summary>
    public string SkillAegis { get; set; } = string.Empty;
    public ushort MaxLevel { get; set; }
}
