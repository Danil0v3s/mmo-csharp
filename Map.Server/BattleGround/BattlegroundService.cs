using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.BattleGround;

/// <summary>Default <see cref="IBattlegroundService"/>. Queue + team registry; per-BG mode rules data-pending on bg_db YAML.</summary>
public sealed class BattlegroundService : IBattlegroundService
{
    private int _nextId = 1;
    private readonly Dictionary<int, BgTeam> _teams = new();
    private readonly Dictionary<EntityId, int> _membership = new();
    private readonly ILogger<BattlegroundService> _logger;

    public BattlegroundService(ILogger<BattlegroundService> logger) => _logger = logger;

    public int Create(int mapIndex, byte side)
    {
        var id = _nextId++;
        _teams[id] = new BgTeam { Id = id, MapIndex = mapIndex, Side = side };
        return id;
    }

    public bool TeamJoin(int bgId, PlayerEntity pc)
    {
        if (!_teams.TryGetValue(bgId, out var t)) return false;
        t.Members.Add(pc.Id);
        _membership[pc.Id] = bgId;
        return true;
    }

    public int TeamLeave(PlayerEntity pc, byte flag)
    {
        if (!_membership.TryGetValue(pc.Id, out var bgId)) return 0;
        _membership.Remove(pc.Id);
        if (_teams.TryGetValue(bgId, out var t)) t.Members.Remove(pc.Id);
        return bgId;
    }

    public bool TeamDelete(int bgId)
    {
        if (!_teams.TryGetValue(bgId, out var t)) return false;
        foreach (var m in t.Members) _membership.Remove(m);
        _teams.Remove(bgId);
        return true;
    }

    public bool TeamWarp(int bgId, int mapIndex, short x, short y) => _teams.ContainsKey(bgId);
    public int TeamGetId(PlayerEntity pc) => _membership.GetValueOrDefault(pc.Id);
    public bool MemberRespawn(PlayerEntity pc) => _membership.ContainsKey(pc.Id);
    public bool PlayerIsInBgMap(PlayerEntity pc) => _membership.ContainsKey(pc.Id);
    public bool MapflagCheck(PlayerEntity pc, ushort skillId) => false;
    public PlayerEntity? GetAvailableSd(int bgId) => null;
    public bool QueueCheckJoinable(PlayerEntity pc, int queueId) => true;
    public bool QueueLeave(PlayerEntity pc) => false;
    public bool QueueOnReady(int queueId) => false;
    public bool QueueReservation(int queueId) => false;
    public void QueueClear(int queueId) { }
    public void QueueJoinSolo(PlayerEntity pc, string queueName) { }
    public void QueueJoinParty(PlayerEntity leader, string queueName) { }
    public void QueueJoinGuild(PlayerEntity leader, string queueName) { }
    public void QueueJoinMulti(PlayerEntity leader, string queueName, byte mode) { }
    public void QueueOnAcceptInvite(PlayerEntity pc, int queueId) { }
    public void QueueStartBattleground(int queueId) { }
    public void JoinActive(PlayerEntity pc) { }
    public void SendXyTimerSub(int bgId) { }
    public void SendDotRemove(PlayerEntity pc) { }
    public void SendMessage(int bgId, string sender, string text) { }

    private sealed class BgTeam
    {
        public int Id;
        public int MapIndex;
        public byte Side;
        public HashSet<EntityId> Members = new();
    }
}
