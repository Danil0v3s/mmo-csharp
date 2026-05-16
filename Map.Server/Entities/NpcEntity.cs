namespace Map.Server.Entities;

/// <summary>
/// Stub for MS2 — see <c>.agents/migrations/map/npc.md</c>. NPCs are stationary
/// clickable entities on a map (warps, shops, dialog NPCs). Subclassed in MS2
/// with the per-type behavior.
/// </summary>
public class NpcEntity : Entity
{
    public string Name { get; }
    public int SpriteId { get; }

    public override EntityType Type => EntityType.Npc;

    public NpcEntity(EntityId id, string name, int spriteId, uint mapId, short x, short y, byte dir = 0)
        : base(id, mapId, x, y)
    {
        Name = name ?? string.Empty;
        SpriteId = spriteId;
        Dir = dir;
    }
}
