namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerSpawn({ ... })</c> call. Mirrors the shape of a row in the
/// existing <c>mob_spawn</c> table; script-side spawns coexist with DB-loaded
/// spawns in <c>MobSpawnService</c>.
/// </summary>
public sealed record SpawnRegistration
{
    public required string Map { get; init; }
    /// <summary>Spawn area. Null = anywhere walkable on the map.</summary>
    public (short X, short Y, short Xs, short Ys)? Area { get; init; }
    public required int MobId { get; init; }
    public required int Amount { get; init; }
    public int RespawnBaseMs { get; init; } = 5_000;
    public int RespawnJitterMs { get; init; } = 2_000;
    public bool Boss { get; init; }
    public string? DisplayName { get; init; }
    public string? OnDeathEvent { get; init; }
    public int Size { get; init; }
    public int Ai { get; init; }
}
