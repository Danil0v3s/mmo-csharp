using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Resolvers;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Visibility;
using Map.Server.World;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// T4.9g — verifies <see cref="SkillCastService.StartCastAt"/> routes
/// ground-targeted casts to <see cref="SkillImpl.CastendPos2"/> with
/// the cell the caller passed in (not the caster's own cell).
///
/// <para>The picker test layer already exercises the call shape from
/// the mob side (<see cref="MobSkillTargetResolver"/> resolves MST_AROUND
/// modes to a cell). These tests cover the receiver side: the cell
/// the SkillImpl sees must match what the caller asked for, and the
/// pre-flight gates (range, level, SP, cooldown) must still apply.</para>
/// </summary>
public class GroundCellCastTests
{
    [Fact]
    public void StartCastAt_RoutesCellToPlugin()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(charId: 1, x: 50, y: 50);

        var recorder = ctx.Recorder;
        // AL_HEAL @ lvl 10 has 0 cast time in the test db, so the
        // ResolveSkillAt → CastendPos2 hop fires synchronously.
        var result = ctx.Skill.StartCastAt(pc, x: 53, y: 56, SkillIds.AL_HEAL, skillLevel: 10);

        Assert.Equal(SkillCastResult.Started, result);
        Assert.Single(recorder.Calls);
        var call = recorder.Calls[0];
        Assert.Equal(53, call.x);
        Assert.Equal(56, call.y);
        Assert.Equal(10, call.skillLevel);
        Assert.Same(pc, call.src);
    }

    [Fact]
    public void StartCastAt_OutOfRange_Refused()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(charId: 1, x: 50, y: 50);

        var recorder = ctx.Recorder;
        // SkillDb's AL_HEAL has Range=9 in the default seed; cast at
        // (100, 100) is way outside.
        var result = ctx.Skill.StartCastAt(pc, x: 100, y: 100, SkillIds.AL_HEAL, skillLevel: 1);

        Assert.Equal(SkillCastResult.OutOfRange, result);
        Assert.Empty(recorder.Calls);
    }

    [Fact]
    public void StartCastAt_UnknownSkill_Refused()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(charId: 1, x: 50, y: 50);

        var result = ctx.Skill.StartCastAt(pc, x: 50, y: 50, skillId: 0xFFFE, skillLevel: 1);
        Assert.Equal(SkillCastResult.UnknownSkill, result);
    }

    /// <summary>
    /// Picker side smoke: the mob picker pushes ground casts at a cell
    /// distinct from the mob's own (x, y). The plugin must see the
    /// chosen cell, NOT the caster's. This guards against the legacy
    /// default-method that delegated to <c>StartCast(self)</c>.
    /// </summary>
    [Fact]
    public void StartCastAt_CellDiffersFromSource()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(charId: 1, x: 50, y: 50);

        var recorder = ctx.Recorder;
        ctx.Skill.StartCastAt(pc, x: 51, y: 49, SkillIds.AL_HEAL, skillLevel: 10);

        Assert.Single(recorder.Calls);
        Assert.NotEqual(pc.X, recorder.Calls[0].x);
        Assert.NotEqual(pc.Y, recorder.Calls[0].y);
    }

    // --- helpers ---

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(),
            NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var spawnRegistry = new MobSpawnRegistry();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new Map.Server.Items.ItemDropService(entities, ids, visibility, NullLogger<Map.Server.Items.ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, itemCatalog, itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var sc = new StatusChangeService(damage, entities,
            new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        var battle = new BattleCalculator(new Random(0));
        var db = new SkillDb();

        // Pre-register the AL_HEAL recorder plugin so all tests share
        // the same recorder instance via TestContext.Recorder.
        var recorder = new CellRecordingPlugin(skillId: SkillIds.AL_HEAL);
        var behaviors = new SkillBehaviorRegistry(new SkillImpl[] { recorder });

        var skill = new SkillCastService(
            db, entities,
            new SkillResolverRegistry(Array.Empty<ISkillResolver>()),
            NullLogger<SkillCastService>.Instance,
            mapFlags: null, maps: null, sc: sc, timing: null,
            behaviors: behaviors, battleCalc: battle, damage: damage, client: null);
        return new TestContext(skill, recorder, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillCastService Skill,
        CellRecordingPlugin Recorder,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}",
                Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            pc.Sp = pc.MaxSp = 100;
            pc.LearnedSkills[SkillIds.AL_HEAL] = 10;
            Entities.Add(pc);
            return pc;
        }
    }

    /// <summary>
    /// SkillImpl that just records every (x, y, level) it sees in
    /// CastendPos2 so the test can assert on the cell passed in.
    /// </summary>
    private sealed class CellRecordingPlugin : SkillImpl
    {
        public List<(Entity src, short x, short y, ushort skillLevel)> Calls { get; } = new();
        public CellRecordingPlugin(ushort skillId) : base(skillId) { }
        public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
            => Calls.Add((src, x, y, skillLevel));
    }

    // --- minimal stubs shared with SkillCastServiceTests ---

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int id) => null;
        public MobDbEntry? GetByAegisName(string aegisName) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
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
    private sealed class EmptyItemCatalog : Map.Server.Items.IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }
}
