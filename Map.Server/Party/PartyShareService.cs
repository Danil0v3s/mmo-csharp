using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Party;

/// <summary>
/// First-slice port of rAthena <c>party_exp_share</c>
/// (party.cpp:1238). Walks the entity registry for PCs in the same
/// party, on the same map, alive — divides EXP across them, applies
/// the even-share bonus (battle_config.party_even_share_bonus =
/// +10% per member by default), then hands off to
/// <see cref="IExpService"/> per recipient.
///
/// Eligibility:
/// - Same party id (non-zero)
/// - Same map
/// - Hp &gt; 0 (rAthena: <c>!pc_isdead(sd)</c>)
/// </summary>
public sealed class PartyShareService : IPartyShareService
{
    /// <summary>rAthena <c>battle_config.party_even_share_bonus</c> default = 10% per extra member.</summary>
    private const int EvenShareBonusPercentPerMember = 10;

    private readonly IEntityRegistry _entities;
    private readonly IExpService _exp;
    private readonly ILogger<PartyShareService> _logger;

    public PartyShareService(IEntityRegistry entities, IExpService exp, ILogger<PartyShareService> logger)
    {
        _entities = entities;
        _exp = exp;
        _logger = logger;
    }

    public bool ShareKill(PlayerEntity killer, long baseExp, long jobExp, int? mobLevel = null)
    {
        if (killer.PartyId <= 0) return false;

        // Collect eligible members. rAthena reads from party_data->data[];
        // we walk the entity registry filtered by party id. Same result
        // for in-map members — out-of-map party members don't get EXP.
        var eligible = new List<PlayerEntity>();
        foreach (var e in _entities.All())
        {
            if (e is not PlayerEntity pc) continue;
            if (pc.PartyId != killer.PartyId) continue;
            if (pc.MapId != killer.MapId) continue;
            if (pc.Hp <= 0) continue;
            eligible.Add(pc);
        }

        if (eligible.Count == 0) return false;
        if (eligible.Count == 1)
        {
            // Solo-from-party-perspective — no split needed. Hand back to caller.
            return false;
        }

        var perMemberBase = baseExp / eligible.Count;
        var perMemberJob = jobExp / eligible.Count;

        // Even-share bonus: rAthena multiplies the per-member share by
        // (100 + bonus*(N-1))/100 to encourage grouping.
        var bonus = 100 + EvenShareBonusPercentPerMember * (eligible.Count - 1);
        perMemberBase = perMemberBase * bonus / 100;
        perMemberJob = perMemberJob * bonus / 100;

        foreach (var member in eligible)
        {
            // DBR-1b: forward mobLevel so each member's GainExp applies
            // the per-recipient level-gap penalty (rAthena pc.cpp:8186).
            _exp.GainExp(member, perMemberBase, perMemberJob, mobLevel);
        }
        _logger.LogDebug(
            "Party {Party} EXP share: {N} members, {Base} base + {Job} job each (×{Bonus}%)",
            killer.PartyId, eligible.Count, perMemberBase, perMemberJob, bonus);
        return true;
    }
}
