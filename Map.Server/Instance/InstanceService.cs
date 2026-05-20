using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
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

    public InstanceService(IServiceScopeFactory scopes, ILogger<InstanceService> logger)
    {
        _scopes = scopes;
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
        _instances[id] = new InstanceRecord { Id = id, DbId = dbId, OwnerId = ownerId, Mode = mode };
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

    public bool Destroy(int instanceId) => _instances.Remove(instanceId);
    public void DestroyCommand(PlayerEntity pc, int instanceId) => Destroy(instanceId);
    public bool ReqInfo(PlayerEntity pc, int instanceId) => _instances.ContainsKey(instanceId);
    public bool StartIdleTimer(int instanceId) => _instances.ContainsKey(instanceId);
    public bool StopIdleTimer(int instanceId) => _instances.ContainsKey(instanceId);
    public bool StartKeepTimer(int instanceId) => _instances.ContainsKey(instanceId);
    public void AddNpc(int instanceId, NpcEntity npc) { }
    public string GenerateMapName(string baseName, int instanceId) => $"{instanceId}@{baseName}";
    public PlayerEntity? GetOwner(int instanceId) => null;
    public void Reload() { _instances.Clear(); _catalog.Clear(); LoadCatalog(); }

    private sealed class InstanceRecord
    {
        public int Id;
        public int DbId;
        public int OwnerId;
        public byte Mode;
        public int Users;
    }
}
