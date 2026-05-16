using System.Collections.Concurrent;

namespace Map.Server.Services;

public class PlayerMapService : IPlayerMapService
{
    private readonly ConcurrentDictionary<long, PlayerEntity> _players = new();

    public void AddPlayerToMap(long characterId, uint mapId, float x, float y, float z)
    {
        _players[characterId] = new PlayerEntity
        {
            CharacterId = characterId,
            MapId = mapId,
            PositionX = x,
            PositionY = y,
            PositionZ = z
        };
    }

    public bool RemovePlayerFromMap(long characterId)
    {
        return _players.TryRemove(characterId, out _);
    }

    public IEnumerable<PlayerEntity> GetPlayersOnMap(uint mapId)
    {
        return _players.Values.Where(p => p.MapId == mapId);
    }

    public bool IsPlayerOnAnyMap(long characterId)
    {
        return _players.ContainsKey(characterId);
    }
}
