namespace Core.Database.Entities;

/// <summary>
/// Per-job per-weapon-type ASPD row (rAthena <c>db/re/job_aspd.yml</c>).
/// Loaded by the status calc to convert weapon-type → base ASPD delay
/// the job has with that weapon class. Lower delay = faster attacks.
///
/// Composite key on (<see cref="JobAegis"/>, <see cref="WeaponType"/>).
/// AT-G wave added this entity — DB-1..6 ported job_stats / job_exp
/// but skipped job_aspd.
/// </summary>
public class JobAspdDbEntity
{
    /// <summary>Job Aegis name (e.g. "Novice", "Swordman", "Knight", "Rune_Knight").</summary>
    public string JobAegis { get; set; } = string.Empty;

    /// <summary>
    /// Weapon class id (rAthena <c>enum weapon_type</c>). 0 = bare-hand,
    /// 1 = 1H sword, 2 = 2H sword, … same numbering as item_db's
    /// <c>subtype</c> for weapons.
    /// </summary>
    public int WeaponType { get; set; }

    /// <summary>
    /// Base ASPD delay in milliseconds the job has with this weapon
    /// type. Stat-calc subtracts AGI/DEX modifiers from this.
    /// </summary>
    public int BaseDelayMs { get; set; }
}
