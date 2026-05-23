using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IEnchantGradeService"/>. Caches the entire
/// enchantgrade chance table at boot as a nested-tuple dictionary.
///
/// DBR-2d: enchantgrade_db is 2 equipment-type groups (Armor,
/// Weapon), enchantgrade_level_db buckets per-item-level, and
/// enchantgrade_chance_db has the per-refine chance row.
/// </summary>
public sealed class EnchantGradeService : IEnchantGradeService
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<EnchantGradeService> _logger;

    private Dictionary<(string equipType, int itemLvl, string grade, int refine), int> _chances
        = new();

    public EnchantGradeService(IServiceScopeFactory scopes, ILogger<EnchantGradeService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty catalog.</summary>
    public EnchantGradeService(ILogger<EnchantGradeService> logger) { _logger = logger; }

    public bool IsLoaded => _chances.Count > 0;

    public int GetUpgradeChance(string equipType, int itemLevel, string grade, int refine)
    {
        if (string.IsNullOrEmpty(equipType) || string.IsNullOrEmpty(grade)) return 0;
        return _chances.TryGetValue((equipType, itemLevel, grade, refine), out var c) ? c : 0;
    }

    public void Reload()
    {
        _chances = new();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IEnchantGradeDbRepository>();
            var rows = repo.GetAllChancesAsync().GetAwaiter().GetResult();
            foreach (var r in rows)
                _chances[(r.EquipType, r.ItemLevel, r.Grade, r.Refine)] = r.Chance;
            _logger.LogInformation(
                "enchantgrade catalog loaded: {Chances} chance row(s)", _chances.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "enchantgrade catalog load failed — upgrade chances will return 0");
        }
    }
}
