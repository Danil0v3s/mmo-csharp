using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Map.Server.Mob;

/// <summary>
/// Parses rAthena's <c>mob_db.yml</c> / <c>mob_db2.yml</c> documents into
/// <see cref="MobDbEntry"/> values. Hand-rolled event-driven walker over
/// YamlDotNet's low-level <c>IParser</c>.
///
/// Event-driven (not representation-model) because rAthena's shipped data
/// occasionally repeats a key inside a <c>RaceGroups:</c> / <c>Modes:</c>
/// block (e.g. <c>EP172ALPHA: true</c> twice). The representation model
/// rejects duplicate map keys; rAthena's loader silently keeps the last
/// occurrence, and we mirror that.
/// </summary>
public static class MobDbYamlReader
{
    public static IReadOnlyList<MobDbEntry> Read(TextReader yaml)
    {
        var parser = new Parser(yaml);
        Expect<StreamStart>(parser);
        if (parser.Current is StreamEnd) return Array.Empty<MobDbEntry>();
        Expect<DocumentStart>(parser);

        var root = ReadMapping(parser);
        Expect<DocumentEnd>(parser);

        if (!root.TryGetValue("Body", out var bodyValue) || bodyValue is not List<object?> body)
        {
            return Array.Empty<MobDbEntry>();
        }

        var entries = new List<MobDbEntry>(body.Count);
        foreach (var item in body)
        {
            if (item is Dictionary<string, object?> m) entries.Add(ParseEntry(m));
        }
        return entries;
    }

    // ---- entry-level parsing ----

    private static MobDbEntry ParseEntry(Dictionary<string, object?> m)
    {
        var id = GetInt(m, "Id")
            ?? throw new InvalidDataException("mob_db entry is missing required 'Id'");
        var aegisName = GetString(m, "AegisName")
            ?? throw new InvalidDataException($"mob_db entry {id} is missing 'AegisName'");
        var name = GetString(m, "Name") ?? aegisName;

        return new MobDbEntry
        {
            Id = id,
            AegisName = aegisName,
            Name = name,
            JapaneseName = GetString(m, "JapaneseName") ?? name,

            Level = GetInt(m, "Level") ?? 1,
            Hp = GetInt(m, "Hp") ?? 1,
            Sp = GetInt(m, "Sp") ?? 1,
            BaseExp = GetLong(m, "BaseExp") ?? 0,
            JobExp = GetLong(m, "JobExp") ?? 0,
            MvpExp = GetLong(m, "MvpExp") ?? 0,

            Attack = GetInt(m, "Attack") ?? 0,
            Attack2 = GetInt(m, "Attack2") ?? 0,
            Defense = GetInt(m, "Defense") ?? 0,
            MagicDefense = GetInt(m, "MagicDefense") ?? 0,
            Resistance = GetInt(m, "Resistance") ?? 0,
            MagicResistance = GetInt(m, "MagicResistance") ?? 0,

            Str = GetInt(m, "Str") ?? 1,
            Agi = GetInt(m, "Agi") ?? 1,
            Vit = GetInt(m, "Vit") ?? 1,
            Int = GetInt(m, "Int") ?? 1,
            Dex = GetInt(m, "Dex") ?? 1,
            Luk = GetInt(m, "Luk") ?? 1,

            AttackRange = GetInt(m, "AttackRange") ?? 0,
            SkillRange = GetInt(m, "SkillRange") ?? 0,
            ChaseRange = GetInt(m, "ChaseRange") ?? 0,

            Size = GetString(m, "Size") ?? "Small",
            Race = GetString(m, "Race") ?? "Formless",
            RaceGroups = GetFlagMap(m, "RaceGroups"),

            Element = GetString(m, "Element") ?? "Neutral",
            ElementLevel = GetInt(m, "ElementLevel") ?? 1,

            WalkSpeed = GetInt(m, "WalkSpeed") ?? 0,
            AttackDelay = GetInt(m, "AttackDelay") ?? 0,
            AttackMotion = GetInt(m, "AttackMotion") ?? 0,
            ClientAttackMotion = GetInt(m, "ClientAttackMotion") ?? 0,
            DamageMotion = GetInt(m, "DamageMotion") ?? 0,
            DamageTaken = GetInt(m, "DamageTaken") ?? 100,

            Ai = GetString(m, "Ai") ?? "06",
            Class = GetString(m, "Class") ?? "Normal",
            Modes = GetFlagMap(m, "Modes"),

            Drops = GetDrops(m, "Drops"),
            MvpDrops = GetDrops(m, "MvpDrops"),
        };
    }

