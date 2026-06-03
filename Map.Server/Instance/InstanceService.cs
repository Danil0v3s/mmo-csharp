using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Instance;

/// <summary>
/// Default <see cref="IInstanceService"/>. Catalog loaded from
/// <c>instance_db</c> (seeded from <c>db/re/instance_db.yml</c>, ~79 instances).
///
/// FEATURE-14 lands the real **lifecycle state machine**: keep/idle limits read from the
/// <c>instance_db</c> row, an idle timer that runs while the instance is empty and a hard keep
/// timer that caps total lifetime (both driven by <see cref="Tick"/> from the game loop), owner
/// resolution by <c>e_instance_mode</c>, a party/guild/clan membership gate on <see cref="Enter"/>,
/// and a real <see cref="Destroy"/> that evicts occupants to their savepoint, despawns the
/// instance's NPCs, and clears the timers. (rAthena <c>instance.cpp</c>.)
///
/// The physical instance-map layer — cloning a base map into the <c>"{id}@{base}"</c> namespace,
/// world-spawning the template's NPCs/mob spawns so the scoped map is non-empty, and warping a player
/// onto it — requires a dynamic/mutable map registry that does not yet exist (<see cref="World.IMapWorldRegistry"/>
/// is immutable). That subsystem is INFRA-12; until it lands, <see cref="AddNpc"/> records + registers
/// the NPC entity (real bookkeeping cleaned up on destroy) but it is not yet client-visible.
/// </summary>
public sealed class InstanceService : IInstanceService
{
    // rAthena e_instance_mode (instance.hpp:32).
    private const byte ImNone = 0;
    private const byte ImChar = 1;
    private const byte ImParty = 2;
    private const byte ImGuild = 3;
    private const byte ImClan = 4;

    private int _nextId = 1;
    private readonly Dictionary<int, InstanceRecord> _instances = new();
    private readonly Dictionary<uint, InstanceDbEntity> _catalog = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<InstanceService> _logger;

    private readonly IPcSetposService? _setpos;
    private readonly IEntityRegistry? _entities;
    private readonly IPcDeathService? _death;
    private readonly Func<long> _now;

    public InstanceService(
        IServiceScopeFactory scopes,
        IPcSetposService setpos,
        ILogger<InstanceService> logger,
        IEntityRegistry? entities = null,
        IPcDeathService? death = null,
        Func<long>? now = null)
    {
        _scopes = scopes;
        _setpos = setpos;
        _logger = logger;
        _entities = entities;
        _death = death;
        _now = now ?? (() => Environment.TickCount64);
        LoadCatalog();
    }

    public InstanceService(ILogger<InstanceService> logger, Func<long>? now = null)
    {
        _logger = logger;
        _now = now ?? (() => Environment.TickCount64);
    }

    /// <summary>Catalog lookup by instance template id.</summary>
    public InstanceDbEntity? GetCatalogEntry(uint instanceId)
        => _catalog.TryGetValue(instanceId, out var v) ? v : null;

    private void LoadCatalog()
    {
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IInstanceDbRepository>();
            foreach (var i in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[i.InstanceId] = i;
            _logger.LogInformation("instance_db loaded: {N} instances", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "instance_db load failed");
        }
    }

    /// <summary>
    /// rAthena <c>instance_create</c> (instance.cpp). Allocates the id, seeds the scoped map slots
    /// from the template (EnterMap + AdditionalMaps), records the owner type/id + keep/idle limits
    /// from the <c>instance_db</c> row, and starts the idle timer — a freshly-created instance with
    /// nobody in it idles out after <c>idle_limit</c> (no leak).
    /// </summary>
    public int Create(int dbId, int ownerId, byte mode)
    {
        var id = _nextId++;
        var record = new InstanceRecord { Id = id, DbId = dbId, OwnerId = ownerId, Mode = mode, State = InstanceState.Idle };
        if (_catalog.TryGetValue((uint)dbId, out var tmpl))
        {
            record.KeepSecs = Math.Max(0, tmpl.TimeLimit);    // rAthena db->limit (hard lifetime)
            record.IdleSecs = Math.Max(0, tmpl.IdleTimeout);  // rAthena db->timeout (empty timeout)
            if (!string.IsNullOrEmpty(tmpl.EnterMap))
                record.Maps.Add(GenerateMapName(tmpl.EnterMap, id));
            if (!string.IsNullOrEmpty(tmpl.AdditionalMaps))
            {
                foreach (var raw in tmpl.AdditionalMaps.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = raw.Trim();
                    if (name.Length > 0)
                        record.Maps.Add(GenerateMapName(name, id));
                }
            }
        }
        _instances[id] = record;
        StartIdleTimer(id); // empty-on-create → idle countdown begins
        return id;
    }

