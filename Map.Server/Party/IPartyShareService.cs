using Map.Server.Entities;

namespace Map.Server.Party;

/// <summary>
/// Port of rAthena <c>party_exp_share</c> (party.cpp:1238). On a mob
/// kill, if the killer is in a party, split the EXP across eligible
/// party members on the same map (alive + non-idle). Solo kills (or
/// killers with PartyId = 0) fall through to the regular pc_gainexp.
/// </summary>
public interface IPartyShareService
{
    /// <summary>
    /// Distribute <paramref name="baseExp"/> + <paramref name="jobExp"/>
    /// among eligible members of <paramref name="killer"/>'s party.
    /// Returns true if the share path ran; false if the killer is solo
    /// or has no eligible partymates (the caller should then fall back
    /// to the single-player IExpService.GainExp path).
    /// </summary>
    bool ShareKill(PlayerEntity killer, long baseExp, long jobExp);
}
