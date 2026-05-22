using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Handlers.ClifWire;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Visibility;
using Map.Server.World;
using Map.Server.Tests.Visibility;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// T5.3b — verifies <see cref="StatusChangeService"/> emits the
/// SC-icon broadcast on Start (with duration + val1..3) and on End
/// (active=false). Mirrors rAthena <c>clif_status_change</c> /
/// <c>clif_efst_status_change</c> behaviour.
/// </summary>
public class StatusChangeBroadcastTests
{
    [Fact]
    public void Start_FiresBroadcast_WithActiveAndDuration()
    {
        var ctx = Build();
        var pc = ctx.AddPc(charId: 1, x: 0, y: 0);

        ctx.Sc.Start(pc, StatusType.IncreaseAgi,
            val1: 10, val2: 0, val3: 0, val4: 0,
            durationMs: 30_000, source: null, nowTick: 0);

        var fired = ctx.Recorder.Calls.Where(c => c.Type == StatusType.IncreaseAgi).ToList();
        Assert.Single(fired);
        Assert.True(fired[0].Active);
        Assert.Equal(30_000, fired[0].TotalMs);
        Assert.Equal(10, fired[0].Val1);
    }

    [Fact]
    public void End_FiresBroadcast_WithActiveFalse()
    {
        var ctx = Build();
        var pc = ctx.AddPc(charId: 1, x: 0, y: 0);
        ctx.Sc.Start(pc, StatusType.IncreaseAgi, 10, 0, 0, 0, 30_000, null, 0);
        ctx.Recorder.Calls.Clear();

        ctx.Sc.End(pc, StatusType.IncreaseAgi);

        var fired = ctx.Recorder.Calls.Where(c => c.Type == StatusType.IncreaseAgi).ToList();
        Assert.Single(fired);
        Assert.False(fired[0].Active);
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
        var mobDb = new StubMobDb();
        var spawnRegistry = new MobSpawnRegistry();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var movement = new Map.Server.Movement.MovementService(entities, world, visibility,
            new Map.Server.Tests.Warps.NoOpWarpService(),
            new Map.Server.Tests.Warps.NoOpWarpDispatcher(),
            NullLogger<Map.Server.Movement.MovementService>.Instance);
        var itemDrops = new Map.Server.Items.ItemDropService(entities, ids, visibility,
            NullLogger<Map.Server.Items.ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, itemCatalog, itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);

        var recorder = new RecordingClifWire();
        // Default StatusEffectRegistry pre-registers IncreaseAgi
        // (and several dozen other SCs); no manual Register needed.
        var sc = new StatusChangeService(damage, entities,
            new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance, clif: recorder);

        return new TestContext(sc, recorder, entities, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        StatusChangeService Sc,
        RecordingClifWire Recorder,
        EntityRegistry Entities,
        uint MapId)
    {
        public PlayerEntity AddPc(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}",
                Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 100;
            Entities.Add(pc);
            return pc;
        }
    }

    private sealed class RecordingClifWire : IClifWireService
    {
        public sealed record Call(StatusType Type, bool Active, int TotalMs,
            int Val1, int Val2, int Val3);
        public List<Call> Calls { get; } = new();

        public void StatusChange(Entity target, StatusType type, bool active,
            int totalMs = 0, int val1 = 0, int val2 = 0, int val3 = 0)
            => Calls.Add(new Call(type, active, totalMs, val1, val2, val3));

        // Unused — no-op.
        public void MessageColor(PlayerEntity pc, uint c, string t) { }
        public void MobChat(MobEntity m, uint c, string t) { }
        public void DisplayMessage(PlayerEntity pc, string t) { }
        public void Broadcast(string t, uint c, byte type) { }
        public void Broadcast2(uint m, string t, uint c, byte type) { }
        public void Refresh(PlayerEntity pc) { }
        public void ChangeMap(PlayerEntity pc, string m, short x, short y) { }
        public void ClearUnitSingle(EntityId id, byte t, PlayerEntity a) { }
        public void ClearUnitArea(Entity t, byte type) { }
        public void AuthOk(PlayerEntity pc) { }
        public void AuthRefuse(int r) { }
        public void AuthFailFd(int fd, byte r) { }
        public void CompanionSpawn(Entity c, Entity m) { }
        public void CompanionVanish(Entity c) { }
        public void CompanionLevelUp(PlayerEntity m, Entity c, int lv) { }
        public void InventoryList(PlayerEntity o, InventoryListKind k) { }
    }

    private sealed class StubMobDb : Map.Server.Mob.IMobDb
    {
        public int Count => 0;
        public Map.Server.Mob.MobDbEntry? Get(int id) => null;
        public Map.Server.Mob.MobDbEntry? GetByAegisName(string n) => null;
        public IEnumerable<Map.Server.Mob.MobDbEntry> All() => Array.Empty<Map.Server.Mob.MobDbEntry>();
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
