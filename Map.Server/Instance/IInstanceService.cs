using Map.Server.Entities;

namespace Map.Server.Instance;

/// <summary>
/// Per-party / per-character map instances. Canonical entry points
/// for rAthena <c>instance.cpp</c> (1 316 lines, 17 functions).
/// </summary>
public interface IInstanceService
{
    /// <summary>rAthena <c>instance_create</c>.</summary>
    int Create(int dbId, int ownerId, byte mode);

    /// <summary>rAthena <c>instance_addusers</c>.</summary>
    bool AddUsers(int instanceId, int count);

    /// <summary>rAthena <c>instance_delusers</c>.</summary>
    bool DelUsers(int instanceId, int count);

    /// <summary>rAthena <c>instance_destroy</c>.</summary>
    bool Destroy(int instanceId);

    /// <summary>rAthena <c>instance_destroy_command</c>.</summary>
    void DestroyCommand(PlayerEntity pc, int instanceId);

    /// <summary>rAthena <c>instance_reqinfo</c>.</summary>
    bool ReqInfo(PlayerEntity pc, int instanceId);

    /// <summary>rAthena <c>instance_startidletimer</c>.</summary>
    bool StartIdleTimer(int instanceId);

    /// <summary>rAthena <c>instance_stopidletimer</c>.</summary>
    bool StopIdleTimer(int instanceId);

    /// <summary>rAthena <c>instance_startkeeptimer</c>.</summary>
    bool StartKeepTimer(int instanceId);

    /// <summary>rAthena <c>instance_addnpc</c>.</summary>
    void AddNpc(int instanceId, NpcEntity npc);

    /// <summary>rAthena <c>instance_generate_mapname</c>.</summary>
    string GenerateMapName(string baseName, int instanceId);

    /// <summary>rAthena <c>instance_getsd</c>.</summary>
    PlayerEntity? GetOwner(int instanceId);

    /// <summary>rAthena <c>do_reload_instance</c>.</summary>
    void Reload();

    /// <summary>rAthena <c>instance_delete_timer</c> sweep — per-tick lifecycle expiry (idle/keep
    /// timeout → destroy). Called from the map-server game loop.</summary>
    void Tick(long nowTick);

    /// <summary>rAthena <c>instance_mapid</c> — resolve a base map id within an instance.</summary>
    int MapId(int baseMapId, int instanceId);

    /// <summary>rAthena <c>instance_addmap</c> — register an extra map slot inside an instance.</summary>
    bool AddMap(int instanceId, string baseMapName);

    /// <summary>rAthena <c>instance_enter</c> — warp the caller into an instance's entry map.</summary>
    bool Enter(PlayerEntity pc, int instanceId);

    /// <summary>List the map slots registered under an instance — backs script lookups.</summary>
    IReadOnlyList<string> GetInstanceMaps(int instanceId);
}
