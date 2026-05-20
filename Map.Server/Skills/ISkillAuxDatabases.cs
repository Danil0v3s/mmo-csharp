namespace Map.Server.Skills;

/// <summary>
/// Auxiliary skill databases — rAthena <c>AbraDatabase</c>,
/// <c>MagicMushroomDatabase</c>, <c>ReadingSpellbookDatabase</c>,
/// <c>SkillArrowDatabase</c>. Each is a small YAML / SQL table that
/// powers a single special-skill behavior:
///
/// <list type="bullet">
///   <item><b>Abra</b> — Abracadabra random-skill pool (SA_ABRACADABRA).</item>
///   <item><b>Magic Mushroom</b> — random spell-roll on SC_MAGICMUSHROOM tick.</item>
///   <item><b>Reading Spell Book</b> — Sage Spell Book consume → status grant.</item>
///   <item><b>Skill Arrow</b> — Arrow Crafting recipe input → output mapping.</item>
/// </list>
///
/// Each interface exposes <c>Get</c> + <c>Reload</c>. Until the YAML
/// loader lands, every <c>Get</c> returns null / empty and Reload is a
/// no-op — the canonical entry point keeps callers from re-implementing
/// the table lookup inline.
/// </summary>
public interface IAbraDatabase
{
    /// <summary>One of the Abra-pool skill ids. Null if the table hasn't loaded yet.</summary>
    ushort? PickRandom(Random rng);
    int Count { get; }
    void Reload();
}

public interface IMagicMushroomDatabase
{
    /// <summary>One of the Magic-Mushroom proc skills. Null if empty.</summary>
    ushort? PickRandom(Random rng);
    int Count { get; }
    void Reload();
}

public interface IReadingSpellbookDatabase
{
    /// <summary>The status grant + spell pair for the given item id.</summary>
    (ushort statusId, ushort skillId)? Get(int itemId);
    void Reload();
}

public interface ISkillArrowDatabase
{
    /// <summary>The arrow output list for the given source item.</summary>
    IReadOnlyList<(int itemId, int qty)> Get(int sourceItemId);
    void Reload();
}
