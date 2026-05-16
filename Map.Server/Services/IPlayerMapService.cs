using Map.Server.Entities;

namespace Map.Server.Services;

/// <summary>
/// Facade over <see cref="IEntityRegistry"/> for IPC-driven flows (gRPC
/// EnterMap/LeaveMap, ForceDisconnectAccount, etc.). Gameplay code should
/// use <see cref="IEntityRegistry"/> directly for spatial queries.
///
/// Keyed by character_id (= EntityId for PCs). Map ids passed here are
/// expected to come from <see cref="EntityRegistryMapHash"/> for spatial
/// queries to find the right index; ids that don't correspond to any loaded
/// map are still tracked by character_id but skipped in spatial queries.
/// </summary>
public interface IPlayerMapService
{
    void AddPlayer(int characterId, int accountId, string name, Guid sessionId, uint mapId, short x, short y);
    bool RemovePlayer(int characterId);
    PlayerEntity? RemoveAndGet(int characterId);
    PlayerEntity? GetByCharacterId(int characterId);
    IEnumerable<PlayerEntity> GetPlayersOnMap(uint mapId);
    IEnumerable<PlayerEntity> GetAllPlayers();
    IEnumerable<PlayerEntity> GetByAccountId(int accountId);
    int Count { get; }
    bool IsPlayerOnAnyMap(int characterId);
}
