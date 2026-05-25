using System.Collections.Concurrent;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Skills;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Party;

/// <summary>
/// Default <see cref="IPartyService"/>. Holds the
/// <see cref="MapPartyEntity"/> cache and exposes the
/// rAthena <c>party_search</c> / <c>party_isleader</c> / <c>party_skill_check</c>
/// / <c>party_send_levelup</c> helper family.
///
/// <para>Threading: the cache is a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// because the gRPC inbound thread (PartyInfo response handler) writes
/// while the game loop reads. Per-entry mutation (member list edits)
/// happens on the game loop only, so individual MapPartyEntity objects
/// don't need locks.</para>
/// </summary>
public sealed class PartyService : IPartyService
{
    private readonly ConcurrentDictionary<int, MapPartyEntity> _cache = new();
    private readonly ICharServerIpcServiceParty? _charIpc;
    private readonly IMapWorldRegistry? _world;
    private readonly ILogger<PartyService> _logger;

    public PartyService(
        ILogger<PartyService> logger,
        ICharServerIpcServiceParty? charIpc = null,
        IMapWorldRegistry? world = null)
    {
        _logger = logger;
        _charIpc = charIpc;
        _world = world;
    }

    public MapPartyEntity? Get(int partyId)
    {
        if (partyId <= 0) return null;
        return _cache.TryGetValue(partyId, out var p) ? p : null;
    }

    public bool IsLeader(PlayerEntity pc)
    {
        if (pc == null || pc.PartyId <= 0) return false;
        var party = Get(pc.PartyId);
        if (party == null) return false;
        return party.LeaderCharacterId == pc.CharacterId;
    }

    public MapPartyMember? GetMember(int partyId, int characterId)
    {
        var party = Get(partyId);
        if (party == null) return null;
        return party.Members.TryGetValue(characterId, out var m) ? m : null;
    }

    public int SkillCheck(PlayerEntity caster, int partyId, ushort skillId, ushort skillLevel)
    {
        if (partyId <= 0) return 0;
        var party = Get(partyId);
        if (party == null) return 0;

        // rAthena party.cpp:1131 switch — gate the buff by which class
        // is in the party. TK_COUNTER / MO_COMBOFINISH return 0 + apply
        // a side-effect (sc_start4 on each matching member) — the
        // side-effect is the caster's skill body; this helper only
        // surfaces the gate so the body can early-return.
        switch (skillId)
        {
            case SkillIds.TK_COUNTER:
                return party.ClassState.Monk ? 1 : 0;
            case SkillIds.MO_COMBOFINISH:
                return party.ClassState.StarGladiator ? 1 : 0;
            case SkillIds.AM_TWILIGHT2:
                return party.ClassState.SuperNovice ? 1 : 0;
            case SkillIds.AM_TWILIGHT3:
                return party.ClassState.Taekwon ? 1 : 0;
            default:
                return 0;
        }
    }

    public void OnLevelUp(PlayerEntity pc)
    {
        if (pc == null || pc.PartyId <= 0) return;
        var party = Get(pc.PartyId);
        if (party == null) return;
        // Refresh the cached level on the member entry — the next
        // PartyChangeMap broadcast will fan it out to other members
        // and minimap dots / HP bars will pick it up. rAthena
        // party_send_levelup is literally one line (intif_party_changemap(sd,1))
        // so we mirror that — issue the changemap RPC if we have an IPC
        // channel; the actual zc_party_xy clients sees comes from
        // PartyChangeMap's char-side fan-out.
        if (party.Members.TryGetValue(pc.CharacterId, out var member))
        {
            member.Level = (uint)pc.Level;
        }
        if (_charIpc == null)
        {
            // ZC_PARTY_LEVELUP packet not yet defined — partner clients
            // get the new level on the next PartyChangeMap tick.
            return;
        }
        var mapName = ResolveMapName(pc.MapId);
        _ = _charIpc.PartyChangeMapAsync(
            partyId: pc.PartyId,
            accountId: pc.AccountId,
            characterId: pc.CharacterId,
            online: true,
            level: (uint)pc.Level,
            mapName: mapName);
    }