    public bool AddUsers(int instanceId, int count)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        var was = r.Users;
        r.Users += count;
        if (was <= 0 && r.Users > 0)
        {
            // rAthena instance_addusers: stop the idle timer, start the keep timer, go BUSY.
            r.State = InstanceState.Busy;
            StopIdleTimer(instanceId);
            StartKeepTimer(instanceId);
        }
        return true;
    }

    public bool DelUsers(int instanceId, int count)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        r.Users = Math.Max(0, r.Users - count);
        if (r.Users == 0)
            StartIdleTimer(instanceId); // rAthena instance_delusers: empty → idle countdown
        return true;
    }

    /// <summary>rAthena <c>instance_addmap</c> — track an extra map slot under this instance.</summary>
    public bool AddMap(int instanceId, string baseMapName)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        var scopedName = GenerateMapName(baseMapName, instanceId);
        if (r.Maps.Contains(scopedName)) return false;
        r.Maps.Add(scopedName);
        _logger.LogInformation("instance_addmap: inst#{Inst} += {Map}", instanceId, scopedName);
        return true;
    }

    /// <summary>
    /// rAthena <c>instance_enter</c> — gate on owner membership (char/party/guild/clan), then warp the
    /// caller into the instance's entry map and bump occupancy (which stops the idle timer + starts the
    /// keep timer). Reads EnterMap / EnterX / EnterY from the <c>instance_db</c> row; falls back to
    /// (100,100) if those columns are null.
    /// </summary>
    public bool Enter(PlayerEntity pc, int instanceId)
    {
        if (_setpos == null) return false;
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        if (!OwnsInstance(pc, r))
        {
            _logger.LogWarning("instance_enter: {Pc} is not a member of inst#{Inst} (mode {Mode}, owner {Owner})",
                pc.Name, instanceId, r.Mode, r.OwnerId);
            return false;
        }
        if (!_catalog.TryGetValue((uint)r.DbId, out var tmpl))
        {
            _logger.LogWarning("instance_enter: db#{Db} missing catalog row", r.DbId);
            return false;
        }
        var enterMap = !string.IsNullOrEmpty(tmpl.EnterMap)
            ? GenerateMapName(tmpl.EnterMap, instanceId)
            : (r.Maps.Count > 0 ? r.Maps[0] : null);
        if (enterMap == null)
        {
            _logger.LogWarning("instance_enter: inst#{Inst} has no entry map", instanceId);
            return false;
        }
        var x = (short)(tmpl.EnterX ?? 100);
        var y = (short)(tmpl.EnterY ?? 100);
        var result = _setpos.Setpos(pc, enterMap, x, y);
        _logger.LogInformation("instance_enter: {Pc} → {Map} ({X},{Y}) = {Result}",
            pc.Name, enterMap, x, y, result);
        if (result != SetposResult.Ok) return false;
        if (r.Occupants.Add(pc.CharacterId))
            AddUsers(instanceId, 1);
        return true;
    }

    /// <summary>rAthena membership gate: the entering PC must belong to the owner per the instance
    /// mode (IM_NONE has no scoping).</summary>
    private static bool OwnsInstance(PlayerEntity pc, InstanceRecord r) => r.Mode switch
    {
        ImChar => pc.CharacterId == r.OwnerId,
        ImParty => pc.PartyId != 0 && pc.PartyId == r.OwnerId,
        ImGuild => pc.GuildId != 0 && pc.GuildId == r.OwnerId,
        ImClan => pc.ClanId != 0 && pc.ClanId == r.OwnerId,
        _ => true,
    };

    /// <summary>List the map slots registered under an instance.</summary>
    public IReadOnlyList<string> GetInstanceMaps(int instanceId)
        => _instances.TryGetValue(instanceId, out var r) ? r.Maps : Array.Empty<string>();

    /// <summary>
    /// rAthena <c>instance_destroy</c> — full teardown: evict every occupant back to their savepoint,
    /// despawn the instance's NPCs from the entity registry, clear the timers, and drop the record.
    /// </summary>
    public bool Destroy(int instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;

        // Evict occupants to their savepoint (rAthena warps everyone out of the instance maps).
        if (_entities != null)
        {
            foreach (var charId in r.Occupants.ToArray())
            {
                if (_entities.Get(new EntityId(charId)) is PlayerEntity pc)
                {
                    if (!(_death?.WarpToSavepoint(pc) ?? false) && _setpos != null)
                        _setpos.Setpos(pc, "prontera", 156, 191); // last-resort safe map if no savepoint recorded
                }
            }
        }
        r.Occupants.Clear();

        // Despawn the instance's NPCs (rAthena runs OnInstanceDestroy + map_delinstancemap).
        if (_entities != null)
            foreach (var npcId in r.Npcs)
                _entities.Remove(npcId);
        r.Npcs.Clear();

        // Clear timers + drop the record.
        r.KeepLimitTick = null;
        r.IdleLimitTick = null;
        r.State = InstanceState.Destroyed;
        _instances.Remove(instanceId);
        _logger.LogInformation("instance_destroy: inst#{Inst} torn down (was {State}, {Maps} maps)",
            instanceId, r.State, r.Maps.Count);
        return true;
    }

    public void DestroyCommand(PlayerEntity pc, int instanceId) => Destroy(instanceId);
    public bool ReqInfo(PlayerEntity pc, int instanceId) => _instances.ContainsKey(instanceId);

    /// <summary>rAthena <c>instance_startkeeptimer</c> — arm the hard-lifetime timer
    /// (keep_limit = now + db->limit). limit 0 = infinite (no timer).</summary>
    public bool StartKeepTimer(int instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        if (r.KeepSecs <= 0) return true;                 // infinite_limit
        r.KeepLimitTick ??= _now() + (long)r.KeepSecs * 1000;
        return true;
    }

    /// <summary>rAthena <c>instance_startidletimer</c> — arm the empty-timeout timer
    /// (idle_limit = now + db->timeout). timeout 0 = infinite (no timer).</summary>
    public bool StartIdleTimer(int instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        if (r.IdleSecs <= 0) return true;                 // infinite_timeout
        r.IdleLimitTick = _now() + (long)r.IdleSecs * 1000;
        return true;
    }

    /// <summary>rAthena <c>instance_stopidletimer</c> — clear the idle countdown (someone entered).</summary>
    public bool StopIdleTimer(int instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        r.IdleLimitTick = null;
        return true;
    }

    /// <summary>
    /// FEATURE-14 — per-tick lifecycle sweep (called from <see cref="MapServerImpl"/>). Destroys any
    /// instance whose hard keep-limit has elapsed (regardless of occupancy) or whose idle-limit has
    /// elapsed while empty. Mirrors rAthena's <c>instance_delete_timer</c> callbacks.
    /// </summary>
    public void Tick(long nowTick)
    {
        if (_instances.Count == 0) return;
        List<int>? expired = null;
        foreach (var (id, r) in _instances)
        {
            var keepExpired = r.KeepLimitTick is { } k && k <= nowTick;
            var idleExpired = r.IdleLimitTick is { } i && i <= nowTick;
            if (keepExpired || idleExpired)
                (expired ??= new List<int>()).Add(id);
        }
        if (expired == null) return;
        foreach (var id in expired)
        {
            var reason = _instances.TryGetValue(id, out var rr) && rr.KeepLimitTick is { } k && k <= nowTick
                ? "keep_limit" : "idle_limit";
            _logger.LogInformation("instance lifecycle: inst#{Inst} expired ({Reason}) → destroy", id, reason);
            Destroy(id);
        }
    }

    /// <summary>
    /// rAthena <c>instance_addnpc</c> — register the NPC under the instance (tracked for despawn on
    /// destroy) and add it to the entity registry. World-visibility on the scoped map is gated on the
    /// dynamic-map subsystem (INFRA-12).
    /// </summary>
    public void AddNpc(int instanceId, NpcEntity npc)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return;
        if (r.Npcs.Contains(npc.Id)) return;
        r.Npcs.Add(npc.Id);
        if (_entities != null && !_entities.Contains(npc.Id))
            _entities.Add(npc);
        _logger.LogInformation("instance_addnpc: inst#{Inst} += npc {Npc}", instanceId, npc.Id.Value);
    }

    public string GenerateMapName(string baseName, int instanceId) => $"{instanceId}@{baseName}";

    /// <summary>
    /// rAthena <c>instance_mapid</c> — given a base map id and an instance id, return the
    /// instance-scoped map id. The current model uses string-prefixed map names so the resolution is a
    /// hash combine; the caller maps the string back to a uint via the scoped name.
    /// </summary>
    public int MapId(int baseMapId, int instanceId)
    {
        if (!_instances.ContainsKey(instanceId)) return baseMapId;
        return unchecked((int)(baseMapId ^ (instanceId * 0x9E3779B1)));
    }

    /// <summary>
    /// rAthena <c>instance_getsd</c> — a representative online owner for the instance: the owner char
    /// for IM_CHAR, or any online member of the owner party/guild/clan otherwise. Null when no owner is
    /// currently online.
    /// </summary>
    public PlayerEntity? GetOwner(int instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var r) || _entities == null) return null;
        return r.Mode switch
        {
            ImChar => _entities.Get(new EntityId(r.OwnerId)) as PlayerEntity,
            ImParty => FirstOnline(p => p.PartyId == r.OwnerId),
            ImGuild => FirstOnline(p => p.GuildId == r.OwnerId),
            ImClan => FirstOnline(p => p.ClanId == r.OwnerId),
            _ => _entities.Get(new EntityId(r.OwnerId)) as PlayerEntity,
        };
    }

    private PlayerEntity? FirstOnline(Func<PlayerEntity, bool> match)
    {
        foreach (var e in _entities!.All())
            if (e is PlayerEntity pc && match(pc)) return pc;
        return null;
    }

    public void Reload() { _instances.Clear(); _catalog.Clear(); LoadCatalog(); }

    /// <summary>FEATURE-14 test seam — seed a template row without a DB round-trip.</summary>
    internal void SeedCatalogForTest(InstanceDbEntity tmpl) => _catalog[tmpl.InstanceId] = tmpl;

    /// <summary>FEATURE-14 test seam — current occupant count.</summary>
    internal int UsersOf(int instanceId) => _instances.TryGetValue(instanceId, out var r) ? r.Users : -1;

    private enum InstanceState { Idle, Busy, Destroyed }

    private sealed class InstanceRecord
    {
        public int Id;
        public int DbId;
        public int OwnerId;
        public byte Mode;
        public int Users;
        public InstanceState State;
        public int KeepSecs;          // db->limit (hard lifetime), 0 = infinite.
        public int IdleSecs;          // db->timeout (empty timeout), 0 = infinite.
        public long? KeepLimitTick;   // absolute tick the keep timer fires, null = unarmed/infinite.
        public long? IdleLimitTick;   // absolute tick the idle timer fires, null = unarmed/stopped.
        public List<string> Maps = new();
        public List<EntityId> Npcs = new();
        public HashSet<int> Occupants = new(); // char ids currently inside.
    }
}
