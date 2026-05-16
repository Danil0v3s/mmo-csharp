namespace Map.Server.Entities;

/// <summary>
/// A player character on a map. The single source of truth for "this account
/// is on this map at this cell." Replaces the legacy struct of the same name
/// that lived in MapServerImpl.cs.
///
/// EntityId == CharacterId for PCs (rAthena convention; char_id is globally
/// unique so it doubles as the block_list id).
/// </summary>
public sealed class PlayerEntity : Entity
{
    public int AccountId { get; }
    public int CharacterId => Id.Value;
    public string Name { get; }
    public Guid SessionId { get; }

    public override EntityType Type => EntityType.Pc;

    public PlayerEntity(
        int characterId,
        int accountId,
        string name,
        Guid sessionId,
        uint mapId,
        short x,
        short y)
        : base(new EntityId(characterId), mapId, x, y)
    {
        AccountId = accountId;
        Name = name ?? string.Empty;
        SessionId = sessionId;
    }
}
