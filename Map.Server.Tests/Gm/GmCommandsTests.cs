using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Gm;
using Map.Server.Gm.Commands;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Gm;

public class GmCommandsTests
{
    [Fact]
    public async Task Where_EchoesMapAndCellToCaller()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 60, charId: 1);
        var cmd = new WhereCommand(ctx.Visibility, ctx.World);

        await cmd.ExecuteAsync(caller, Array.Empty<string>(), CancellationToken.None);

        var msg = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_PLAYERCHAT)
            .Select(s => ((ZC_NOTIFY_PLAYERCHAT)s.packet).Message)
            .Single();
        Assert.Contains("test_map", msg);
        Assert.Contains("(50,60)", msg);
    }

    [Fact]
    public async Task Warp_TeleportsCallerAndBroadcastsVanishThenSpawn()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 50, charId: 1);
        var oldViewer = ctx.AddPlayer(52, 50, charId: 2); // sees caller pre-warp
        var newViewer = ctx.AddPlayer(150, 150, charId: 3); // sees caller post-warp
        var cmd = new WarpCommand(ctx.Entities, ctx.World, ctx.Visibility);

        await cmd.ExecuteAsync(caller, new[] { "150", "150" }, CancellationToken.None);

        Assert.Equal((short)150, caller.X);
        Assert.Equal((short)150, caller.Y);

        // Old neighbor should have received VANISH for the caller.
        var vanishes = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_VANISH && s.sessionId == oldViewer.SessionId)
            .Select(s => (ZC_NOTIFY_VANISH)s.packet)
            .ToList();
        Assert.Contains(vanishes, v => v.EntityId == caller.Id.Value && v.Reason == VanishReason.Teleport);

        // New neighbor should have received STANDENTRY for the caller.
        var standEntries = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_STANDENTRY && s.sessionId == newViewer.SessionId)
            .Select(s => (ZC_NOTIFY_STANDENTRY)s.packet)
            .ToList();
        Assert.Contains(standEntries, e => e.CharacterOrEntityId == caller.CharacterId);
    }

    [Fact]
    public async Task Warp_BadArgs_LeavesCallerInPlace()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 50, charId: 1);
        var cmd = new WarpCommand(ctx.Entities, ctx.World, ctx.Visibility);

        await cmd.ExecuteAsync(caller, new[] { "abc" }, CancellationToken.None);

        Assert.Equal((short)50, caller.X);
        Assert.Equal((short)50, caller.Y);
    }

    [Fact]
    public async Task KillMob_KillsNearestMobInView()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 50, charId: 1);
        var nearMob = new MobEntity(new EntityId(400_000_001), 1002, "Poring", ctx.MapId, 52, 50);
        var farMob = new MobEntity(new EntityId(400_000_002), 1002, "Poring", ctx.MapId, 60, 50);
        ctx.Entities.Add(nearMob);
        ctx.Entities.Add(farMob);
        // Register the spawn entry for KillMob's respawn schedule.
        var entry = new MobSpawnEntry
        {
            MobClassId = 1002, MapId = ctx.MapId, X = 52, Y = 50,
            Amount = 1, RespawnDelayMs = 5000, RespawnJitterMs = 0,
        };

        var cmd = new KillMobCommand(ctx.Entities, ctx.Spawn, ctx.Visibility);
        await cmd.ExecuteAsync(caller, Array.Empty<string>(), CancellationToken.None);

        Assert.Null(ctx.Entities.Get(nearMob.Id));
        Assert.NotNull(ctx.Entities.Get(farMob.Id));
    }

    [Fact]
    public async Task KillMob_NoMobInView_FeedbackOnly()
    {
        var ctx = NewContext();
        var caller = ctx.AddPlayer(50, 50, charId: 1);
        var cmd = new KillMobCommand(ctx.Entities, ctx.Spawn, ctx.Visibility);

        await cmd.ExecuteAsync(caller, Array.Empty<string>(), CancellationToken.None);

        var msg = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_PLAYERCHAT)
            .Select(s => ((ZC_NOTIFY_PLAYERCHAT)s.packet).Message)
            .Single();
        Assert.Contains("no mob in view", msg);
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
        var spawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, movement, visibility,
            new EntityIdAllocator(), NullLogger<MobSpawnService>.Instance, new Random(0));
        return new TestContext(
            entities, dispatcher, visibility, world, spawn, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        EntityRegistry Entities,
        RecordingDispatcher Dispatcher,
        IVisibilityService Visibility,
        IMapWorldRegistry World,
        IMobSpawnService Spawn,
        uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y, int charId)
        {
            var p = new PlayerEntity(charId, charId * 10, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            Entities.Add(p);
            return p;
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
