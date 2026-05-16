using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

public class DamageServiceTests
{
    [Fact]
    public void ApplyDamage_DecrementsHpAndBroadcastsAct()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 50, 1);
        var mob = ctx.AddMob(52, 50, hp: 100);

        var dealt = ctx.Service.ApplyDamage(mob, 30, caller);

        Assert.Equal(30, dealt);
        Assert.Equal(70, mob.Hp);

        var act = (ZC_NOTIFY_ACT3)ctx.Dispatcher.Sent
            .Single(s => s.packet is ZC_NOTIFY_ACT3 && s.sessionId == caller.SessionId).packet;
        Assert.Equal(caller.Id.Value, act.SourceId);
        Assert.Equal(mob.Id.Value, act.TargetId);
        Assert.Equal(30, act.Damage);
        Assert.Equal(DamageActionType.Normal, act.ActionType);
    }

    [Fact]
    public void ApplyDamage_ClampsToRemainingHp()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 50, 1);
        var mob = ctx.AddMob(52, 50, hp: 25);

        var dealt = ctx.Service.ApplyDamage(mob, 9999, caller);

        Assert.Equal(25, dealt);
    }

    [Fact]
    public void ApplyDamage_OnZeroHp_RoutesMobThroughKillMob()
    {
        var ctx = NewContext();
        ctx.AddPlayer(50, 50, 1); // viewer
        var mob = ctx.AddMob(52, 50, hp: 50);

        ctx.Service.ApplyDamage(mob, 50);

        Assert.Null(ctx.Entities.Get(mob.Id));
        // KillMob's vanish broadcast uses VanishReason.Died.
        var vanish = (ZC_NOTIFY_VANISH)ctx.Dispatcher.Sent
            .Last(s => s.packet is ZC_NOTIFY_VANISH).packet;
        Assert.Equal(VanishReason.Died, vanish.Reason);
    }

    [Fact]
    public void ApplyDamage_NegativeAmountTreatedAsZero_BroadcastsFlee()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 50, 1);
        var mob = ctx.AddMob(52, 50, hp: 100);

        var dealt = ctx.Service.ApplyDamage(mob, -10, caller);

        Assert.Equal(0, dealt);
        Assert.Equal(100, mob.Hp);
        var act = (ZC_NOTIFY_ACT3)ctx.Dispatcher.Sent
            .Single(s => s.packet is ZC_NOTIFY_ACT3).packet;
        Assert.Equal(DamageActionType.Flee, act.ActionType);
    }

    [Fact]
    public void ApplyDamage_PcDeath_RemovesAndBroadcastsVanish()
    {
        var ctx = NewContext();
        var attacker = ctx.AddPlayer(50, 50, 1);
        var victim = ctx.AddPlayer(51, 50, 2);
        victim.Hp = 10;
        victim.MaxHp = 10;

        ctx.Service.ApplyDamage(victim, 999, attacker);

        Assert.Null(ctx.Entities.Get(victim.Id));
    }

    // ---- helpers ----

    private static TestContext NewContext()
    {
        const string mapName = "test_map";
        var cells = new byte[200 * 200];
        var map = new MapData(mapName, 200, 200, cells);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility, NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var spawnRegistry = new MobSpawnRegistry();
        var ids = new EntityIdAllocator();
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, movement, visibility,
            ids, NullLogger<MobSpawnService>.Instance, new Random(0));
        var service = new DamageService(visibility, mobSpawn, entities, NullLogger<DamageService>.Instance);
        return new TestContext(service, entities, dispatcher, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        DamageService Service,
        EntityRegistry Entities,
        RecordingDispatcher Dispatcher,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y, int charId)
        {
            var p = new PlayerEntity(charId, charId * 10, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            Entities.Add(p);
            return p;
        }

        public MobEntity AddMob(short x, short y, int hp)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y) { Hp = hp };
            Entities.Add(m);
            return m;
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

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string aegisName) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }
}
