using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IRefineService"/>. Loads the entire
/// <c>refine_group_db</c> / <c>refine_level_db</c> / <c>refine_chance_db</c>
/// tree at boot and serves lookups out of nested dictionaries.
///
/// Cache shape:
///   _levels: (group, itemLevel, refineLevel) → bonus
///   _chances: (group, itemLevel, refineLevel, chanceType) → (rate, price, material)
///
/// DBR-2c: replaces the prior AT-C7 placeholder. Refine UI handler and
/// <c>@refine</c> GM cmd can pull rates / materials directly.
/// </summary>
public sealed class RefineService : IRefineService
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<RefineService> _logger;

    private Dictionary<(string group, int itemLvl, int refineLvl), int> _levels = new();
    private Dictionary<(string group, int itemLvl, int refineLvl, string chanceType), RefineAttempt> _chances
        = new();
    private readonly HashSet<string> _groups = new(StringComparer.OrdinalIgnoreCase);

    public RefineService(IServiceScopeFactory scopes, ILogger<RefineService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty catalog.</summary>
    public RefineService(ILogger<RefineService> logger) { _logger = logger; }

    public bool IsLoaded => _groups.Count > 0;

    public int GetRefineBonus(string groupName, int itemLevel, int refineLevel)
    {
        if (string.IsNullOrEmpty(groupName)) return 0;
        return _levels.TryGetValue((groupName, itemLevel, refineLevel), out var bonus) ? bonus : 0;
    }

    public RefineAttempt? GetRefineChance(string groupName, int itemLevel, int refineLevel, string chanceType)
    {
        if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(chanceType)) return null;
        return _chances.TryGetValue((groupName, itemLevel, refineLevel, chanceType), out var row) ? row : null;
    }

    public void Reload()
    {
        _levels = new();
        _chances = new();
        _groups.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRefineDbRepository>();
            foreach (var g in repo.GetAllGroupsAsync().GetAwaiter().GetResult())
                _groups.Add(g.GroupName);
            foreach (var l in repo.GetAllLevelsAsync().GetAwaiter().GetResult())
                _levels[(l.GroupName, l.ItemLevel, l.RefineLevel)] = l.Bonus;
            foreach (var c in repo.GetAllChancesAsync().GetAwaiter().GetResult())
                _chances[(c.GroupName, c.ItemLevel, c.RefineLevel, c.ChanceType)]
                    = new RefineAttempt(c.Rate, c.Price, c.MaterialAegis);
            _logger.LogInformation(
                "refine catalog loaded: {Groups} group(s) / {Levels} level row(s) / {Chances} chance row(s)",
                _groups.Count, _levels.Count, _chances.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "refine catalog load failed — refine queries will return 0/null");
        }
    }
}
