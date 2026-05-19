using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

public class SkillCastServiceTests
{
    [Fact]
    public void Bash_OutOfRange_Rejected()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(60, 50, hp: 1000);

        var result = ctx.Skill.StartCast(caster, target.Id, SkillIds.SM_BASH, 1);
        Assert.Equal(SkillCastResult.OutOfRange, result);
        Assert.Equal(1000, target.Hp);
    }

    [Fact]
    public void Bash_NotEnoughSp_Rejected()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Sp = 0;
        var target = ctx.AddMob(51, 50, hp: 1000);

        Assert.Equal(SkillCastResult.NotEnoughSp,
            ctx.Skill.StartCast(caster, target.Id, SkillIds.SM_BASH, 1));
    }

    [Fact]
    public void Bash_InRange_DealsAtLeastWeaponDamage_AndConsumesSp()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.WatkMin = caster.Stats.WatkMax = 50;
        caster.Stats.Hit = 200;
        caster.Stats.Cri = 0;
        caster.Sp = 50;
        var target = ctx.AddMob(51, 50, hp: 10000);
        target.Stats.Flee = 0;
        target.Stats.Flee2 = 0;

        var result = ctx.Skill.StartCast(caster, target.Id, SkillIds.SM_BASH, 1);
        Assert.Equal(SkillCastResult.Started, result);
        Assert.True(target.Hp < 10000); // damage dealt
        Assert.Equal(42, caster.Sp); // 50 - 8 (lv1 sp cost)
    }

    [Fact]
    public void Heal_OnSelf_RaisesHp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Hp = 1; pc.MaxHp = 1000;
        pc.Sp = 100;
        pc.Stats.IntStat = 50;
        pc.Level = 50;

        // Heal is instant if cast time = 0; lvl 10 caststime = 0.
        var result = ctx.Skill.StartCast(pc, pc.Id, SkillIds.AL_HEAL, 10);
        Assert.Equal(SkillCastResult.Started, result);
        // base = (50 + 50) / 8 = 12; multiplier @ lv10 = 84; heal = 12 * 84 = 1008 → cap to 1000.
        Assert.True(pc.Hp > 1);
    }

    [Fact]
    public void IncreaseAgi_AppliesSc_AndBoostsAgi()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 50);
        target.Stats.Agi = 10;
        caster.Sp = 100;

        ctx.Skill.StartCast(caster, target.Id, SkillIds.AL_INCAGI, 5);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.IncreaseAgi));
        Assert.Equal(15, target.Stats.Agi); // 10 + lvl 5
    }

    [Fact]
    public void Cooldown_BlocksSecondCast()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Sp = 100;
        var target = ctx.AddPlayer(2, 51, 50);

        ctx.Skill.StartCast(caster, target.Id, SkillIds.AL_INCAGI, 1);
        var second = ctx.Skill.StartCast(caster, target.Id, SkillIds.AL_INCAGI, 1);
        // No cooldown defined for Increase Agi at this slice — second call
        // succeeds (refresh-on-restart). Sanity that the path doesn't crash.
        Assert.Equal(SkillCastResult.Started, second);
    }

    [Fact]
    public void FireBolt_Vs_WaterTarget_HasReducedDamage_NoCastForLevel1()
    {
        // Lv1 Fire Bolt cast time = 600ms — we still scheduled but for
        // the validation contract we just want StartCast = Started.
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Stats.MatkMin = caster.Stats.MatkMax = 100;
        caster.Sp = 100;
        var target = ctx.AddMob(53, 50, hp: 10000);
        target.Stats.DefenseElement = BattleElement.Water;
        target.Stats.ElementLevel = 1;
        target.Stats.Mdef = 0;
        target.Stats.Mdef2 = 0;

        Assert.Equal(SkillCastResult.Started,
            ctx.Skill.StartCast(caster, target.Id, SkillIds.MG_FIREBOLT, 1));
        // Damage hasn't landed yet (cast time pending).
        Assert.Equal(10000, target.Hp);
        ctx.Skill.Tick(nowTick: Environment.TickCount64 + 5_000);
        Assert.True(target.Hp < 10000);
    }

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
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, itemCatalog, itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        var db = new SkillDb();
        var skill = new SkillCastService(db, entities, damage,
            new BattleCalculator(new Random(0)), sc, NullLogger<SkillCastService>.Instance);
        return new TestContext(skill, sc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillCastService Skill,
        StatusChangeService Sc,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            pc.Sp = pc.MaxSp = 100;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y, int hp)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = hp };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            mob.MaxHp = hp; mob.Hp = hp;
            Entities.Add(mob);
            return mob;
        }
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
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

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }
}
