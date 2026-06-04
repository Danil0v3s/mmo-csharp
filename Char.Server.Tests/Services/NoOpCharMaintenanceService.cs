using Char.Server.Services;

namespace Char.Server.Tests.Services;

/// <summary>
/// Inert <see cref="ICharMaintenanceService"/> for tests that build
/// <see cref="CharServerImpl"/> directly but never drive the game loop's
/// maintenance tick. The real service hits MySQL; the parity / data /
/// cascade tests don't need that.
/// </summary>
internal sealed class NoOpCharMaintenanceService : ICharMaintenanceService
{
    public Task TickAsync(CancellationToken ct) => Task.CompletedTask;
    public Task<int> RunMailReturnTickAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> RunMailDeleteTickAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> RunClanCleanupTickAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> RunOnlineCleanupTickAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> RunAuctionExpiryTickAsync(CancellationToken ct = default) => Task.FromResult(0);
}
