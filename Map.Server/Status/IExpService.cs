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
    /// player. Returns true if a base- or job-level-up occurred.
    /// </summary>
    bool GainExp(PlayerEntity player, long baseExp, long jobExp);
}
