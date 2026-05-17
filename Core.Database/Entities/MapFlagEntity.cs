namespace Core.Database.Entities;

/// <summary>
/// A per-map property flag. Ported from rAthena's
/// <c>npc/re/mapflag/*.txt</c> files:
/// <c>mapname &lt;TAB&gt; mapflag &lt;TAB&gt; flag &lt;TAB&gt; [value]</c>.
///
/// rAthena's flag set is huge (~50 distinct flags including pvp,
/// gvg, gvg_castle, nopvp, restricted, nowarpto, nosave, noteleport,
/// nobranch, nomemo, nopenalty, hidemobhpbar, town, night, …) and
/// some take an optional value (numeric restriction tier, "off",
/// or a comma list). Stored verbatim from the file so the runtime
/// flag-parser can interpret per-flag semantics.
/// </summary>
public class MapFlagEntity
{
    public int FlagId { get; set; }

    public string MapName { get; set; } = string.Empty;

    /// <summary>Flag name (rAthena <c>mapflag</c> column 3): pvp, gvg, restricted, nosave, etc.</summary>
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Optional value column. Empty when the flag is a pure toggle
    /// (e.g. <c>town</c>). For toggleable flags can be <c>"off"</c>.
    /// For tiered flags carries a number ("restricted 6"). For
    /// list-valued flags carries the comma list verbatim
    /// ("nosave SavePoint,prontera,200,200").
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
