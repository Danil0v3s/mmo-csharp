namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerNpc({ ... })</c> call, marshalled into a typed record.
/// The TS-side shape lives in <c>scripts/types/api.d.ts</c>; this record
/// is the C# mirror.
///
/// Coordinates are <see cref="short"/> to match the wire protocol and the
/// entity layer. The display name must be unique across the corpus —
/// <c>NpcRegistry</c> enforces this at <c>AddNpc</c> time.
/// </summary>
public sealed record NpcRegistration
{
    public required string Map { get; init; }
    public required short X { get; init; }
    public required short Y { get; init; }
    public byte Dir { get; init; }
    public required int Sprite { get; init; }
    public required string Name { get; init; }
    public (short Xs, short Ys)? TriggerArea { get; init; }
    public required NpcHooks Hooks { get; init; }
}
