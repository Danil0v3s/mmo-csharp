using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

/// <summary>
/// Read-only access to the declarative <c>warp</c> table (ported from
/// rAthena <c>npc/re/warps/*.txt</c>). The map server loads every warp for
/// its hosted maps once at boot and serves trigger lookups from memory —
/// callers should not query this on the gameplay hot path.
/// </summary>
public interface IWarpRepository
{
    /// <summary>All warps whose source is <paramref name="mapName"/>.</summary>
    Task<List<WarpEntity>> GetBySrcMapAsync(string mapName, CancellationToken ct = default);

    /// <summary>All warps in the table — used for boot-time loading.</summary>
    Task<List<WarpEntity>> GetAllAsync(CancellationToken ct = default);
}
