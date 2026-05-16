namespace Map.Server.Mob;

/// <summary>
/// Static catalog entry for a mob class. Matches the schema of
/// rAthena's <c>mob_db.yml</c> (renewal). Field defaults follow rAthena's
/// <c>mob_parse_dbrow</c>; unset values use 0 / null / empty.
///
/// MS2 scope: fields needed for spawn placement + visibility broadcast.
/// Combat-relevant fields (atk/matk/def/res/stats) are still parsed and
/// stored, but the gameplay code that consumes them lands in MS3.
/// </summary>
public sealed record MobDbEntry
{
    public required int Id { get; init; }
    public required string AegisName { get; init; }
    public required string Name { get; init; }
    public string JapaneseName { get; init; } = string.Empty;

    public int Level { get; init; } = 1;
    public int Hp { get; init; } = 1;
    public int Sp { get; init; } = 1;
    public long BaseExp { get; init; }
    public long JobExp { get; init; }
    public long MvpExp { get; init; }

    public int Attack { get; init; }
    public int Attack2 { get; init; }
    public int Defense { get; init; }
    public int MagicDefense { get; init; }
    public int Resistance { get; init; }
    public int MagicResistance { get; init; }

    public int Str { get; init; } = 1;
    public int Agi { get; init; } = 1;
    public int Vit { get; init; } = 1;
    public int Int { get; init; } = 1;
    public int Dex { get; init; } = 1;
    public int Luk { get; init; } = 1;

    public int AttackRange { get; init; }
    public int SkillRange { get; init; }
    public int ChaseRange { get; init; }

    public string Size { get; init; } = "Small";
    public string Race { get; init; } = "Formless";

    /// <summary>
    /// Secondary race-group flags from <c>RaceGroups:</c>. Stored opaquely so
    /// MS3 combat can pull whichever ones it needs without us hardcoding the
    /// full set today.
    /// </summary>
    public IReadOnlyDictionary<string, bool> RaceGroups { get; init; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public string Element { get; init; } = "Neutral";
    public int ElementLevel { get; init; } = 1;

    public int WalkSpeed { get; init; }
    public int AttackDelay { get; init; }
    public int AttackMotion { get; init; }
    public int ClientAttackMotion { get; init; }
    public int DamageMotion { get; init; }
    public int DamageTaken { get; init; } = 100;

    /// <summary>Aegis AI behavior id (string like "01", "02", "21").</summary>
    public string Ai { get; init; } = "06";

    public string Class { get; init; } = "Normal";

    /// <summary>
    /// Behavior modes from the <c>Modes:</c> block. Stored opaquely. MS3 will
    /// expose typed wrappers (e.g. <c>IsMvp</c>, <c>IsAggressive</c>) as the
    /// combat / AI code lands.
    /// </summary>
    public IReadOnlyDictionary<string, bool> Modes { get; init; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MobDrop> Drops { get; init; } = Array.Empty<MobDrop>();
    public IReadOnlyList<MobDrop> MvpDrops { get; init; } = Array.Empty<MobDrop>();
}

/// <summary>
/// A single drop / MVP-drop entry from <c>Drops:</c> or <c>MvpDrops:</c>.
/// Stored verbatim — emission rules land in MS3 items.
/// </summary>
public sealed record MobDrop(
    string Item,
    int Rate,
    bool StealProtected = false,
    string? RandomOptionGroup = null,
    int? Index = null);
