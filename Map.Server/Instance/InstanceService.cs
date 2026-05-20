using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Instance;

/// <summary>Default <see cref="IInstanceService"/>. Instance ID counter + minimal map; instance_db YAML data-pending.</summary>
public sealed class InstanceService : IInstanceService
{
    private int _nextId = 1;
    private readonly Dictionary<int, InstanceRecord> _instances = new();
    private readonly ILogger<InstanceService> _logger;

    public InstanceService(ILogger<InstanceService> logger) => _logger = logger;

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
    public void Reload() { _instances.Clear(); }

    private sealed class InstanceRecord
    {
        public int Id;
        public int DbId;
        public int OwnerId;
        public byte Mode;
        public int Users;
    }
}
