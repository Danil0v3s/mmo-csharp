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

namespace Map.Server.Tests.Mob;

public class SummonAiServiceTests
{
    [Fact]
    public void Summon_FarFromMaster_StartsWalkingTowardMaster()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(50, 50);
        var summon = ctx.AddSummon(master.Id, 70, 50); // 20 cells away

        ctx.Ai.Tick(nowTick: 0);

        Assert.NotNull(summon.Walk);
    }

    [Fact]
    public void Summon_AssistsMasterTarget()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(50, 50);
        var summon = ctx.AddSummon(master.Id, 51, 50);
        var target = ctx.AddMob(52, 50, hp: 1000);

        // Master engages target.
        master.Attack = new AttackState { TargetId = target.Id, Continuous = true };

        ctx.Ai.Tick(nowTick: 0);
        Assert.NotNull(summon.Attack);
        Assert.Equal(target.Id, summon.Attack!.TargetId);
    }

    [Fact]
    public void Summon_LosesMaster_IsDespawned()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(50, 50);
        var summon = ctx.AddSummon(master.Id, 51, 50);

        ctx.Entities.Remove(master.Id);
        ctx.Ai.Tick(nowTick: 0);

        Assert.Null(ctx.Entities.Get(summon.Id));
    }

    [Fact]
    public void LooterPet_AdjacentItem_PicksItIntoBag()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(50, 50);
        var pet = ctx.AddLooterPet(master.Id, 51, 50, autoLootMax: 10);
        var item = ctx.AddFloorItem(51, 51); // adjacent to the pet

        ctx.Ai.Tick(nowTick: 0);

        Assert.Null(ctx.Entities.Get(item.Id));        // floor item removed
        Assert.Single(pet.LootItems);                  // into the bag
        Assert.Equal(909, pet.LootItems[0].ItemId);
    }

    [Fact]
    public void LooterPet_DistantItem_WalksTowardIt()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(50, 50);
        var pet = ctx.AddLooterPet(master.Id, 51, 50, autoLootMax: 10);
        ctx.AddFloorItem(53, 50); // a few cells away, within loot range

        ctx.Ai.Tick(nowTick: 0);

        Assert.NotNull(pet.Walk);                       // walking to fetch it
        Assert.Empty(pet.LootItems);
    }

    [Fact]
    public void LooterPet_FullBag_DoesNotLoot()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(50, 50);
        var pet = ctx.AddLooterPet(master.Id, 51, 50, autoLootMax: 1);
        pet.LootItems.Add(new MobLootSlot(501, 1, pet.ClassId)); // already at cap
        var item = ctx.AddFloorItem(51, 51);

        ctx.Ai.Tick(nowTick: 0);

        Assert.NotNull(ctx.Entities.Get(item.Id));      // left on the floor
        Assert.Single(pet.LootItems);
    }

    [Fact]
    public void NonLooterPet_IgnoresFloorItems()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(50, 50);
        var pet = ctx.AddLooterPet(master.Id, 51, 50, autoLootMax: 0); // not a looter
        var item = ctx.AddFloorItem(51, 51);

        ctx.Ai.Tick(nowTick: 0);

        Assert.NotNull(ctx.Entities.Get(item.Id));
        Assert.Empty(pet.LootItems);
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
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(new MobSpawnRegistry(), entities, world, mobDb,
            itemCatalog, itemDrops, movement, visibility, ids, new StatusCalcService(),
            NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var attack = new AttackService(entities, damage, movement, NullLogger<AttackService>.Instance);
        var looter = new MobLooterService(entities, NullLogger<MobLooterService>.Instance);
        var ai = new SummonAiService(entities, attack, movement, NullLogger<SummonAiService>.Instance, looter);
        return new TestContext(ai, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SummonAiService Ai,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y)
        {
            var pc = new PlayerEntity(1, 1, "Master", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }
        public MobEntity AddSummon(EntityId masterId, short x, short y)
        {
            var db = new MobDbEntry { Id = 9001, AegisName = "Pet", Name = "Pet", Hp = 100 };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 9001 };
            var summon = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(summon);
            summon.MasterId = masterId;
            Entities.Add(summon);
            return summon;
        }
        public MobEntity AddMob(short x, short y, int hp)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = hp };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
        }
        public PetEntity AddLooterPet(EntityId masterId, short x, short y, int autoLootMax)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 100 };
            var pet = new PetEntity(Ids.NextMob(), db, MapId, x, y) { MasterId = masterId, AutoLootMax = autoLootMax };
            new StatusCalcService().CalcMob(pet);
            Entities.Add(pet);
            return pet;
        }
        public FloorItemEntity AddFloorItem(short x, short y, int itemId = 909, short amount = 1)
        {
            var fi = new FloorItemEntity(Ids.NextMob(), itemId, amount, MapId, x, y, 0, 0, 0);
            Entities.Add(fi);
            return fi;
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
