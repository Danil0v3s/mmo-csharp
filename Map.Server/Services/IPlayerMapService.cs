namespace Map.Server.Services;

/// <summary>
/// Manages player locations on maps.
/// </summary>
public interface IPlayerMapService
{
    void AddPlayerToMap(long characterId, uint mapId, float x, float y, float z);
    void AddPlayerToMap(long characterId, int accountId, uint mapId, float x, float y, float z);
    bool RemovePlayerFromMap(long characterId);
    PlayerEntity? RemovePlayerAndGet(long characterId);
    IEnumerable<PlayerEntity> GetPlayersOnMap(uint mapId);
    IEnumerable<PlayerEntity> GetAllPlayers();
    int Count { get; }
    bool IsPlayerOnAnyMap(long characterId);
}
