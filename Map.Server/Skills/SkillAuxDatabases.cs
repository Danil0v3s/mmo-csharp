using System.Text.Json;
using Core.Database.Repositories.Api;
using Map.Server.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// rAthena auxiliary skill databases — Abracadabra pool, Magic
/// Mushroom proc list, Sage Reading Spell Book mapping. Loaded
/// from MariaDB seeds produced by Tools.RathenaImporter
/// (abra_db / magicmushroom_db / spellbook_db). Falls back to an
/// empty in-memory pool if the SQL table is empty — keeps tests
/// happy without a live DB.
/// </summary>
public sealed class AbraDatabase : IAbraDatabase
{
    private readonly List<ushort> _pool = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<AbraDatabase> _logger;

    public AbraDatabase(IServiceScopeFactory scopes, ILogger<AbraDatabase> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty pool.</summary>
    public AbraDatabase(ILogger<AbraDatabase> logger) { _logger = logger; }

    public ushort? PickRandom(Random rng) => _pool.Count == 0 ? null : _pool[rng.Next(_pool.Count)];
    public int Count => _pool.Count;

    public void Reload()
    {
        _pool.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAbraDbRepository>();
            var db = scope.ServiceProvider.GetRequiredService<ISkillDb>();
            var rows = repo.GetAllAsync().GetAwaiter().GetResult();
            foreach (var r in rows)
            {
                var id = db.Name2Id(r.SkillName);
                if (id != 0) _pool.Add(id);
            }
            _logger.LogInformation("Abra pool loaded: {N} skills", _pool.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "abra_db load failed — pool empty");
        }
    }
}

public sealed class MagicMushroomDatabase : IMagicMushroomDatabase
{
    private readonly List<ushort> _pool = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<MagicMushroomDatabase> _logger;

    public MagicMushroomDatabase(IServiceScopeFactory scopes, ILogger<MagicMushroomDatabase> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public MagicMushroomDatabase(ILogger<MagicMushroomDatabase> logger) { _logger = logger; }

    public ushort? PickRandom(Random rng) => _pool.Count == 0 ? null : _pool[rng.Next(_pool.Count)];
    public int Count => _pool.Count;

    public void Reload()
    {
        _pool.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IMagicMushroomDbRepository>();
            var db = scope.ServiceProvider.GetRequiredService<ISkillDb>();
            foreach (var r in repo.GetAllAsync().GetAwaiter().GetResult())
            {
                var id = db.Name2Id(r.SkillName);
                if (id != 0) _pool.Add(id);
            }
            _logger.LogInformation("MagicMushroom pool loaded: {N} skills", _pool.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "magicmushroom_db load failed — pool empty");
        }
    }
}

/// <summary>
/// Warlock Reading Spell Book mapping — book item name → granted
/// (statusId, skillId). statusId currently set to 0; the runtime
/// fills it once the SC table assigns a `SC_FREEZE_SP` row to
/// preserved spells. PreservePoints is stored alongside as val2.
/// </summary>
public sealed class ReadingSpellbookDatabase : IReadingSpellbookDatabase
{
    private readonly Dictionary<string, (ushort skillId, int preservePoints)> _byBook = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<ReadingSpellbookDatabase> _logger;

    public ReadingSpellbookDatabase(IServiceScopeFactory scopes, ILogger<ReadingSpellbookDatabase> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public ReadingSpellbookDatabase(ILogger<ReadingSpellbookDatabase> logger) { _logger = logger; }

    /// <summary>
    /// rAthena indexes by item id; we receive a name from the SQL
    /// seed. Until ItemCatalog exposes a name→id lookup the call
    /// path here can't resolve numeric itemId. Callers that have
    /// the item name should use <see cref="GetByBookName"/> below.
    /// </summary>
    public (ushort statusId, ushort skillId)? Get(int itemId) => null;

    /// <summary>Lookup by item Aegis name (e.g. "WL_MB_MS").</summary>
    public (ushort statusId, ushort skillId, int preservePoints)? GetByBookName(string nameAegis)
        => _byBook.TryGetValue(nameAegis, out var v) ? (statusId: (ushort)0, v.skillId, v.preservePoints) : null;

    public void Reload()
    {
        _byBook.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISpellbookDbRepository>();
            var db = scope.ServiceProvider.GetRequiredService<ISkillDb>();
            foreach (var r in repo.GetAllAsync().GetAwaiter().GetResult())
            {
                var id = db.Name2Id(r.SkillName);
                if (id != 0) _byBook[r.BookNameAegis] = (id, r.PreservePoints);
            }
            _logger.LogInformation("Spell Book db loaded: {N} entries", _byBook.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "spellbook_db load failed — empty");
        }
    }
}

/// <summary>
/// rAthena <c>db/create_arrow_db.yml</c> — source-item → produced-arrow
/// recipe map, consulted by <c>skill_arrow_create</c> (AC_MAKINGARROW,
/// GC_POISONINGWEAPON) when the player picks a source item from the
/// MakingArrowList dialog. Hydrated from the SQL <c>create_arrow_db</c>
/// table (seeded by Tools.RathenaImporter — 136 source-item rows).
/// Aegis names resolve through <see cref="IItemCatalog.GetByAegisName"/>;
/// unknown ids drop silently on load.
/// </summary>
public sealed class SkillArrowDatabase : ISkillArrowDatabase
{
    private readonly Dictionary<int, IReadOnlyList<(int, int)>> _recipes = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly IItemCatalog? _catalog;
    private readonly ILogger<SkillArrowDatabase> _logger;

    public SkillArrowDatabase(IServiceScopeFactory scopes, IItemCatalog catalog, ILogger<SkillArrowDatabase> logger)
    {
        _scopes = scopes;
        _catalog = catalog;
        _logger = logger;
        Reload();
    }

    /// <summary>Test ctor — empty recipe map.</summary>
    public SkillArrowDatabase(ILogger<SkillArrowDatabase> logger) { _logger = logger; }

    public IReadOnlyList<(int itemId, int qty)> Get(int sourceItemId)
        => _recipes.TryGetValue(sourceItemId, out var v) ? v : Array.Empty<(int, int)>();

    /// <summary>Total source-item entries currently loaded.</summary>
    public int Count => _recipes.Count;

    public void Reload()
    {
        _recipes.Clear();
        if (_scopes == null || _catalog == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICreateArrowDbRepository>();
            var rows = repo.GetAllAsync().GetAwaiter().GetResult();
            var unknown = 0;
            foreach (var r in rows)
            {
                var srcItem = _catalog.GetByAegisName(r.SourceItem);
                if (srcItem == null) { unknown++; continue; }
                var recipeList = new List<(int, int)>();
                try
                {
                    var entries = JsonSerializer.Deserialize<List<MakeEntry>>(r.MakeJson) ?? new();
                    foreach (var e in entries)
                    {
                        if (string.IsNullOrEmpty(e.Item)) continue;
                        var arrow = _catalog.GetByAegisName(e.Item);
                        if (arrow == null) { unknown++; continue; }
                        recipeList.Add(((int)arrow.Id, e.Amount > 0 ? e.Amount : 1));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "create_arrow_db row {Source} has malformed Make json", r.SourceItem);
                    continue;
                }
                if (recipeList.Count > 0)
                    _recipes[(int)srcItem.Id] = recipeList;
            }
            _logger.LogInformation("create_arrow_db loaded: {N} source items, {U} aegis names skipped",
                _recipes.Count, unknown);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "create_arrow_db load failed — recipe map empty");
        }
    }

    private sealed class MakeEntry
    {
        public string? Item { get; set; }
        public int Amount { get; set; }
    }
}
