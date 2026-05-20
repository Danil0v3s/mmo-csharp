using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// In-memory stubs for the four rAthena auxiliary skill databases.
/// Empty until the YAML loaders land — the entry points exist so
/// the special-skill code (Abracadabra, Magic Mushroom proc, Spell
/// Book read, Arrow Craft) has a typed lookup target.
/// </summary>
public sealed class AbraDatabase : IAbraDatabase
{
    private readonly List<ushort> _pool = new();
    private readonly ILogger<AbraDatabase> _logger;
    public AbraDatabase(ILogger<AbraDatabase> logger) => _logger = logger;
    public ushort? PickRandom(Random rng) => _pool.Count == 0 ? null : _pool[rng.Next(_pool.Count)];
    public int Count => _pool.Count;
    public void Reload() { /* abra_db.yml loader data-pending */ }
}

public sealed class MagicMushroomDatabase : IMagicMushroomDatabase
{
    private readonly List<ushort> _pool = new();
    private readonly ILogger<MagicMushroomDatabase> _logger;
    public MagicMushroomDatabase(ILogger<MagicMushroomDatabase> logger) => _logger = logger;
    public ushort? PickRandom(Random rng) => _pool.Count == 0 ? null : _pool[rng.Next(_pool.Count)];
    public int Count => _pool.Count;
    public void Reload() { /* magicmushroom_db.yml loader data-pending */ }
}

public sealed class ReadingSpellbookDatabase : IReadingSpellbookDatabase
{
    private readonly Dictionary<int, (ushort, ushort)> _map = new();
    private readonly ILogger<ReadingSpellbookDatabase> _logger;
    public ReadingSpellbookDatabase(ILogger<ReadingSpellbookDatabase> logger) => _logger = logger;
    public (ushort statusId, ushort skillId)? Get(int itemId)
        => _map.TryGetValue(itemId, out var v) ? v : null;
    public void Reload() { /* spellbook_db.yml loader data-pending */ }
}

public sealed class SkillArrowDatabase : ISkillArrowDatabase
{
    private readonly Dictionary<int, IReadOnlyList<(int, int)>> _recipes = new();
    private readonly ILogger<SkillArrowDatabase> _logger;
    public SkillArrowDatabase(ILogger<SkillArrowDatabase> logger) => _logger = logger;
    public IReadOnlyList<(int itemId, int qty)> Get(int sourceItemId)
        => _recipes.TryGetValue(sourceItemId, out var v) ? v : Array.Empty<(int, int)>();
    public void Reload() { /* arrow_db.yml loader data-pending */ }
}
