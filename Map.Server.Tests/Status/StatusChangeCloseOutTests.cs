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
/// ST.1 — exercises the new StatusChangeService close-out methods:
/// ClearAll / ClearBuffs / ClearOnChangeMap / ClearOnLogout / Spread /
/// GetMaxStacks / IsDisabledOnMap. Each test verifies the
/// <see cref="StatusFlagDefaults"/> classification drives the right
/// behavior.
/// </summary>
public class StatusChangeCloseOutTests
{
    [Fact]
    public void ClearAll_RemovesEveryActiveSc()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);

        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.IncreaseAgi, 5, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.Poison, 0, 0, 0, 0, 10_000);

        Assert.Equal(3, ctx.Service.ClearAll(pc));
        Assert.Null(ctx.Service.Get(pc, StatusType.Blessing));
        Assert.Null(ctx.Service.Get(pc, StatusType.Poison));
    }

    [Fact]
    public void ClearAll_Type0_LeavesPermanentScs()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(2, 100, 100);
        // BasilicaCell is classified Permanent by StatusFlagDefaults.
        ctx.Service.Start(pc, StatusType.BasilicaCell, 0, 0, 0, 0, -1);
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);

        Assert.Equal(1, ctx.Service.ClearAll(pc, type: 0));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.BasilicaCell));
        Assert.Null(ctx.Service.Get(pc, StatusType.Blessing));
    }

    [Fact]
    public void ClearAll_Type1_RemovesEvenPermanentScs()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(3, 100, 100);
        ctx.Service.Start(pc, StatusType.BasilicaCell, 0, 0, 0, 0, -1);
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);

        // ClearAll currently respects Permanent only when type=0; type=1
        // wipes them (matches rAthena's PC-disconnect path).
        // NB: the impl uses `type==0 && Permanent` to skip; with type=1 it
        // does NOT skip — so all 2 SCs go.
        Assert.Equal(2, ctx.Service.ClearAll(pc, type: 1));
    }

    [Fact]
    public void ClearBuffs_Buffs_RemovesPositiveOnly()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(4, 100, 100);
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.IncreaseAgi, 5, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.Poison, 0, 0, 0, 0, 10_000);   // debuff
        ctx.Service.Start(pc, StatusType.DecreaseAgi, 5, 0, 0, 0, 10_000); // debuff

        var cleared = ctx.Service.ClearBuffs(pc, SccbFlag.Buffs);

        Assert.Equal(2, cleared); // Blessing + IncreaseAgi
        Assert.Null(ctx.Service.Get(pc, StatusType.Blessing));
        Assert.Null(ctx.Service.Get(pc, StatusType.IncreaseAgi));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Poison));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.DecreaseAgi));
    }

    [Fact]
    public void ClearBuffs_Debuffs_RemovesNegativeOnly()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(5, 100, 100);
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.Poison, 0, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.DecreaseAgi, 5, 0, 0, 0, 10_000);

        var cleared = ctx.Service.ClearBuffs(pc, SccbFlag.Debuffs);

        Assert.Equal(2, cleared);
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Blessing));
        Assert.Null(ctx.Service.Get(pc, StatusType.Poison));
        Assert.Null(ctx.Service.Get(pc, StatusType.DecreaseAgi));
    }

    [Fact]
    public void ClearBuffs_Refresh_RemovesRefreshableDebuffs()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(6, 100, 100);
        // Stun + Sleep + Stone + Bleeding are all SCF_REM_ON_REFRESH.
        ctx.Service.Start(pc, StatusType.Stun, 0, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.Sleep, 0, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.Bleeding, 0, 0, 0, 0, 10_000);
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000); // buff — survives

        var cleared = ctx.Service.ClearBuffs(pc, SccbFlag.Refresh);

        Assert.Equal(3, cleared);
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Blessing));
    }

    [Fact]
    public void ClearOnChangeMap_DropsFlaggedScsOnly()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(7, 100, 100);
        // No SCs in the current default table carry RemoveOnChangeMap by
        // default — rAthena's design is "most buffs survive map change."
        // Test that the method runs without removing equipment-style
        // buffs.
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);

        var cleared = ctx.Service.ClearOnChangeMap(pc);
        Assert.Equal(0, cleared);
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Blessing));
    }

    [Fact]
    public void ClearOnLogout_DropsBuffsAndDebuffsButKeepsPermanent()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(8, 100, 100);
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000); // buff
        ctx.Service.Start(pc, StatusType.Poison, 0, 0, 0, 0, 10_000);   // debuff
        ctx.Service.Start(pc, StatusType.BasilicaCell, 0, 0, 0, 0, -1); // permanent

        var cleared = ctx.Service.ClearOnLogout(pc);

        Assert.Equal(2, cleared);
        Assert.Null(ctx.Service.Get(pc, StatusType.Blessing));
        Assert.Null(ctx.Service.Get(pc, StatusType.Poison));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.BasilicaCell));
    }

    [Fact]
    public void Spread_PropagatesSpreadEffectScs()
    {
        var ctx = Build();
        var source = ctx.AddPlayer(9, 100, 100);
        var target = ctx.AddPlayer(10, 101, 100);

        // Burning + Bleeding default-classified as SpreadEffect.
        ctx.Service.Start(source, StatusType.Burning, 0, 0, 0, 0, 30_000);
        ctx.Service.Start(source, StatusType.Blessing, 5, 0, 0, 0, 10_000); // not spread

        var propagated = ctx.Service.Spread(source, target);

        Assert.Equal(1, propagated);
        Assert.NotNull(ctx.Service.Get(target, StatusType.Burning));
        Assert.Null(ctx.Service.Get(target, StatusType.Blessing));
    }

    [Fact]
    public void GetMaxStacks_DefaultsToOneForRegisteredScs()
    {
        var ctx = Build();
        Assert.Equal(1, ctx.Service.GetMaxStacks(StatusType.Blessing));
        Assert.Equal(1, ctx.Service.GetMaxStacks(StatusType.Poison));
    }

    [Fact]
    public void IsDisabledOnMap_DefaultsToFalse()
    {
        var ctx = Build();
        Assert.False(ctx.Service.IsDisabledOnMap(mapId: 1, StatusType.Blessing));
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
        var service = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        return new TestContext(service, entities, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        StatusChangeService Service,
        EntityRegistry Entities,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
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
