using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.BattleGround;

/// <summary>
/// Default <see cref="IBattlegroundService"/> — port of rAthena
/// <c>battleground.cpp</c>. Includes a minimal queue state machine
/// (SETUP → SETUP_DELAY → ACTIVE → ENDED) keyed by named-queue
/// (bgsmall/bgmedium/bglarge/bg). DBR-0: BG map pool now sourced from
/// battleground_location_db (DB-8f typed catalog, seeded from
/// db/battleground_db.yml). See
/// [battleground-parity.md](../../../.agents/migrations/map/battleground-parity.md).
/// </summary>
public sealed class BattlegroundService : IBattlegroundService
{
    private int _nextId = 1;
    private int _nextQueueId = 1;
    private readonly Dictionary<int, BgTeam> _teams = new();
    private readonly Dictionary<EntityId, int> _membership = new();
    private readonly Dictionary<int, BgQueue> _queues = new();
    private readonly Dictionary<string, int> _queueByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<EntityId, int> _playerQueue = new();
    private readonly IEntityRegistry? _entities;
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<BattlegroundService> _logger;

    /// <summary>
    /// BG map pool (rAthena db/battleground_db.yml Locations:[]). DBR-0:
    /// loaded from battleground_location_db at boot. Reserved per-queue
    /// at SETUP_DELAY; freed at ENDED. Empty list = no maps configured
    /// (QueueReservation returns false → caller falls back to requeue).
    /// </summary>
    private readonly List<string> _bgMapPool = new();
    private readonly HashSet<string> _reservedMaps = new();

    public BattlegroundService(IEntityRegistry entities, IServiceScopeFactory scopes, ILogger<BattlegroundService> logger)
    {
        _entities = entities;
        _scopes = scopes;
        _logger = logger;
        LoadMapPool();
        // rAthena bg_queue_create — preallocate one queue per named BG type.
        // Sizes mirror s_battleground_type.required_players defaults.
        EnsureQueue("bgsmall", required: 5);
        EnsureQueue("bgmedium", required: 10);
        EnsureQueue("bglarge", required: 20);
        EnsureQueue("bg", required: 5);
    }

