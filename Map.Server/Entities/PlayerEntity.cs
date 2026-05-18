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

    /// <summary>
    /// Current HP. Defaults to <see cref="MaxHp"/> at spawn; mutated by
    /// <see cref="Items.IItemDropService"/>-adjacent damage path in
    /// MS3 combat. Will be hydrated from the char-side persistence once
    /// the inventory/status IPC carries the full stats payload.
    /// </summary>
    public int Hp { get; set; } = 40;

    /// <summary>
    /// Maximum HP. Level-1 default mirrors rAthena's pre-status-recalc
    /// baseline (40); MS3 status will recompute from Vit + class + level.
    /// </summary>
    public int MaxHp { get; set; } = 40;

    /// <summary>
    /// Current SP. Same lifecycle as <see cref="Hp"/>: default at spawn,
    /// mutated by skill / heal paths, hydrated from char-side persistence
    /// once the IPC carries the full stats payload.
    /// </summary>
    public int Sp { get; set; } = 11;

    /// <summary>Maximum SP. Level-1 default mirrors rAthena's baseline (11).</summary>
    public int MaxSp { get; set; } = 11;

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