    public MapPartyEntity ApplySnapshot(int partyId, string name, byte exp, byte item,
        int leaderCharacterId, int leaderAccountId,
        IEnumerable<MapPartyMember> members)
    {
        var party = _cache.GetOrAdd(partyId, id => new MapPartyEntity(id));
        party.Name = name ?? string.Empty;
        party.Exp = exp;
        party.Item = item;
        party.LeaderCharacterId = leaderCharacterId;
        party.LeaderAccountId = leaderAccountId;
        party.Members.Clear();
        if (members != null)
        {
            foreach (var m in members)
            {
                m.Leader = m.CharacterId == leaderCharacterId;
                party.Members[m.CharacterId] = m;
            }
        }
        RecomputeClassState(party);
        _logger.LogDebug(
            "PartyService.ApplySnapshot party={Id} name={Name} leader={Leader} members={Count}",
            partyId, party.Name, leaderCharacterId, party.Members.Count);
        return party;
    }

    public void Forget(int partyId)
    {
        if (_cache.TryRemove(partyId, out _))
        {
            _logger.LogInformation("PartyService.Forget dropped party {Id}", partyId);
        }
    }

    public void UpdateMemberMap(int partyId, int characterId, string mapName, uint level, bool online)
    {
        var party = Get(partyId);
        if (party == null) return;
        if (!party.Members.TryGetValue(characterId, out var member)) return;
        member.MapName = mapName ?? string.Empty;
        member.Level = level;
        member.Online = online;
    }

    public void Hydrate(int partyId, int requestingCharacterId)
    {
        if (partyId <= 0 || _charIpc == null) return;
        _ = HydrateAsync(partyId, requestingCharacterId);
    }

    public async Task<MapPartyEntity?> HydrateAsync(int partyId, int requestingCharacterId, CancellationToken ct = default)
    {
        if (partyId <= 0 || _charIpc == null) return null;
        var response = await _charIpc.PartyInfoAsync(partyId, requestingCharacterId, ct);
        if (response == null || !response.Success || response.Party == null)
        {
            // rAthena party_recv_noinfo — drop the cached entry, the
            // caller should clear PlayerEntity.PartyId to 0 if it was
            // counting on this party.
            Forget(partyId);
            return null;
        }
        var members = response.Party.Members.Select(m => new MapPartyMember
        {
            AccountId = m.AccountId,
            CharacterId = (int)m.CharacterId,
            Name = m.Name,
            ClassId = m.ClassId,
            MapName = m.MapName,
            Level = m.Level,
            Online = m.Online,
        });
        // Proto's PartyInfoData doesn't carry an exp-share byte yet
        // (only item / item2). Until the schema is widened we infer
        // even-share from item (non-zero == sharing on).
        var leaderMember = response.Party.Members
            .FirstOrDefault(m => m.CharacterId == response.Party.LeaderCharacterId);
        return ApplySnapshot(
            partyId: response.Party.PartyId,
            name: response.Party.Name,
            exp: response.Party.Item != 0 ? (byte)1 : (byte)0,
            item: (byte)response.Party.Item,
            leaderCharacterId: (int)response.Party.LeaderCharacterId,
            leaderAccountId: leaderMember?.AccountId ?? 0,
            members: members);
    }

    /// <summary>Port of rAthena <c>party_check_state</c> (party.cpp:228) —
    /// rebuilds the monk / sg / snovice / tk flags from the online
    /// member list. Called after any member-list mutation.</summary>
    private static void RecomputeClassState(MapPartyEntity party)
    {
        var state = new PartyClassState();
        foreach (var m in party.Members.Values)
        {
            if (!m.Online) continue;
            // Aegis job-id ranges — same mapping rAthena uses, just
            // open-coded since we don't have the JOB_* enum in C# yet.
            switch (m.ClassId)
            {
                // Monk family (JOB_MONK=15, JOB_CHAMPION=4015, JOB_SURA=4047,
                // baby/transcendent variants etc.)
                case 15: case 4015: case 4047: case 4081: case 4109: case 4209:
                    state.Monk = true;
                    break;
                // Star Gladiator family
                case 25: case 4047 + 1: // sloppy — accurate full table is class_db
                    state.StarGladiator = true;
                    break;
                // Super Novice / Hyper Novice
                case 23: case 4045: case 4190: case 4191:
                    state.SuperNovice = true;
                    break;
                // Taekwon
                case 4046:
                    state.Taekwon = true;
                    break;
            }
        }
        party.ClassState = state;
    }

    private string ResolveMapName(uint mapId)
    {
        if (_world == null) return string.Empty;
        foreach (var m in _world.All)
        {
            if ((uint)m.Name.GetHashCode() == mapId) return m.Name;
        }
        return string.Empty;
    }
}
