namespace Map.Server.Services;

/// <summary>
/// Partial <see cref="CharServerIpcService"/> — mapreg slice.
/// The char-side gRPC handler + proto messages are deferred until the
/// script engine's <c>$var</c> consumer ports (Phase 4 of
/// <c>map/scripting/README.md</c>). Until then this is a no-op that
/// satisfies the typed seam — the rAthena
/// <c>intif_request_mapreg</c> / <c>intif_save_mapreg</c> entry
/// points dispatch through here instead of returning 0.
/// </summary>
public partial class CharServerIpcService : ICharServerIpcServiceMapreg
{
    public Task<bool> RequestMapregAsync(CancellationToken cancellationToken = default)
    {
        // No-op until the char-side mapreg gRPC binding lands.
        return Task.FromResult(true);
    }

    public Task<bool> SaveMapregAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
