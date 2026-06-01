namespace Map.Server.Status;

/// <summary>
/// SKILL-01 — mirror of rAthena <c>e_status_change_start_flags</c>
/// (status.hpp, <c>SCSTART_*</c>). Passed to the rate-aware
/// <see cref="IStatusChangeService.Start"/> / <see cref="IStatusChangeService.GetScDef"/>
/// to gate which parts of the <c>status_get_sc_def</c> resist pipeline run.
/// </summary>
[System.Flags]
public enum ScStartFlag
{
    None = 0,
    /// <summary><c>SCSTART_NORATEDEF</c> — skip the rate (landing-chance) resistance reduction.</summary>
    NoRateDef = 1,
    /// <summary><c>SCSTART_NOTICKDEF</c> — skip the duration (tick) resistance reduction.</summary>
    NoTickDef = 2,
    /// <summary><c>SCSTART_NOAVOID</c> — cannot be resisted/avoided regardless of stats (and bypasses boss/MVP resist).</summary>
    NoAvoid = 4,
    /// <summary><c>SCSTART_LOADED</c> — SC restored from a save (login); skip the apply roll.</summary>
    Loaded = 8,
}
