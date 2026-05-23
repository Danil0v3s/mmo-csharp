namespace Core.Database.Entities;

/// <summary>
/// Script constant row (rAthena <c>db/const.yml</c>). Identifiers
/// usable inside NPC scripts that resolve to a fixed integer at
/// load-time. Examples: <c>SWORDCLAN=1</c>, <c>Job_Novice=0</c>,
/// <c>EAJ_NOVICE=0x00000001</c>, plus rAthena's <c>Parameter</c>
/// flag for the special handful of identifiers that proxy runtime
/// state via <c>pc_readparam</c> / <c>pc_setparam</c> (e.g. Zeny,
/// BaseLevel, JobLevel).
///
/// Composed of free-form Name → Value pairs; the script engine loads
/// the catalog at boot and exposes the constants as global symbols.
/// AT-G wave added this entity — DB-1..6 + TS+Jint pivot left
/// const.yml unported.
/// </summary>
public class ConstDbEntity
{
    /// <summary>
    /// Unique constant name. Must not collide with script commands,
    /// functions, or variables (rAthena loader rejects duplicates).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Constant value (signed 64-bit to accommodate bitmask flags).</summary>
    public long Value { get; set; }

    /// <summary>
    /// If true, the constant is a *parameter* — the script engine
    /// dispatches reads through <c>pc_readparam(value, sd)</c> and
    /// writes through <c>pc_setparam(value, sd, n)</c> instead of
    /// resolving to the literal Value. Used by Zeny, BaseLevel,
    /// JobLevel, StatusPoint, etc.
    /// </summary>
    public bool IsParameter { get; set; }
}