    /// <summary>
    /// Hydrate the BG map pool from the DB. Reserved-map state is
    /// untouched; reload is idempotent for the queue state machine.
    /// </summary>
    public void LoadMapPool()
    {
        _bgMapPool.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBattlegroundCatalogDbRepository>();
            foreach (var loc in repo.GetAllLocationsAsync().GetAwaiter().GetResult())
                if (!_bgMapPool.Contains(loc.MapName)) _bgMapPool.Add(loc.MapName);
            _logger.LogInformation("battleground_location_db loaded: {N} map(s) in pool", _bgMapPool.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "battleground_location_db load failed; pool empty");
        }
    }

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

    /// <summary>rAthena <c>bg_team_warp</c> — refresh team's target coords.</summary>
    public bool TeamWarp(int bgId, int mapIndex, short x, short y)
    {
        if (!_teams.TryGetValue(bgId, out var t)) return false;
        t.MapIndex = mapIndex;
        t.SpawnX = x;
        t.SpawnY = y;
        return true;
    }

    public int TeamGetId(PlayerEntity pc) => _membership.GetValueOrDefault(pc.Id);

    /// <summary>rAthena <c>bg_member_respawn</c> — return a downed member to the team spawn.</summary>
    public bool MemberRespawn(PlayerEntity pc)
    {
        if (!_membership.TryGetValue(pc.Id, out var bgId)) return false;
        if (!_teams.TryGetValue(bgId, out var t)) return false;
        // Coord mutation handled by the caller (uses IPcSetposService);
        // the service-level seam returns the resolved spawn target.
        _logger.LogDebug("bg_member_respawn: {Pc} → bg#{Bg} ({X},{Y})",
            pc.Name, bgId, t.SpawnX, t.SpawnY);
        return true;
    }

    public bool PlayerIsInBgMap(PlayerEntity pc) => _membership.ContainsKey(pc.Id);

    /// <summary>
    /// rAthena <c>bg_mapflag_check</c> — true if BG players on the
    /// caller's map should block the skill (NOWARP / NOJOBCHANGE etc.).
    /// Minimum-viable: blocks teleport-class skills (28=AL_TELEPORT,
    /// 27=AL_WARP). Full mapflag matrix lands with IMapFlagService.
    /// </summary>
    public bool MapflagCheck(PlayerEntity pc, ushort skillId)
    {
        if (!_membership.ContainsKey(pc.Id)) return false;
        return skillId is 26 or 27 or 28; // AL_WARP / AL_TELEPORT / WE_BABY
    }

    /// <summary>rAthena <c>bg_getavailablesd</c> — first online member of the team.</summary>
    public PlayerEntity? GetAvailableSd(int bgId)
    {
        if (!_teams.TryGetValue(bgId, out var t)) return null;
        if (_entities == null) return null;
        foreach (var memberId in t.Members)
            if (_entities.Get(memberId) is PlayerEntity p) return p;
        return null;
    }

    // ----- Queue state machine — rAthena bg_queue_* family -----

    public bool QueueCheckJoinable(PlayerEntity pc, int queueId)
    {
        // rAthena bg_queue_check_joinable — checks if already queued, deserter SC,
        // and level / job gates from s_battleground_type. Minimum-viable: reject
        // double-queue + active membership.
        return !_playerQueue.ContainsKey(pc.Id) && !_membership.ContainsKey(pc.Id);
    }

    public bool QueueLeave(PlayerEntity pc)
    {
        // rAthena bg_queue_leave — drops from roster and clears player binding.
        if (!_playerQueue.TryGetValue(pc.Id, out var qid)) return false;
        _playerQueue.Remove(pc.Id);
        if (_queues.TryGetValue(qid, out var q)) q.Members.Remove(pc.Id);
        return true;
    }

    public bool QueueOnReady(int queueId)
    {
        // rAthena bg_queue_on_ready — fires once both rosters meet required
        // size. Transitions SETUP → SETUP_DELAY and broadcasts "ready" to
        // members. Minimum-viable: state flip + log.
        if (!_queues.TryGetValue(queueId, out var q)) return false;
        if (q.State != BgQueueState.Setup) return false;
        if (q.Members.Count < q.Required * 2) return false;
        q.State = BgQueueState.SetupDelay;
        _logger.LogInformation("bg_queue_on_ready: queue {Qid} → SETUP_DELAY ({N}/{R}×2)",
            queueId, q.Members.Count, q.Required);
        return true;
    }

    public bool QueueReservation(int queueId)
    {
        // rAthena bg_queue_reservation — reserves an open BG map from
        // the pool for this queue. Returns false if every map is
        // already reserved (caller falls back to tid_requeue).
        if (!_queues.TryGetValue(queueId, out var q)) return false;
        foreach (var m in _bgMapPool)
        {
            if (_reservedMaps.Add(m))
            {
                q.ReservedMap = m;
                _logger.LogInformation("bg_queue_reservation: queue {Qid} → map {Map}", queueId, m);
                return true;
            }
        }
        return false; // pool exhausted
    }

    public void QueueClear(int queueId)
    {
        // rAthena bg_queue_clear — resets to SETUP, drops rosters,
        // releases the reserved map back to the pool.
        if (!_queues.TryGetValue(queueId, out var q)) return;
        foreach (var m in q.Members) _playerQueue.Remove(m);
        q.Members.Clear();
        q.AcceptedCount = 0;
        q.State = BgQueueState.Setup;
        if (q.ReservedMap != null)
        {
            _reservedMaps.Remove(q.ReservedMap);
            q.ReservedMap = null;
        }
    }

    public void QueueJoinSolo(PlayerEntity pc, string queueName)
    {
        // rAthena bg_queue_join_solo — single player → assigned roster slot.
        var qid = EnsureQueue(queueName, required: 5);
        if (!QueueCheckJoinable(pc, qid)) return;
        _queues[qid].Members.Add(pc.Id);
        _playerQueue[pc.Id] = qid;
        QueueOnReady(qid);
    }

    public void QueueJoinParty(PlayerEntity leader, string queueName)
    {
        // rAthena bg_queue_join_party — needs IPartyService.GetMembers to fan
        // out. Minimum-viable: enroll leader; parties data-pending.
        QueueJoinSolo(leader, queueName);
    }

    public void QueueJoinGuild(PlayerEntity leader, string queueName)
    {
        // rAthena bg_queue_join_guild — same shape as party path.
        QueueJoinSolo(leader, queueName);
    }

    public void QueueJoinMulti(PlayerEntity leader, string queueName, byte mode)
    {
        // rAthena bg_queue_join_multi — mode flag picks solo/party/guild.
        switch (mode)
        {
            case 1: QueueJoinParty(leader, queueName); break;
            case 2: QueueJoinGuild(leader, queueName); break;
            default: QueueJoinSolo(leader, queueName); break;
        }
    }

    public void QueueOnAcceptInvite(PlayerEntity pc, int queueId)
    {
        // rAthena bg_queue_on_accept_invite — increment AcceptedCount, flip
        // to ACTIVE when everyone confirms.
        if (!_queues.TryGetValue(queueId, out var q)) return;
        if (q.State != BgQueueState.SetupDelay) return;
        q.AcceptedCount++;
        if (q.AcceptedCount >= q.Required * 2)
        {
            QueueStartBattleground(queueId);
        }
    }

    public void QueueStartBattleground(int queueId)
    {
        // rAthena bg_queue_start_battleground — transition to ACTIVE.
        if (!_queues.TryGetValue(queueId, out var q)) return;
        q.State = BgQueueState.Active;
        _logger.LogInformation("bg_queue_start_battleground: queue {Qid} → ACTIVE", queueId);
    }

    public void JoinActive(PlayerEntity pc)
    {
        // rAthena bg_join_active — late-joiner warp-in. data-pending on map pool.
    }

    /// <summary>
    /// rAthena <c>bg_send_xy_timer_sub</c> — periodic minimap dot
    /// refresh for team members. Per-PC ZC_NOTIFY_POSITION_TO_GROUP_M
    /// emit lives in the wire-broadcaster; the service-level seam
    /// surfaces the recipient list.
    /// </summary>
    public void SendXyTimerSub(int bgId)
    {
        if (!_teams.TryGetValue(bgId, out var t)) return;
        // The clif caller iterates t.Members and emits per-PC packets.
    }

    /// <summary>rAthena <c>bg_send_dot_remove</c> — strip minimap dot on leave.</summary>
    public void SendDotRemove(PlayerEntity pc)
    {
        // Wire packet 0x0192 (ZC_NOTIFY_POSITION_TO_GROUP_M w/ marker=0)
        // emit lives clif-side; service-level just gates on membership.
        if (!_membership.ContainsKey(pc.Id)) return;
    }

    /// <summary>rAthena <c>bg_send_message</c> — broadcast a system line to team members.</summary>
    public void SendMessage(int bgId, string sender, string text)
    {
        if (!_teams.TryGetValue(bgId, out var t)) return;
        _logger.LogInformation("bg_send_message: bg#{Bg} <{Sender}> {Text} → {Count} members",
            bgId, sender, text, t.Members.Count);
        // ZC_NOTIFY_CHAT_PARTY emit per-member is the clif call; the
        // service-level kept here lets handlers chain without holding
        // ISessionManagerAccessor (added in AT-D2 service injections).
    }

    // ----- helpers — not on the interface, exposed for the @bg* commands -----

    /// <summary>Lookup a queue id by canonical name (creates lazily).</summary>
    public int EnsureQueue(string queueName, byte required)
    {
        if (_queueByName.TryGetValue(queueName, out var existing)) return existing;
        var id = _nextQueueId++;
        _queues[id] = new BgQueue { Id = id, Name = queueName, Required = required };
        _queueByName[queueName] = id;
        return id;
    }

    /// <summary>Snapshot a queue for the @bg / @bgleader / @bginvite commands.</summary>
    public (int QueueId, string Name, BgQueueState State, int Members, byte Required)? GetQueueSnapshot(string queueName)
    {
        if (!_queueByName.TryGetValue(queueName, out var qid)) return null;
        var q = _queues[qid];
        return (q.Id, q.Name, q.State, q.Members.Count, q.Required);
    }

    /// <summary>End a queue (rAthena <c>bg_queue_clear(true)</c>).</summary>
    public void QueueEnd(int queueId)
    {
        if (!_queues.TryGetValue(queueId, out var q)) return;
        q.State = BgQueueState.Ended;
        _logger.LogInformation("bg_queue_end: queue {Qid} → ENDED", queueId);
        QueueClear(queueId);
    }

    /// <summary>Mark a player as queue leader for invite/multi-member flows.</summary>
    public bool SetQueueLeader(PlayerEntity pc)
    {
        if (!_playerQueue.TryGetValue(pc.Id, out var qid)) return false;
        if (!_queues.TryGetValue(qid, out var q)) return false;
        q.LeaderId = pc.Id;
        return true;
    }

    /// <summary>List all known queues + state — for @bg with no args.</summary>
    public IReadOnlyList<(int QueueId, string Name, BgQueueState State, int Members, byte Required)> GetAllQueues()
    {
        var list = new List<(int, string, BgQueueState, int, byte)>(_queues.Count);
        foreach (var q in _queues.Values)
            list.Add((q.Id, q.Name, q.State, q.Members.Count, q.Required));
        return list;
    }

    private sealed class BgTeam
    {
        public int Id;
        public int MapIndex;
        public byte Side;
        public short SpawnX;
        public short SpawnY;
        public HashSet<EntityId> Members = new();
    }

    private sealed class BgQueue
    {
        public int Id;
        public string Name = string.Empty;
        public byte Required;
        public BgQueueState State = BgQueueState.Setup;
        public HashSet<EntityId> Members = new();
        public int AcceptedCount;
        public EntityId LeaderId;
        public string? ReservedMap;
    }
}

/// <summary>rAthena <c>enum e_queue_state</c>.</summary>
public enum BgQueueState : byte
{
    Setup = 0,
    SetupDelay = 1,
    Active = 2,
    Ended = 3,
}
