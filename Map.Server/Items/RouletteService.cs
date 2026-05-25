using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Items;

/// <summary>
/// Roulette wheel runtime catalog. Port of rAthena
/// <c>itemdb_parse_roulette_db</c> (<c>itemdb.cpp:4222</c>) — loads
/// the per-level reward rows from <c>db_roulette</c> at boot and
/// caches them by level. The wheel UI consults
/// <see cref="GetRow"/> when a player spins; the actual award flow
/// (roulette skill cooldown, ticket consumption, broadcast) lives in
/// the roulette feature service when it ports.
/// </summary>
public interface IRouletteService
{
    /// <summary>Total rows across every level (for boot logging).</summary>
    int Count { get; }

    /// <summary>Number of reward levels indexed (rAthena uses 7).</summary>
    int LevelCount { get; }

    /// <summary>True iff at least one row loaded.</summary>
    bool IsLoaded { get; }

    /// <summary>All rows for the given level (0-indexed into the wheel position).</summary>
    IReadOnlyList<DbRouletteEntity> GetByLevel(ushort level);

    /// <summary>Lookup a single (level, position) row, null if absent.</summary>
    DbRouletteEntity? GetRow(ushort level, int positionIndex);

    /// <summary>Reload the in-memory cache from the database.</summary>
    bool Reload();
}

/// <summary>Default <see cref="IRouletteService"/>.</summary>
public sealed class RouletteService : IRouletteService
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<RouletteService> _logger;

    // level → ordered rows (sorted by Index)
    private Dictionary<ushort, DbRouletteEntity[]> _byLevel = new();
    private int _totalRows;

    public RouletteService(IServiceScopeFactory scopes, ILogger<RouletteService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty cache.</summary>
    public RouletteService(ILogger<RouletteService> logger)
    {
        _logger = logger;
    }

    public int Count => _totalRows;
    public int LevelCount => _byLevel.Count;
    public bool IsLoaded => _totalRows > 0;

    public IReadOnlyList<DbRouletteEntity> GetByLevel(ushort level)
        => _byLevel.TryGetValue(level, out var rows) ? rows : Array.Empty<DbRouletteEntity>();

    public DbRouletteEntity? GetRow(ushort level, int positionIndex)
    {
        if (!_byLevel.TryGetValue(level, out var rows)) return null;
        if (positionIndex < 0 || positionIndex >= rows.Length) return null;
        return rows[positionIndex];
    }

    public bool Reload()
    {
        if (_scopes == null)
        {
            _byLevel = new();
            _totalRows = 0;
            return false;
        }
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRouletteDbRepository>();
            var rows = repo.GetAllAsync().GetAwaiter().GetResult();
            var grouped = rows
                .GroupBy(r => r.Level)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Index).ToArray());
            _byLevel = grouped;
            _totalRows = rows.Count;
            _logger.LogInformation(
                "db_roulette loaded: {Rows} rows / {Levels} level(s)",
                _totalRows, _byLevel.Count);
            return _totalRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "db_roulette load failed — catalog empty");
            _byLevel = new();
            _totalRows = 0;
            return false;
        }
    }
}
