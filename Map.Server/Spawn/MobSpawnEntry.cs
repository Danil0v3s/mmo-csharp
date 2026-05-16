namespace Map.Server.Spawn;

/// <summary>
/// Static description of one mob spawn declaration on a map. Multiple entries
/// can target the same map and same mob class — each contributes its own
/// amount + respawn cadence. Mirrors rAthena's <c>monster</c> script command
/// arguments (mob.cpp <c>mob_parse_dataset</c>).
///
/// Coordinates form an inclusive box: random walkable cell in
/// [X − Xs, X + Xs] × [Y − Ys, Y + Ys]. If both <see cref="Xs"/> and
/// <see cref="Ys"/> are 0 AND <see cref="X"/>/<see cref="Y"/> are 0,
/// the mob spawns anywhere walkable on the whole map.
/// </summary>
public sealed record MobSpawnEntry
{
    public required int MobClassId { get; init; }
    public required uint MapId { get; init; }

    public short X { get; init; }
    public short Y { get; init; }
    public short Xs { get; init; }
    public short Ys { get; init; }

    /// <summary>Number of live instances this entry should maintain.</summary>
    public int Amount { get; init; } = 1;

    /// <summary>Base respawn delay in ms (rAthena <c>delay1</c>).</summary>
    public int RespawnDelayMs { get; init; } = 5000;

    /// <summary>Additional random jitter added on top of <see cref="RespawnDelayMs"/> (rAthena <c>delay2</c>).</summary>
    public int RespawnJitterMs { get; init; } = 2000;

    /// <summary>Display name override; falls back to <c>mob_db.Name</c>.</summary>
    public string? DisplayName { get; init; }
}
