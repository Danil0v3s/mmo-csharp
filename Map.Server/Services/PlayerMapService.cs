using Map.Server.Entities;

namespace Map.Server.Services;

/// <summary>
/// Facade implementation over <see cref="IEntityRegistry"/>. Translates the
/// legacy character-id-keyed API into the new registry operations and
/// constructs <see cref="PlayerEntity"/> instances on Add.
///
/// IPC-driven flows (P6 gRPC handlers) use this; gameplay code uses
/// <see cref="IEntityRegistry"/> directly.
/// </summary>
public sealed class PlayerMapService : IPlayerMapService
{
    private readonly IEntityRegistry _registry;

    public PlayerMapService(IEntityRegistry registry)
    {
        _registry = registry;
    }

    public void AddPlayer(int characterId, int accountId, string name, Guid sessionId, uint mapId, short x, short y)
    {
        // Defensive: if a stale entry exists (e.g. from a crashed previous
        // session), remove it first to keep the registry consistent.
        var existing = _registry.Get(new EntityId(characterId));
        if (existing != null)
        {
            _registry.Remove(new EntityId(characterId));
        }
        var entity = new PlayerEntity(characterId, accountId, name, sessionId, mapId, x, y);
        _registry.Add(entity);
    }

    public bool RemovePlayer(int characterId) =>
        _registry.Remove(new EntityId(characterId)) is PlayerEntity;

    public PlayerEntity? RemoveAndGet(int characterId) =>
        _registry.Remove(new EntityId(characterId)) as PlayerEntity;

    public PlayerEntity? GetByCharacterId(int characterId) =>
        _registry.Get(new EntityId(characterId)) as PlayerEntity;

    public IEnumerable<PlayerEntity> GetPlayersOnMap(uint mapId) =>
        _registry.All().OfType<PlayerEntity>().Where(p => p.MapId == mapId);

    public IEnumerable<PlayerEntity> GetAllPlayers() =>
        _registry.All().OfType<PlayerEntity>();

    public IEnumerable<PlayerEntity> GetByAccountId(int accountId) =>
        _registry.All().OfType<PlayerEntity>().Where(p => p.AccountId == accountId);

    public int Count => _registry.All().OfType<PlayerEntity>().Count();

    public bool IsPlayerOnAnyMap(int characterId) =>
        _registry.Contains(new EntityId(characterId));
}
