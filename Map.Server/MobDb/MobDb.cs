using System.Reflection;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DbMob = Core.Database.Entities.MobEntity;

namespace Map.Server.Mob;

/// <summary>
/// Default <see cref="IMobDb"/>. Hydrates from <see cref="IMobRepository"/> at
/// construction — one round-trip pulls every row of the seeded <c>mob_db</c>
/// table (~2500 entries) and builds the id + aegis-name indexes. Mirrors
/// rAthena's <c>use_sql_db: yes</c> mode (mob.cpp <c>mob_read_sqldb</c>);
/// the SQL schema in <see cref="Core.Database.Entities.MobEntity"/> matches
/// rAthena's <c>mob_db_re</c> table column-for-column.
///
/// Singleton + scoped repository: we take <see cref="IServiceScopeFactory"/>
/// rather than <see cref="IMobRepository"/> directly to avoid the captive-
/// dependency lifecycle issue. <c>Reload()</c> creates a fresh scope.
/// </summary>
public sealed class MobDb : IMobDb
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MobDb> _logger;
    private volatile Snapshot _snapshot;

    public MobDb(IServiceScopeFactory scopeFactory, ILogger<MobDb> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _snapshot = LoadSnapshot();
    }

    public int Count => _snapshot.ById.Count;

    public MobDbEntry? Get(int classId) =>
        _snapshot.ById.TryGetValue(classId, out var e) ? e : null;

    public MobDbEntry? GetByAegisName(string aegisName) =>
        aegisName is null ? null :
        _snapshot.ByName.TryGetValue(aegisName, out var e) ? e : null;

    public IEnumerable<MobDbEntry> All() => _snapshot.ById.Values;

    public void Reload() => _snapshot = LoadSnapshot();

    private Snapshot LoadSnapshot()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMobRepository>();
        // Sync block here is intentional: this runs once at map-server boot
        // (and on /reloadmobdb), not on the hot tick, so blocking the
        // startup thread on ~2500 mob rows is fine.
        var rows = repository.GetAllAsync().GetAwaiter().GetResult();

        var byId = new Dictionary<int, MobDbEntry>(rows.Count);
        foreach (var row in rows)
        {
            byId[(int)row.Id] = MobDbRowMapper.Map(row);
        }
        var byName = new Dictionary<string, MobDbEntry>(byId.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in byId.Values)
        {
            byName[entry.AegisName] = entry;
        }
        _logger.LogInformation("MobDb loaded {Count} entries", byId.Count);
        return new Snapshot(byId, byName);
    }

    private sealed record Snapshot(
        IReadOnlyDictionary<int, MobDbEntry> ById,
        IReadOnlyDictionary<string, MobDbEntry> ByName);
}

