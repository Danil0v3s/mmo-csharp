namespace Core.Database.Entities;

/// <summary>
/// rAthena <c>mob_skill_db</c> table — one row per (mob, skill) pair
/// the mob can cast. Schema mirrors rAthena's pre-generated SQL
/// (sql-files/mob_skill_db_re.sql) so the seed script imports
/// without modification.
///
/// Per-row fields:
/// <list type="bullet">
///   <item><c>MobId</c> + <c>Info</c> identify the (mob, skill) pair;
///         <c>Info</c> is "MobName@SKILL_NAME" — read-only metadata.</item>
///   <item><c>State</c> — AI state the skill activates in (idle / attack /
///         chase / dead / loot / anytarget / …).</item>
///   <item><c>SkillId</c> + <c>SkillLv</c> point at <c>skill_db</c>.</item>
///   <item><c>Rate</c> (0..10000) — % chance to cast when conditions met.</item>
///   <item><c>CastTime</c> + <c>Delay</c> — milliseconds.</item>
///   <item><c>Cancelable</c> — "yes" / "no".</item>
///   <item><c>Target</c> — "target" / "self" / "friend" / "master" /
///         "randomtarget" / "around1..around8".</item>
///   <item><c>Condition</c> + <c>ConditionValue</c> — see mob_skill_db.txt
///         schema (rAthena db/re/mob_skill_db.txt). Examples:
///         "always", "myhpltmaxrate", "longrangeattacked", "skillused", …</item>
///   <item><c>Val1..Val5</c> — skill-specific argument bag.</item>
///   <item><c>Emotion</c> + <c>Chat</c> — visual / dialogue overlay.</item>
/// </list>
///
/// Composite key: (MobId, Info) — rAthena uses MyISAM with no PK, but
/// the (mob, skill-name) pair is unique enough for our EF mapping.
/// </summary>
public class MobSkillDbEntity
{
    public short MobId { get; set; }
    public string Info { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public short SkillId { get; set; }
    public byte SkillLv { get; set; }
    public short Rate { get; set; }
    public int CastTime { get; set; }
    public int Delay { get; set; }
    public string Cancelable { get; set; } = "no";
    public string Target { get; set; } = "target";
    public string Condition { get; set; } = "always";
    public string? ConditionValue { get; set; }
    public int? Val1 { get; set; }
    public int? Val2 { get; set; }
    public int? Val3 { get; set; }
    public int? Val4 { get; set; }
    public int? Val5 { get; set; }
    public string? Emotion { get; set; }
    public string? Chat { get; set; }
}
