using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Port of rAthena <c>pc_gainexp</c> (pc.cpp:8314). Adds base + job
/// EXP to a player, walks the level-up chain
/// (<c>pc_checkbaselevelup</c>/<c>pc_checkjoblevelup</c>), full-heals on
/// each level-up, and broadcasts the right <c>ZC_LONGLONGPAR_CHANGE</c> /
/// <c>ZC_PAR_CHANGE</c> stream so the client UI stays current.
/// </summary>
public interface IExpService
{
    /// <summary>
    /// Grant <paramref name="baseExp"/> + <paramref name="jobExp"/> to the
    /// player. Returns true if a base- or job-level-up occurred. When
    /// <paramref name="mobLevel"/> is provided the level-gap penalty
    /// (rAthena <c>pc_level_penalty_mod(diff, PENALTY_EXP)</c>) is
    /// applied to both EXP buckets before they're added; a null
    /// <paramref name="mobLevel"/> (e.g. quest reward, GM grant) skips
    /// the penalty entirely.
    /// </summary>
    bool GainExp(PlayerEntity player, long baseExp, long jobExp, int? mobLevel = null);

    /// <summary>
    /// rAthena <c>pc_lostexp</c> (pc.cpp:8425) — subtract base+job exp
    /// without dropping below 0. Used by death penalty path + GM
    /// commands. Returns the actual amount drained.
    /// </summary>
    (long BaseLost, long JobLost) LoseExp(PlayerEntity player, long baseExp, long jobExp);

    /// <summary>
    /// rAthena <c>pc_baselevelchanged</c> (pc.cpp:8499) — hook called
    /// after every base-level mutation. Default impl runs the
    /// dragon/eleanor/babyclass auto-grant chain; the first slice just
    /// re-emits the SP_BASELEVEL packet so the client UI updates.
    /// </summary>
    void OnBaseLevelChanged(PlayerEntity player);
}