    private static string? GetString(Dictionary<string, object?> m, string key) =>
        m.TryGetValue(key, out var v) ? v as string : null;

    private static int? GetInt(Dictionary<string, object?> m, string key) =>
        int.TryParse(GetString(m, key), out var v) ? v : null;

    private static long? GetLong(Dictionary<string, object?> m, string key) =>
        long.TryParse(GetString(m, key), out var v) ? v : null;

    private static IReadOnlyDictionary<string, bool> GetFlagMap(
        Dictionary<string, object?> m, string key)
    {
        if (!m.TryGetValue(key, out var v) || v is not Dictionary<string, object?> inner)
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }
        var result = new Dictionary<string, bool>(inner.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (k, val) in inner)
        {
            if (val is string s && bool.TryParse(s, out var b)) result[k] = b;
        }
        return result;
    }

    private static IReadOnlyList<MobDrop> GetDrops(Dictionary<string, object?> m, string key)
    {
        if (!m.TryGetValue(key, out var v) || v is not List<object?> seq)
        {
            return Array.Empty<MobDrop>();
        }
        var drops = new List<MobDrop>(seq.Count);
        foreach (var item in seq)
        {
            if (item is not Dictionary<string, object?> dm) continue;
            var itemName = GetString(dm, "Item");
            var rate = GetInt(dm, "Rate");
            if (itemName == null || rate == null) continue;

            drops.Add(new MobDrop(
                Item: itemName,
                Rate: rate.Value,
                StealProtected: bool.TryParse(GetString(dm, "StealProtected"), out var sp) && sp,
                RandomOptionGroup: GetString(dm, "RandomOptionGroup"),
                Index: GetInt(dm, "Index")));
        }
        return drops;
    }

    // ---- low-level YAML event walker (duplicate-key tolerant) ----

    private static Dictionary<string, object?> ReadMapping(IParser parser)
    {
        Expect<MappingStart>(parser);
        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        while (parser.Current is not MappingEnd)
        {
            var key = ((Scalar)parser.Current!).Value;
            parser.MoveNext();
            // Later occurrences of the same key silently overwrite — matches
            // rAthena's loader behavior on duplicate flags inside RaceGroups/Modes.
            map[key] = ReadNode(parser);
        }
        parser.MoveNext(); // consume MappingEnd
        return map;
    }

    private static List<object?> ReadSequence(IParser parser)
    {
        Expect<SequenceStart>(parser);
        var seq = new List<object?>();
        while (parser.Current is not SequenceEnd)
        {
            seq.Add(ReadNode(parser));
        }
        parser.MoveNext(); // consume SequenceEnd
        return seq;
    }

    private static object? ReadNode(IParser parser) => parser.Current switch
    {
        Scalar s => ReadScalar(parser, s),
        MappingStart => ReadMapping(parser),
        SequenceStart => ReadSequence(parser),
        _ => throw new InvalidDataException(
            $"Unexpected YAML event {parser.Current?.GetType().Name} at {parser.Current?.Start}"),
    };

    private static string ReadScalar(IParser parser, Scalar s)
    {
        var value = s.Value;
        parser.MoveNext();
        return value;
    }

    private static void Expect<T>(IParser parser) where T : ParsingEvent
    {
        if (parser.Current is null)
        {
            parser.MoveNext();
        }
        if (parser.Current is not T)
        {
            throw new InvalidDataException(
                $"Expected {typeof(T).Name} but got {parser.Current?.GetType().Name} at {parser.Current?.Start}");
        }
        parser.MoveNext();
    }
}