/// <summary>
/// Translates a persisted <see cref="DbMob"/> row into the runtime
/// <see cref="MobDbEntry"/> shape that the rest of the map server consumes.
/// Drop slots (1-10) flatten into a list; race-group + mode bit columns
/// fold back into bool dictionaries (mirrors rAthena's serialized YAML
/// representation).
/// </summary>
internal static class MobDbRowMapper
{
    public static MobDbEntry Map(DbMob row) => new()
    {
        Id = (int)row.Id,
        AegisName = row.NameAegis,
        Name = row.NameEnglish,
        JapaneseName = row.NameJapanese ?? row.NameEnglish,

        Level = row.Level ?? 1,
        Hp = (int)(row.Hp ?? 1),
        Sp = (int)(row.Sp ?? 1),
        BaseExp = row.BaseExp ?? 0,
        JobExp = row.JobExp ?? 0,
        MvpExp = row.MvpExp ?? 0,

        Attack = row.Attack ?? 0,
        Attack2 = row.Attack2 ?? 0,
        Defense = row.Defense ?? 0,
        MagicDefense = row.MagicDefense ?? 0,
        Resistance = row.Resistance ?? 0,
        MagicResistance = row.MagicResistance ?? 0,

        Str = row.Str ?? 1,
        Agi = row.Agi ?? 1,
        Vit = row.Vit ?? 1,
        Int = row.Int ?? 1,
        Dex = row.Dex ?? 1,
        Luk = row.Luk ?? 1,

        AttackRange = row.AttackRange ?? 0,
        SkillRange = row.SkillRange ?? 0,
        ChaseRange = row.ChaseRange ?? 0,

        Size = row.Size ?? "Small",
        Race = row.Race ?? "Formless",
        RaceGroups = ExtractFlags(row, "Racegroup"),

        Element = row.Element ?? "Neutral",
        ElementLevel = row.ElementLevel ?? 1,

        WalkSpeed = row.WalkSpeed ?? 0,
        AttackDelay = row.AttackDelay ?? 0,
        AttackMotion = row.AttackMotion ?? 0,
        ClientAttackMotion = 0, // not on the seeded schema; rAthena fills only when overridden
        DamageMotion = row.DamageMotion ?? 0,
        DamageTaken = row.DamageTaken ?? 100,

        Ai = row.Ai ?? "06",
        Class = row.Class ?? "Normal",
        Modes = ExtractFlags(row, "Mode"),

        Drops = ExtractDrops(row),
        MvpDrops = ExtractMvpDrops(row),
    };

    // ---- bit-column → dictionary ----

    private static readonly PropertyInfo[] AllProps = typeof(DbMob).GetProperties();

    private static IReadOnlyDictionary<string, bool> ExtractFlags(DbMob row, string prefix)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in AllProps)
        {
            if (!prop.Name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            // Mode flag column names look like ModeAggressive; Racegroup
            // columns look like RacegroupClocktower. Strip the prefix.
            var key = prop.Name[prefix.Length..];
            if (key.Length == 0) continue;
            // We only want byte? bit-flag columns, not strings.
            if (prop.PropertyType != typeof(byte?)) continue;
            var raw = (byte?)prop.GetValue(row);
            if (raw is { } v && v != 0)
            {
                result[key] = true;
            }
        }
        return result;
    }

    // ---- drop slots → list ----

    private static IReadOnlyList<MobDrop> ExtractDrops(DbMob row)
    {
        var drops = new List<MobDrop>();
        for (var slot = 1; slot <= 10; slot++)
        {
            var item = GetProp<string?>(row, $"Drop{slot}Item");
            var rate = GetProp<ushort?>(row, $"Drop{slot}Rate");
            if (string.IsNullOrEmpty(item) || rate is null) continue;

            drops.Add(new MobDrop(
                Item: item!,
                Rate: rate.Value,
                StealProtected: (GetProp<byte?>(row, $"Drop{slot}Nosteal") ?? 0) != 0,
                RandomOptionGroup: GetProp<string?>(row, $"Drop{slot}Option"),
                Index: GetProp<byte?>(row, $"Drop{slot}Index")));
        }
        return drops;
    }

    private static IReadOnlyList<MobDrop> ExtractMvpDrops(DbMob row)
    {
        var drops = new List<MobDrop>();
        for (var slot = 1; slot <= 3; slot++)
        {
            var item = GetProp<string?>(row, $"Mvpdrop{slot}Item");
            var rate = GetProp<ushort?>(row, $"Mvpdrop{slot}Rate");
            if (string.IsNullOrEmpty(item) || rate is null) continue;

            drops.Add(new MobDrop(
                Item: item!,
                Rate: rate.Value,
                StealProtected: false, // MVP drops don't have nosteal
                RandomOptionGroup: GetProp<string?>(row, $"Mvpdrop{slot}Option"),
                Index: GetProp<byte?>(row, $"Mvpdrop{slot}Index")));
        }
        return drops;
    }

    private static T GetProp<T>(DbMob row, string name)
    {
        var prop = typeof(DbMob).GetProperty(name)
            ?? throw new InvalidOperationException(
                $"MobEntity is missing expected column property '{name}'");
        return (T)prop.GetValue(row)!;
    }
}
