namespace Map.Server.Party;

/// <summary>
/// Map-side in-memory party record. Port of rAthena <c>struct party_data</c>
/// (party.hpp:30 + party.cpp <c>party_db</c>). Distinct from
/// <see cref="Core.Database.Entities.PartyEntity"/> (the char-side DB row)
/// — this holds the live runtime view: who is currently online, what
/// their last-known map / level was, the EXP / item share flags, and the
/// leader-character id.
/// <para>Cached entries are created on first reference and refreshed by
/// <c>PartyService.Hydrate</c> after a <c>PartyInfo</c> RPC round-trip.
/// The cache is purely an accelerator over the authoritative char-side
/// row — losing it just costs one extra IPC round-trip.</para>
/// </summary>
public sealed class MapPartyEntity
{
    /// <summary>rAthena <c>MAX_PARTY</c> — hard cap on members per party.</summary>
    public const int MaxParty = 12;

    public int PartyId { get; }
    public string Name { get; set; } = string.Empty;

    /// <summary>rAthena <c>party.exp</c> — EXP share flag (0 = no share, 1 = even-share).</summary>
    public byte Exp { get; set; }

    /// <summary>rAthena <c>party.item</c> — loot share bitmask
    /// (0x1 = item even-share, 0x2 = mvp item even-share).</summary>
    public byte Item { get; set; }

    /// <summary>Character id of the party leader. Maps to
    /// <c>PartyEntity.LeaderChar</c> in the DB row.</summary>
    public int LeaderCharacterId { get; set; }

    /// <summary>Account id of the party leader.</summary>
    public int LeaderAccountId { get; set; }

    /// <summary>Online member list. Mirrors rAthena <c>party.member[]</c>
    /// keyed by character id (we don't slot-bind the way rAthena does;
    /// keying by char id keeps removals O(1) and avoids the slot
    /// reshuffle dance).</summary>
    public Dictionary<int, MapPartyMember> Members { get; } = new();

    /// <summary>rAthena <c>p->state.monk / sg / snovice / tk</c> bag —
    /// recomputed by <c>party_check_state</c>; consumed by
    /// <c>party_skill_check</c>.</summary>
    public PartyClassState ClassState { get; set; }

    public MapPartyEntity(int partyId) => PartyId = partyId;
}

/// <summary>One row of rAthena <c>party.member[]</c>. Holds only the
/// fields we read from the map side (the heavy state lives on the live
/// <c>PlayerEntity</c> when the member is online).</summary>
public sealed class MapPartyMember
{
    public int AccountId { get; set; }
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public uint Level { get; set; }
    public bool Online { get; set; }
    /// <summary>True if this member is the party leader (rAthena
    /// <c>member.leader</c>). Single-leader rule — exactly one
    /// member per party has this set.</summary>
    public bool Leader { get; set; }
}

/// <summary>rAthena <c>p-&gt;state</c> — which "trigger classes" are
/// currently in the party. Read by <c>party_skill_check</c> to gate
/// passive party skills.</summary>
public struct PartyClassState
{
    /// <summary>Monk / Sura / Champion / Inquisitor — gates TK_COUNTER's MO_TRIPLEATTACK buff.</summary>
    public bool Monk { get; set; }
    /// <summary>Star Gladiator / Star Emperor / Sky Emperor — gates MO_COMBOFINISH's TK_COUNTER buff.</summary>
    public bool StarGladiator { get; set; }
    /// <summary>Super Novice / Hyper Novice — return-flag for AM_TWILIGHT2.</summary>
    public bool SuperNovice { get; set; }
    /// <summary>Taekwon — return-flag for AM_TWILIGHT3.</summary>
    public bool Taekwon { get; set; }
}
