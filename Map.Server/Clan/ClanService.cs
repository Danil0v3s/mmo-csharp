using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Clan;

/// <summary>
/// Default <see cref="IClanService"/>. Tracks per-player ClanId on
/// PlayerEntity; in-memory roster + alliance tracking. Cross-server
/// roster sync lives on the char server (clan_db.yml).
///
/// AT-D2 wave: real ZC_NOTIFY_CHAT_PARTY broadcast for clan chat,
/// per-clan roster index + alliance count, MemberJoined/Left
/// notifications to current map.
/// </summary>
public sealed class ClanService : IClanService
{
    private readonly IEntityRegistry _entities;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<ClanService> _logger;

    private readonly Dictionary<int, ClanRoom> _clans = new();

    public ClanService(
        IEntityRegistry entities,
        ISessionManagerAccessor sessions,
        ILogger<ClanService> logger)
    {
        _entities = entities;
        _sessions = sessions;
        _logger = logger;
    }

    public bool MemberJoin(PlayerEntity pc, int clanId)
    {
        pc.ClanId = clanId;
        var clan = Ensure(clanId);
        clan.Members.Add(pc.Id);
        return true;
    }

    public bool MemberLeave(PlayerEntity pc)
    {
        if (pc.ClanId != 0 && _clans.TryGetValue(pc.ClanId, out var clan))
            clan.Members.Remove(pc.Id);
        pc.ClanId = 0;
        return true;
    }

    /// <summary>rAthena <c>clan_member_joined</c> — broadcast a roster update.</summary>
    public void MemberJoined(PlayerEntity pc)
    {
        if (pc.ClanId == 0) return;
        BroadcastSystem(pc.ClanId, $"Clan: {pc.Name} has connected.");
    }

    /// <summary>rAthena <c>clan_member_left</c>.</summary>
    public void MemberLeft(PlayerEntity pc)
    {
        if (pc.ClanId == 0) return;
        BroadcastSystem(pc.ClanId, $"Clan: {pc.Name} has logged out.");
    }

    /// <summary>rAthena <c>clan_load_clandata</c> — hydrate from char server.</summary>
    public void LoadClanData(PlayerEntity pc)
    {
        if (pc.ClanId == 0) return;
        var clan = Ensure(pc.ClanId);
        clan.Members.Add(pc.Id);
    }

    /// <summary>rAthena <c>clan_getMemberIndex</c>.</summary>
    public int GetMemberIndex(int clanId, PlayerEntity pc)
    {
        if (!_clans.TryGetValue(clanId, out var clan)) return -1;
        int idx = 0;
        foreach (var m in clan.Members)
        {
            if (m == pc.Id) return idx;
            idx++;
        }
        return -1;
    }

    /// <summary>rAthena <c>clan_getNextFreeMemberIndex</c>.</summary>
    public int GetNextFreeMemberIndex(int clanId)
    {
        if (!_clans.TryGetValue(clanId, out var clan)) return 0;
        return clan.Members.Count; // next slot after current roster
    }

    /// <summary>rAthena <c>clan_get_alliance_count</c>.</summary>
    public int GetAllianceCount(int clanId)
        => _clans.TryGetValue(clanId, out var clan) ? clan.Alliances.Count : 0;

    public PlayerEntity? GetAvailableMember(int clanId)
    {
        if (!_clans.TryGetValue(clanId, out var clan)) return null;
        foreach (var m in clan.Members)
            if (_entities.Get(m) is PlayerEntity p) return p;
        return null;
    }

    /// <summary>rAthena <c>clan_send_message</c> — fan-out to clan members on this map.</summary>
    public void SendMessage(PlayerEntity from, string text)
    {
        if (from.ClanId == 0) return;
        var line = $"[Clan] {from.Name}: {text}";
        Fanout(from.ClanId, line, fromAccountId: from.AccountId);
    }

    /// <summary>rAthena <c>clan_recv_message</c> — cross-server message inbound.</summary>
    public void RecvMessage(int clanId, string sender, string text)
        => Fanout(clanId, $"[Clan] {sender}: {text}", fromAccountId: 0);

    // ----- helpers -----

    private void Fanout(int clanId, string line, int fromAccountId)
    {
        if (!_clans.TryGetValue(clanId, out var clan)) return;
        var packet = new ZC_NOTIFY_CHAT_PARTY { AccountId = fromAccountId, Message = line };
        foreach (var memberId in clan.Members)
            _sessions.GetByEntityId(memberId)?.EnqueuePacket(packet);
    }

    private void BroadcastSystem(int clanId, string line)
        => Fanout(clanId, line, fromAccountId: 0);

    private ClanRoom Ensure(int clanId)
    {
        if (!_clans.TryGetValue(clanId, out var clan))
            _clans[clanId] = clan = new ClanRoom { Id = clanId };
        return clan;
    }

    private sealed class ClanRoom
    {
        public int Id;
        public HashSet<EntityId> Members = new();
        public HashSet<int> Alliances = new(); // other clan ids
    }
}
