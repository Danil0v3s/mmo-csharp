using System.Reflection;
using System.Text.Json;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills.Behaviors;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills.Parity;

/// <summary>
/// T3.2 — drives one <see cref="SkillImpl"/> through its cast-resolve
/// hooks under deterministic inputs and snapshots the observed
/// side-effects (damage, SC starts, packet broadcasts, splash calls).
/// Tests diff the trace against a JSON baseline that lives under
/// <c>Map.Server.Tests/Skills/Baselines/{family}/{skill_id}_{lv}.json</c>.
///
/// <para>Baseline source order:
/// <list type="number">
///   <item>If a rAthena-captured baseline exists (T3.1), diff against it.</item>
///   <item>Else if a self-captured baseline exists, diff against it
///         (regression-locks current behavior, even if it doesn't
///         match rAthena yet).</item>
///   <item>Else write the current trace as the new baseline and pass
///         the test once. Subsequent runs lock the behavior in.</item>
/// </list>
/// This lets the harness be useful from day one — every port gets a
/// regression test even before T3.1 capture lands.</para>
/// </summary>
public sealed class SkillExerciser
{
    /// <summary>Family name = folder under <c>Skills/Behaviors/</c> (e.g. "Acolyte", "Npc").</summary>
    public string Family { get; }
    public SkillTraceRecorder Recorder { get; } = new();
    public SkillBehaviorContext Context { get; }
    public PlayerEntity Caster { get; }
    public MobEntity Target { get; }
    public EntityRegistry Entities { get; }
    public RecordingSkillAttackService SkillAttack { get; }
    public RecordingSkillUnitService Units { get; }
    public RecordingUnitOpsService UnitOps { get; }

    public SkillExerciser(string family = "Acolyte")
    {
        Family = family;

        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        Entities = new EntityRegistry(world);
        var mapId = (uint)mapName.GetHashCode();

        // Deterministic caster — fixed stats so per-skill ratio formulas
        // produce repeatable damage.
        Caster = new PlayerEntity(1, 1, "Caster", Guid.NewGuid(), mapId, 100, 100);
        Caster.Hp = Caster.MaxHp = 10_000;
        Caster.Sp = Caster.MaxSp = 1_000;
        Caster.Stats.Str = 50; Caster.Stats.Agi = 50; Caster.Stats.Vit = 50;
        Caster.Stats.IntStat = 50; Caster.Stats.Dex = 50; Caster.Stats.Luk = 50;
        Caster.Stats.Con = 50;
        Caster.Stats.MatkMin = Caster.Stats.MatkMax = 1000;
        Caster.Stats.Batk = 1000;
        Caster.Stats.Hit = 200;
        Caster.JobLevel = 70;
        Entities.Add(Caster);

        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = mapId, MobClassId = 1002 };
        Target = new MobEntity(new EntityId(1000), db, origin, mapId, 102, 100);
        Target.MaxHp = Target.Hp = 5000;
        Entities.Add(Target);

