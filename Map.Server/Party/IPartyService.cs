using Map.Server.Entities;

namespace Map.Server.Party;

/// <summary>
/// Map-side party-cache facade. Port of the rAthena <c>party_db</c>
/// (party.cpp:67) + the <c>party_search</c> / <c>party_isleader</c> /
/// <c>party_getmemberid</c> / <c>party_skill_check</c> helper family.
///
/// <para>The cache is **not** authoritative — the canonical row lives
/// char-side on <c>PartyEntity</c>. This service holds the live runtime
/// projection (who is online on this map / what their share flags
/// are) and hands it to gameplay code.</para>
///
/// <para>Hydrate-on-demand: when a caller asks for a party id that's
/// not in the cache, the service queues an async <c>PartyInfo</c> RPC
/// and creates a stub entry (so concurrent callers don't double-fetch).
/// The cache is filled when the RPC response lands via <see cref="ApplySnapshot"/>.</para>
/// </summary>
public interface IPartyService
{
    /// <summary>rAthena <c>party_search</c>. Returns the cached entry
    /// for <paramref name="partyId"/> or null if not yet hydrated.</summary>
    MapPartyEntity? Get(int partyId);

    /// <summary>rAthena <c>party_isleader</c> (party.cpp:474). True if
    /// <paramref name="pc"/> is the current leader of its own party.
    /// Returns false for solo players and missed-cache lookups.</summary>
    bool IsLeader(PlayerEntity pc);

    /// <summary>rAthena <c>party_getmemberid</c> (party.cpp:1044). Looks
    /// up the slot index for a character id; -1 when not in the party.
    /// Map-side equivalent — returns the live <see cref="MapPartyMember"/>
    /// or null.</summary>
    MapPartyMember? GetMember(int partyId, int characterId);

    /// <summary>rAthena <c>party_skill_check</c> (party.cpp:1122). Gates
    /// the "needs a teammate of class X" passive party skills
    /// (TK_COUNTER, MO_COMBOFINISH, AM_TWILIGHT2/3). For the partial-
    /// implementation skills it just returns whether the trigger class
    /// is present — full proc dispatch (sc_start4 on each Monk / SG
    /// in the party) lives in the caster's skill body.</summary>
    int SkillCheck(PlayerEntity caster, int partyId, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>party_send_levelup</c> (party.cpp:1075). When
    /// a party member levels (base or job), re-broadcast their position +
    /// level via <c>PartyChangeMap</c> so other members' minimap dot /
    /// HP bar refreshes pick up the new level.</summary>
    void OnLevelUp(PlayerEntity pc);

    /// <summary>Hydrate the cache from a char-server <c>PartyInfo</c>
    /// snapshot. Replaces any cached entry. rAthena equivalent:
    /// <c>party_recv_info</c> (party.cpp:268) — the diff /
    /// removed-member / added-member fan-out is the caller's job (or
    /// a separate handler); this method only updates the cache.</summary>
    MapPartyEntity ApplySnapshot(int partyId, string name, byte exp, byte item,
        int leaderCharacterId, int leaderAccountId,
        IEnumerable<MapPartyMember> members);

    /// <summary>rAthena <c>party_recv_noinfo</c> (party.cpp:213). Drops
    /// the cached entry — used when char-side replies "party doesn't
    /// exist."</summary>
    void Forget(int partyId);

    /// <summary>Mark a member's online flag + last-known map. Mirrors
    /// rAthena <c>party_recv_movemap</c> (party.cpp:1013); the
    /// authoritative side already saved the row, this just refreshes
    /// the cached projection.</summary>
    void UpdateMemberMap(int partyId, int characterId, string mapName, uint level, bool online);

    /// <summary>rAthena <c>party_request_info</c> (party.cpp:178) —
    /// async PartyInfo RPC that populates the cache. Fire-and-forget;
    /// completion lands via <see cref="ApplySnapshot"/>. Callers that
    /// need to await the round-trip can use <see cref="HydrateAsync"/>.</summary>
    void Hydrate(int partyId, int requestingCharacterId);

    /// <summary>Awaitable variant of <see cref="Hydrate"/>. Returns the
    /// freshly-populated <see cref="MapPartyEntity"/> when the char-side
    /// info round-trip completes, or null when the party doesn't exist
    /// (rAthena <c>party_recv_noinfo</c> branch).</summary>
    Task<MapPartyEntity?> HydrateAsync(int partyId, int requestingCharacterId, CancellationToken ct = default);
}
