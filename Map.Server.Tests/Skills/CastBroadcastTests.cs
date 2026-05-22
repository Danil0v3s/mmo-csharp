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
using Map.Server.Tests.Skills.Parity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// T5.3a — verifies <see cref="SkillCastService"/> emits the cast-start
/// frame via <see cref="ISkillClientService.BroadcastSkillCasting"/>
/// when the cast time is non-zero, and skips it for instant casts.
/// </summary>
public class CastBroadcastTests
{
    [Fact]
    public void NonZeroCastTime_EmitsCastingBroadcast()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(charId: 1, x: 50, y: 50);
        var target = ctx.AddPlayer(charId: 2, x: 51, y: 50);

        // AL_HEAL @ lvl 1 has cast time 1800 ms in the test seed.
        ctx.Skill.StartCast(pc, target.Id, SkillIds.AL_HEAL, skillLevel: 1);

        var castings = ctx.Recorder.Events.Where(e => e.Kind == "casting").ToList();
        Assert.Single(castings);
        Assert.Equal((int)pc.Id, castings[0].Data["src"]!);
        Assert.Equal((int)target.Id, castings[0].Data["target"]);
        Assert.Equal(SkillIds.AL_HEAL, (int)castings[0].Data["skillId"]!);
        Assert.Equal(1800, castings[0].Data["castTimeMs"]);
    }

    [Fact]
    public void InstantCast_DoesNotEmitCastingBroadcast()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(charId: 1, x: 50, y: 50);
        var target = ctx.AddPlayer(charId: 2, x: 51, y: 50);

        // AL_HEAL @ lvl 10 has cast time 0 in the test seed.
        ctx.Skill.StartCast(pc, target.Id, SkillIds.AL_HEAL, skillLevel: 10);

        Assert.Empty(ctx.Recorder.Events.Where(e => e.Kind == "casting"));
    }

    [Fact]
    public void GroundCast_NonZeroCastTime_EmitsCastingBroadcastWithCell()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(charId: 1, x: 50, y: 50);

        ctx.Skill.StartCastAt(pc, x: 53, y: 56, SkillIds.AL_HEAL, skillLevel: 1);

        var castings = ctx.Recorder.Events.Where(e => e.Kind == "casting").ToList();
        Assert.Single(castings);
        Assert.Null(castings[0].Data["target"]); // ground cast, no target entity
        Assert.Equal((short)53, castings[0].Data["x"]);
        Assert.Equal((short)56, castings[0].Data["y"]);
    }

    // ---- harness ----

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
        var itemDrops = new Map.Server.Items.ItemDropService(entities, ids, visibility,
            NullLogger<Map.Server.Items.ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, itemCatalog, itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var sc = new StatusChangeService(damage, entities,
            new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        var battle = new BattleCalculator(new Random(0));
        var db = new SkillDb();

        var recorder = new SkillTraceRecorder();
        var client = new RecordingSkillClientService(recorder);
        var behaviors = new SkillBehaviorRegistry(Array.Empty<SkillImpl>());

        var skill = new SkillCastService(
            db, entities,
            new SkillResolverRegistry(Array.Empty<ISkillResolver>()),
            NullLogger<SkillCastService>.Instance,
            mapFlags: null, maps: null, sc: sc, timing: null,
            behaviors: behaviors, battleCalc: battle, damage: damage, client: client);

        return new TestContext(skill, recorder, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillCastService Skill,
        SkillTraceRecorder Recorder,
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

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int id) => null;
        public MobDbEntry? GetByAegisName(string n) => null;
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