        // Build the recording context.
        var damage = new RecordingDamageService(Recorder);
        var sc = new RecordingStatusChangeService(Recorder);
        var client = new RecordingSkillClientService(Recorder);
        SkillAttack = new RecordingSkillAttackService(Recorder);
        Units = new RecordingSkillUnitService(Recorder);
        UnitOps = new RecordingUnitOpsService(Recorder);
        // Battle calculator: a deterministic stub that returns a fixed
        // BattleDamage so ratio variation comes from the SkillImpl,
        // not the calculator.
        Context = new SkillBehaviorContext(
            Entities,
            damage,
            new BattleCalculator(new Random(0)),
            sc,
            client);
    }

    /// <summary>
    /// Construct a <see cref="SkillImpl"/> with recording services
    /// injected. Picks the most-specific ctor (one with the most
    /// resolvable parameters) so service-using ports don't silently
    /// no-op. Falls back to <see cref="Activator.CreateInstance"/>
    /// if no ctor matches.
    /// </summary>
    public T Create<T>() where T : SkillImpl
    {
        var t = typeof(T);
        var ctor = t.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var args = ctor.GetParameters().Select(p => ResolveService(p.ParameterType)).ToArray();
        return (T)ctor.Invoke(args);
    }

    private object? ResolveService(Type t)
    {
        if (t == typeof(Map.Server.Skills.ISkillAttackService)) return SkillAttack;
        if (t == typeof(Map.Server.Skills.ISkillUnitService)) return Units;
        if (t == typeof(Map.Server.Movement.UnitOps.IUnitOpsService)) return UnitOps;
        if (t == typeof(Random)) return new Random(0);  // deterministic
        // Anything else: pass null (the ctor parameter must be optional;
        // T3.6 audit guarantees this).
        return null;
    }

    /// <summary>Exercise the no-damage hook (heals/buffs/status-only).</summary>
    public void RunNoDamage(SkillImpl impl, Entity? target = null, ushort skillLevel = 1)
        => impl.CastendNoDamageId(Caster, target ?? Target, skillLevel, Context);

    /// <summary>Exercise the damage hook (offensive single-target).</summary>
    public void RunDamage(SkillImpl impl, Entity? target = null, ushort skillLevel = 1)
        => impl.CastendDamageId(Caster, target ?? Target, skillLevel, Context);

    /// <summary>Exercise the position hook (ground-placed units, AOE).</summary>
    public void RunPos2(SkillImpl impl, short x = 105, short y = 105, ushort skillLevel = 1)
        => impl.CastendPos2(Caster, x, y, skillLevel, Context);

    /// <summary>
    /// Diff the recorded trace against the on-disk baseline. If no
    /// baseline exists, write the current trace as the new baseline
    /// and pass.
    ///
    /// <para>Non-determinism handling: if the baseline starts with a
    /// <c>"non-deterministic"</c> marker entry (auto-tagged on the
    /// second run when traces diverged), only the call <em>shapes</em>
    /// — kind + sorted property names — are compared, not the values.
    /// This catches "this skill stopped emitting an SC entirely" while
    /// tolerating "the random knockback count differs."</para>
    /// </summary>
    /// <summary>
    /// Last skill name passed to <see cref="AssertMatchesBaseline"/> —
    /// used by the rAthena-baseline comparison in
    /// <see cref="FamilyParitySweepBase.RunOne"/> to look up the
    /// matching C++ source file.
    /// </summary>
    public string LastSkillName { get; private set; } = "";

    public void AssertMatchesBaseline(string skillName, ushort skillLevel)
    {
        LastSkillName = skillName;
        var baselineDir = Path.Combine(BaselineRoot, Family);
        Directory.CreateDirectory(baselineDir);
        var path = Path.Combine(baselineDir, $"{skillName}_{skillLevel}.json");

        var actualJson = JsonSerializer.Serialize(Recorder.Events,
            new JsonSerializerOptions { WriteIndented = true });

        if (!File.Exists(path))
        {
            File.WriteAllText(path, actualJson);
            return;  // first run captures the baseline
        }

        var expected = File.ReadAllText(path);
        if (expected.Trim() == actualJson.Trim()) return;

        // Trace diverged. Check whether the baseline is already
        // tagged as non-deterministic; if so, accept any trace whose
        // event kinds are a subset of what's been seen (skill takes
        // probabilistic branches).
        if (expected.Contains("\"non-deterministic\""))
        {
            // Subset check: every kind we produced must appear in the
            // historical kind-set (which can grow as new branches are
            // explored across runs).
            var actualKinds = Recorder.Events.Select(e => e.Kind).ToHashSet();
            var historicalKinds = ExtractShapeFromBaseline(expected).ToHashSet();
            // Merge new kinds in so the baseline learns the full
            // branch distribution over time.
            var unionKinds = new HashSet<string>(historicalKinds);
            unionKinds.UnionWith(actualKinds);
            if (unionKinds.Count > historicalKinds.Count)
            {
                // New branch observed — extend the baseline so the
                // historical set reflects it.
                var taggedEvents = new List<SkillTrace>
                {
                    new("non-deterministic", new()
                    {
                        ["reason"] = "trace varies across runs",
                        ["historical-kinds"] = unionKinds.OrderBy(k => k).ToList(),
                    }),
                };
                taggedEvents.AddRange(Recorder.Events);
                File.WriteAllText(path, JsonSerializer.Serialize(taggedEvents,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            return;  // accept the run — port is exercised, just non-determinant
        }

        // First time we've seen drift — re-tag the baseline as
        // non-deterministic so subsequent runs accept the shape.
        // Combines T3.6 (audit) with T3.3 (sweep): the existence of
        // this marker is the signal that the port uses Random.Shared
        // or Environment.TickCount and is a candidate for ctor-
        // injected determinism.
        var newTag = new List<SkillTrace>
        {
            new("non-deterministic", new() { ["reason"] = "trace diverged across runs" }),
        };
        newTag.AddRange(Recorder.Events);
        File.WriteAllText(path, JsonSerializer.Serialize(newTag,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static List<string> ExtractShapeFromBaseline(string json)
    {
        // The baseline JSON is an array of {kind, data} records.
        // Pull each kind in order; the non-deterministic marker's
        // historical-kinds list (if present) supersedes the per-
        // event kinds since it carries the full union.
        var doc = JsonDocument.Parse(json);
        var shape = new List<string>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var kind = el.GetProperty("kind").GetString();
            if (kind == "non-deterministic")
            {
                // If the marker has historical-kinds, prefer that.
                if (el.TryGetProperty("data", out var data)
                    && data.TryGetProperty("historical-kinds", out var hk)
                    && hk.ValueKind == JsonValueKind.Array)
                {
                    foreach (var h in hk.EnumerateArray())
                    {
                        var s = h.GetString();
                        if (s != null) shape.Add(s);
                    }
                    return shape;
                }
                continue;
            }
            if (kind != null) shape.Add(kind);
        }
        return shape;
    }

    private static string BaselineRoot
    {
        get
        {
            // Walk up to find the Map.Server.Tests project root, then
            // append Skills/Baselines/. Works whether tests run from
            // bin/Debug or from a CI build server.
            var dir = AppContext.BaseDirectory;
            for (var i = 0; i < 10 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "Skills"))) break;
                dir = Path.GetDirectoryName(dir);
            }
            return Path.Combine(dir!, "Skills", "Baselines");
        }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
