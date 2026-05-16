namespace Map.Server.Entities;

/// <summary>
/// Strong type for an entity's unique runtime id. Wraps an <see cref="int"/>
/// to avoid confusion with character_id, account_id, or mob class id. Mirrors
/// rAthena's <c>bl-&gt;id</c> field.
///
/// Allocation ranges mirror rAthena conventions so logs/dumps are recognizable:
///   - PCs:          uses the character_id directly (always &gt; 0, well-known range).
///   - NPCs:         800,000,000 + sequence (rAthena START_NPC_ID).
///   - Floor items:  2,000,000,000 + sequence (rAthena MIN_FLOORITEM).
///   - Mobs/skill units: allocated from a non-colliding range maintained by
///     <see cref="EntityIdAllocator"/>.
/// </summary>
public readonly record struct EntityId(int Value)
{
    public static readonly EntityId None = new(0);
    public bool IsValid => Value > 0;
    public override string ToString() => Value.ToString();
    public static implicit operator int(EntityId id) => id.Value;
}
