using Core.Database.Entities;
using Map.Server.Entities;
using Map.Server.Warps;

namespace Map.Server.Tests.Warps;

/// <summary>
/// Test doubles for <see cref="IWarpService"/> + <see cref="IWarpDispatcher"/>
/// — used by the non-warp tests in Movement / Spawn / Combat / GM / Session
/// which exercise <see cref="Map.Server.Movement.MovementService"/> without
/// needing a real warp catalog. Keeps those tests focused on their own
/// subject without touching the database.
/// </summary>
internal sealed class NoOpWarpService : IWarpService
{
    public WarpEntity? TryGetWarpAt(string mapName, short x, short y) => null;
    public int Count => 0;
}

internal sealed class NoOpWarpDispatcher : IWarpDispatcher
{
    public void OnEnterWarp(PlayerEntity entity, WarpEntity warp) { }
}
