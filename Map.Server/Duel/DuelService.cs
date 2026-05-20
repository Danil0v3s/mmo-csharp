using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Duel;

/// <summary>
/// Default <see cref="IDuelService"/>. Transient in-memory duel
/// registry (no DB persistence — duels evaporate at process restart,
/// same as rAthena). The cooldown / last-time path stores its
/// timestamp on a per-account dictionary; when PC-side last_duel_time
/// surfaces on PlayerEntity it moves there.
/// </summary>
public sealed class DuelService : IDuelService
{
    private readonly Dictionary<int, DuelRoom> _rooms = new();
    private readonly Dictionary<int, long> _lastDuelTicks = new();
    private int _nextId = 1;
    private readonly ILogger<DuelService> _logger;

    public DuelService(ILogger<DuelService> logger) => _logger = logger;

    public int Create(PlayerEntity owner, int? maxMembers = null)
    {
        var id = _nextId++;
        _rooms[id] = new DuelRoom { Id = id, OwnerId = owner.AccountId, MaxMembers = maxMembers ?? 2 };
        _rooms[id].Members.Add(owner.AccountId);
        return id;
    }

    public bool Invite(PlayerEntity inviter, PlayerEntity invitee)
    {
        // rAthena: send the invite packet; the invitee replies via Accept/Reject.
        // First slice — invite tracking is implicit (the room is open until accept).
        return true;
    }

    public bool Accept(int duelId, PlayerEntity invitee)
    {
        if (!_rooms.TryGetValue(duelId, out var r)) return false;
        if (r.Members.Count >= r.MaxMembers) return false;
        r.Members.Add(invitee.AccountId);
        return true;
    }

    public bool Reject(int duelId, PlayerEntity invitee) => Exists(duelId);

    public bool Leave(int duelId, PlayerEntity who)
    {
        if (!_rooms.TryGetValue(duelId, out var r)) return false;
        r.Members.Remove(who.AccountId);
        if (r.Members.Count == 0) _rooms.Remove(duelId);
        return true;
    }

    public bool Exists(int duelId) => _rooms.ContainsKey(duelId);

    public bool CheckPlayerLimit(int duelId)
    {
        if (!_rooms.TryGetValue(duelId, out var r)) return false;
        return r.Members.Count < r.MaxMembers;
    }

    public bool CheckTime(PlayerEntity who)
    {
        // rAthena: duel_time_interval (battle_config) of cooldown
        // between duels. Default 60 sec. Honor that here.
        if (!_lastDuelTicks.TryGetValue(who.AccountId, out var last)) return true;
        return Environment.TickCount64 - last >= 60_000;
    }

    public void SaveTime(PlayerEntity who)
        => _lastDuelTicks[who.AccountId] = Environment.TickCount64;

    public string ShowInfo(int duelId)
    {
        if (!_rooms.TryGetValue(duelId, out var r)) return "(no such duel)";
        return $"Duel #{r.Id}: {r.Members.Count}/{r.MaxMembers} members";
    }

    private sealed class DuelRoom
    {
        public int Id;
        public int OwnerId;
        public int MaxMembers;
        public List<int> Members = new();
    }
}
