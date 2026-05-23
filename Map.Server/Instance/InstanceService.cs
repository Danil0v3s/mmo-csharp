using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Map.Server.Movement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Instance;

/// <summary>
/// Default <see cref="IInstanceService"/>. Catalog loaded from
/// <c>instance_db</c> (seeded from <c>db/re/instance_db.yml</c>,
/// ~79 instances). Per-instance runtime state stays in-memory.
/// </summary>
public sealed class InstanceService : IInstanceService
{
    private int _nextId = 1;
    private readonly Dictionary<int, InstanceRecord> _instances = new();
    private readonly Dictionary<uint, InstanceDbEntity> _catalog = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<InstanceService> _logger;

    private readonly IPcSetposService? _setpos;

    public InstanceService(IServiceScopeFactory scopes, IPcSetposService setpos, ILogger<InstanceService> logger)
    {
        _scopes = scopes;
        _setpos = setpos;
        _logger = logger;
        LoadCatalog();
    }

    public InstanceService(ILogger<InstanceService> logger) { _logger = logger; }

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

    public int Create(int dbId, int ownerId, byte mode)
    {
        var id = _nextId++;
        var record = new InstanceRecord { Id = id, DbId = dbId, OwnerId = ownerId, Mode = mode };
        // Seed Maps[] from the instance_db.additional_maps column
        // (rAthena instance_db.yml AdditionalMaps). Format: comma-
        // separated base map names.
        if (_catalog.TryGetValue((uint)dbId, out var tmpl))
        {
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
        return id;
    }

    public bool AddUsers(int instanceId, int count)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        r.Users += count; return true;
    }

    public bool DelUsers(int instanceId, int count)
    {
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
        r.Users = Math.Max(0, r.Users - count); return true;
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
    /// rAthena <c>instance_enter</c> — warp the caller into the
    /// instance's entry map. Reads EnterMap / EnterX / EnterY from
    /// the <c>instance_db</c> SQL row (no more hardcoded coords);
    /// falls back to (100,100) if those columns are null for the
    /// template (rAthena default behavior).
    /// </summary>
    public bool Enter(PlayerEntity pc, int instanceId)
    {
        if (_setpos == null) return false;
        if (!_instances.TryGetValue(instanceId, out var r)) return false;
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
        return result == SetposResult.Ok;
    }

    /// <summary>List the map slots registered under an instance.</summary>
    public IReadOnlyList<string> GetInstanceMaps(int instanceId)
        => _instances.TryGetValue(instanceId, out var r) ? r.Maps : Array.Empty<string>();

    public bool Destroy(int instanceId) => _instances.Remove(instanceId);
    public void DestroyCommand(PlayerEntity pc, int instanceId) => Destroy(instanceId);
    public bool ReqInfo(PlayerEntity pc, int instanceId) => _instances.ContainsKey(instanceId);
    public bool StartIdleTimer(int instanceId) => _instances.ContainsKey(instanceId);
    public bool StopIdleTimer(int instanceId) => _instances.ContainsKey(instanceId);
    public bool StartKeepTimer(int instanceId) => _instances.ContainsKey(instanceId);
    public void AddNpc(int instanceId, NpcEntity npc) { }
    public string GenerateMapName(string baseName, int instanceId) => $"{instanceId}@{baseName}";

    /// <summary>
    /// rAthena <c>instance_mapid</c> — given a base map id and an
    /// instance id, return the instance-scoped map id. The current
    /// model uses string-prefixed map names so the resolution is a
    /// hash combine; the caller maps the string back to a uint via
    /// <see cref="EntityRegistryMapHash"/>.
    /// </summary>
    public int MapId(int baseMapId, int instanceId)
    {
        if (!_instances.ContainsKey(instanceId)) return baseMapId;
        // Combine base + instance into a stable namespace-mangled id;
        // the warp handler (instance_enter — still pending) will resolve
        // this back via GenerateMapName.
        return unchecked((int)(baseMapId ^ (instanceId * 0x9E3779B1)));
    }

    public PlayerEntity? GetOwner(int instanceId)
        => _instances.TryGetValue(instanceId, out var r) ? null : null;

    public void Reload() { _instances.Clear(); _catalog.Clear(); LoadCatalog(); }

    private sealed class InstanceRecord
    {
        public int Id;
        public int DbId;
        public int OwnerId;
        public byte Mode;
        public int Users;
        public List<string> Maps = new();
    }
}
