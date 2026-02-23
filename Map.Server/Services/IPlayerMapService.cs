namespace Map.Server.Services;

/// <summary>
/// Manages player locations on maps.
/// </summary>
public interface IPlayerMapService
{
    void AddPlayerToMap(long characterId, uint mapId, float x, float y, float z);
    bool RemovePlayerFromMap(long characterId);
    IEnumerable<PlayerEntity> GetPlayersOnMap(uint mapId);
}
