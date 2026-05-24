using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// T2.4b acceptance tests — covers the first wave of per-SC
/// behaviors added to <see cref="StatusEffectRegistry"/> beyond the
/// initial 5 (Poison/Blessing/IncreaseAgi/DecreaseAgi/HealOverTime).
/// Cast-time-overlay SCs (Suffragium, Memorize, Slowcast, …) are
/// covered by <see cref="SkillCastFixScTests"/>.
/// </summary>
public class StatusEffectsExpansionTests
{
    // ---------- Crowd-control gates ----------

    [Fact]
    public void Stone_AttachesAndGatesCanAct()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);

        Assert.True(pc.CanAct(ctx.Service));
        ctx.Service.Start(pc, StatusType.Stone, 0, 0, 0, 0, durationMs: 10_000);
        Assert.False(pc.CanAct(ctx.Service));

        Assert.True(ctx.Service.End(pc, StatusType.Stone));
        Assert.True(pc.CanAct(ctx.Service));
    }

    [Fact]
    public void Silence_BlocksCastSkill_ButNotPlainAction()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);

        ctx.Service.Start(pc, StatusType.Silence, 0, 0, 0, 0, durationMs: 10_000);

        Assert.True(pc.CanAct(ctx.Service));         // SC_SILENCE keeps melee
        Assert.False(pc.CanCastSkill(ctx.Service));  // … but blocks magic.
    }

    // ---------- Damage-over-time ----------

    [Fact]
    public void Bleeding_TicksMaxHpOver100_Every10Seconds()
    {
        var ctx = Build();
        var mob = ctx.AddMob(101, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Service.Start(mob, StatusType.Bleeding, 0, 0, 0, 0, durationMs: 60_000, nowTick: 0);

        ctx.Service.Tick(nowTick: 5000);   // before period
        Assert.Equal(1000, mob.Hp);

        ctx.Service.Tick(nowTick: 10_500); // first tick fires
        Assert.Equal(1000 - 10, mob.Hp);

        ctx.Service.Tick(nowTick: 20_600); // second tick
        Assert.Equal(1000 - 20, mob.Hp);
    }

    [Fact]
    public void Burning_TicksThreePercent_Every3Seconds()
    {
        var ctx = Build();
        var mob = ctx.AddMob(101, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Service.Start(mob, StatusType.Burning, 0, 0, 0, 0, durationMs: 30_000, nowTick: 0);

        ctx.Service.Tick(nowTick: 3100);
        Assert.Equal(1000 - 30, mob.Hp);

        ctx.Service.Tick(nowTick: 6300);
        Assert.Equal(1000 - 60, mob.Hp);
    }

    [Fact]
    public void DeadlyPoison_TicksTwoPercent_EverySecond()
    {
        var ctx = Build();
        var mob = ctx.AddMob(101, 100);
        mob.Stats.MaxHp = 500;
        mob.Hp = 500;

        ctx.Service.Start(mob, StatusType.DeadlyPoison, 0, 0, 0, 0, durationMs: 10_000, nowTick: 0);

        ctx.Service.Tick(nowTick: 1100);
        Assert.Equal(500 - 10, mob.Hp);
    }

    // ---------- Stat buffs ----------

    [Fact]
    public void Adrenaline_AddsAspdRate_AndRevertsOnEnd()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.AspdRate = 0;

        ctx.Service.Start(pc, StatusType.Adrenaline, val1: 30, 0, 0, 0, durationMs: 10_000);
        Assert.Equal(30, pc.Stats.AspdRate);

        Assert.True(ctx.Service.End(pc, StatusType.Adrenaline));
        Assert.Equal(0, pc.Stats.AspdRate);
    }

    [Fact]
    public void Provoke_DropsDef_AndBoostsBatk()
    {
        // NS-3 wave 4a rAthena port: status.cpp:11299-11303
        //   val2 = 2+3*val1 = ATK% rate, val3 = 5+5*val1 = DEF% reduction.
        // val1=10: batk_delta = base_batk*(2+30)/100 = 100*32/100 = 32;
        //          def_delta  = base_def*(5+50)/100 = 50*55/100 = 27.
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Def = 50;
        pc.Stats.Batk = 100;

        ctx.Service.Start(pc, StatusType.Provoke, val1: 10, 0, 0, 0, durationMs: 10_000);

        Assert.Equal(50 - 27, pc.Stats.Def);
        Assert.Equal((ushort)(100 + 32), pc.Stats.Batk);

        Assert.True(ctx.Service.End(pc, StatusType.Provoke));
        Assert.Equal(50, pc.Stats.Def);
        Assert.Equal((ushort)100, pc.Stats.Batk);
    }

    [Fact]
    public void Concentrate_AddsAgiAndDex_OnPotion()
    {
        // NS-3 wave 4a rAthena port: status.cpp:11215-11221
        //   val2 = 2+val1 = percent applied to (agi - card_bonus_agi)/dex.
        // val1=5, base agi=50: delta = 50*7/100 = 3.5 → 3 (int trunc).
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Agi = 50;
        pc.Stats.Dex = 50;

        ctx.Service.Start(pc, StatusType.Concentrate, val1: 5, 0, 0, 0, durationMs: 10_000);

        Assert.Equal(50 + 3, pc.Stats.Agi);
        Assert.Equal(50 + 3, pc.Stats.Dex);

        Assert.True(ctx.Service.End(pc, StatusType.Concentrate));
        Assert.Equal(50, pc.Stats.Agi);
        Assert.Equal(50, pc.Stats.Dex);
    }

    [Fact]
    public void Concentration_LkSkill_AddsHit()
    {
        // NS-3 wave 4a rAthena port: status.cpp:11247-11257 (renewal)
        //   val2 = 5+val1*2 (Batk%), val3 = 10*val1 (Hit flat), val4 = 5+val1*2 (Def%-).
        // val1=5: hit_delta = 50; batk_delta = base*(5+10)/100 = 200*15/100 = 30;
        //         def_delta = base*(5+10)/100 = 50*15/100 = 7.
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Hit = 100;
        pc.Stats.Batk = 200;
        pc.Stats.Def = 50;

        ctx.Service.Start(pc, StatusType.Concentration, val1: 5, 0, 0, 0, durationMs: 10_000);
        Assert.Equal(100 + 50, pc.Stats.Hit);
        Assert.Equal((ushort)(200 + 30), pc.Stats.Batk);
        Assert.Equal(50 - 7, pc.Stats.Def);

        ctx.Service.End(pc, StatusType.Concentration);
        Assert.Equal(100, pc.Stats.Hit);
        Assert.Equal((ushort)200, pc.Stats.Batk);
        Assert.Equal(50, pc.Stats.Def);
    }

    [Fact]
    public void Angelus_AddsDef_PerLevel()
    {
        // NS-3 wave 4a rAthena port: status.cpp:11258-11260
        //   val2 = 5*val1 (Def increase, NOT Mdef2). status_calc_def adds val2.
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Def = 20;

        ctx.Service.Start(pc, StatusType.Angelus, val1: 10, 0, 0, 0, durationMs: 10_000);
        Assert.Equal(20 + 50, pc.Stats.Def);

        ctx.Service.End(pc, StatusType.Angelus);
        Assert.Equal(20, pc.Stats.Def);
    }

    [Fact]
    public void Assumptio_AddsDefAndMdef_AndReverts()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Def = 100;
        pc.Stats.Mdef = 50;

        // val1=5 → +Def*5/5 = +Def itself = +100 → Def 200; Mdef +50 → 100.
        ctx.Service.Start(pc, StatusType.Assumptio, val1: 5, 0, 0, 0, durationMs: 10_000);
        Assert.Equal(200, pc.Stats.Def);
        Assert.Equal(100, pc.Stats.Mdef);

        ctx.Service.End(pc, StatusType.Assumptio);
        Assert.Equal(100, pc.Stats.Def);
        Assert.Equal(50, pc.Stats.Mdef);
    }

    // ---------- Cast-time SCs (registry side only — overlays tested elsewhere) ----------

    [Fact]
    public void Suffragium_Memorize_Slowcast_Paralysis_Izayoi_Poembragi_AllRegister()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);

        foreach (var sc in new[]
        {
            StatusType.Suffragium, StatusType.Memorize, StatusType.Slowcast,
            StatusType.Paralysis, StatusType.Izayoi, StatusType.Poembragi,
        })
        {
            var instance = ctx.Service.Start(pc, sc, val1: 1, val2: 1, val3: 0, val4: 0, durationMs: 10_000);
            Assert.NotNull(instance);
            Assert.NotNull(ctx.Service.Get(pc, sc));
            Assert.True(ctx.Service.End(pc, sc));
        }
    }

    // ---------- Test rig ----------

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
        var service = new StatusChangeService(damage, entities, new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        return new TestContext(service, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        StatusChangeService Service,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 1000 };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
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
