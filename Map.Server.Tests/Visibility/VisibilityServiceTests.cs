using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Tests.Visibility;

public class VisibilityServiceTests
{
    private const short LargeMap = 200;

    [Fact]
    public void SendToSelf_DeliversToOwnSession()
    {
        var ctx = NewContext();
        var a = ctx.AddPlayer(100, 100, charId: 1);

        var packet = new ZC_AID { AccountId = 999 };
        ctx.Service.SendToSelf(a, packet);

        Assert.Single(ctx.Dispatcher.Sent);
        Assert.Equal(a.SessionId, ctx.Dispatcher.Sent[0].sessionId);
        Assert.Same(packet, ctx.Dispatcher.Sent[0].packet);
    }

    [Fact]
    public void SendToArea_DeliversToAllViewersIncludingSource()
    {
        var ctx = NewContext();
        var a = ctx.AddPlayer(100, 100, charId: 1);
        var b = ctx.AddPlayer(105, 100, charId: 2);
        var c = ctx.AddPlayer(50, 50, charId: 3);

        ctx.Service.SendToArea(a, new ZC_NOTIFY_TIME { ServerTick = 1 }, SendTarget.Area);

        var recipients = ctx.Dispatcher.Sent.Select(s => s.sessionId).ToHashSet();
        Assert.Contains(a.SessionId, recipients);
        Assert.Contains(b.SessionId, recipients);
        Assert.DoesNotContain(c.SessionId, recipients);
    }

    [Fact]
    public void SendToArea_AreaWos_ExcludesSource()
    {
        var ctx = NewContext();
        var a = ctx.AddPlayer(100, 100, charId: 1);
        var b = ctx.AddPlayer(105, 100, charId: 2);

        ctx.Service.SendToArea(a, new ZC_NOTIFY_TIME { ServerTick = 1 }, SendTarget.AreaWos);

        var recipients = ctx.Dispatcher.Sent.Select(s => s.sessionId).ToHashSet();
        Assert.DoesNotContain(a.SessionId, recipients);
        Assert.Contains(b.SessionId, recipients);
    }

    [Fact]
    public void SendToArea_EdgeOfRange_IsInView_OneMoreIsOut()
    {
        var ctx = NewContext();
        var a = ctx.AddPlayer(100, 100, charId: 1);
        var inRange = ctx.AddPlayer((short)(100 + VisibilityConfig.AreaSize), 100, charId: 2);
        var outRange = ctx.AddPlayer((short)(100 + VisibilityConfig.AreaSize + 1), 100, charId: 3);

        ctx.Service.SendToArea(a, new ZC_NOTIFY_TIME { ServerTick = 1 }, SendTarget.AreaWos);
        var recipients = ctx.Dispatcher.Sent.Select(s => s.sessionId).ToHashSet();

        Assert.Contains(inRange.SessionId, recipients);
        Assert.DoesNotContain(outRange.SessionId, recipients);
    }

    [Fact]
    public void ViewDiff_OneCellStep_NorthRowAppearsSouthRowDisappears()
    {
        var ctx = NewContext();
        // Step from (100,100) to (100,101). Old view y∈[86,114], new view y∈[87,115].
        // Cell y=115 enters, cell y=86 leaves; y=100 stays.
        var appearing = ctx.AddPlayer(100, 115, charId: 2);
        var vanishing = ctx.AddPlayer(100, 86, charId: 3);
        var staying = ctx.AddPlayer(100, 100, charId: 4);

        var nv = ctx.Service.NewlyVisible(ctx.MapId, 100, 100, 100, 101, EntityType.Pc);
        var nh = ctx.Service.NewlyInvisible(ctx.MapId, 100, 100, 100, 101, EntityType.Pc);

        Assert.Contains(nv, e => e.Id == appearing.Id);
        Assert.DoesNotContain(nv, e => e.Id == staying.Id);
        Assert.Contains(nh, e => e.Id == vanishing.Id);
        Assert.DoesNotContain(nh, e => e.Id == staying.Id);
    }

    [Fact]
    public void NotifySpawnedToArea_SendsStandEntryToViewers_ExcludingSource()
    {
        var ctx = NewContext();
        var entered = ctx.AddPlayer(100, 100, charId: 42, name: "Hero");
        var viewer = ctx.AddPlayer(105, 100, charId: 7);

        ctx.Service.NotifySpawnedToArea(entered);

        var recipients = ctx.Dispatcher.Sent.Where(s => s.packet is ZC_NOTIFY_STANDENTRY).ToList();
        Assert.Single(recipients);
        Assert.Equal(viewer.SessionId, recipients[0].sessionId);
    }

    [Fact]
    public void NotifyVanishedToArea_SendsVanishToViewers_ExcludingSource()
    {
        var ctx = NewContext();
        var gone = ctx.AddPlayer(100, 100, charId: 42);
        var viewer = ctx.AddPlayer(105, 100, charId: 7);

        ctx.Service.NotifyVanishedToArea(gone, VanishReason.Logout);

        var vanishPackets = ctx.Dispatcher.Sent.Where(s => s.packet is ZC_NOTIFY_VANISH).ToList();
        Assert.Single(vanishPackets);
        Assert.Equal(viewer.SessionId, vanishPackets[0].sessionId);
        var p = (ZC_NOTIFY_VANISH)vanishPackets[0].packet;
        Assert.Equal(gone.Id.Value, p.EntityId);
        Assert.Equal(VanishReason.Logout, p.Reason);
    }

    [Fact]
    public void NotifyMoveToArea_SendsMovePacketToViewers_ExcludingSource()
    {
        var ctx = NewContext();
        var walker = ctx.AddPlayer(100, 100, charId: 1);
        var viewer = ctx.AddPlayer(105, 100, charId: 2);

        ctx.Service.NotifyMoveToArea(walker, 100, 100, 101, 100, startTime: 12345u);

        var movePackets = ctx.Dispatcher.Sent.Where(s => s.packet is ZC_NOTIFY_MOVE).ToList();
        Assert.Single(movePackets);
        Assert.Equal(viewer.SessionId, movePackets[0].sessionId);
        var p = (ZC_NOTIFY_MOVE)movePackets[0].packet;
        Assert.Equal(walker.Id.Value, p.EntityId);
        Assert.Equal(12345u, p.StartTime);
    }

    [Fact]
    public void NotifyMoveDiff_WhenWalkerStaysInPlace_DoesNothing()
    {
        var ctx = NewContext();
        var walker = ctx.AddPlayer(100, 100, charId: 1);
        var viewer = ctx.AddPlayer(102, 100, charId: 2);

        ctx.Service.NotifyMoveDiff(walker, 100, 100, 100, 100);

        Assert.Empty(ctx.Dispatcher.Sent);
    }

    [Fact]
    public void NotifyMoveDiff_WalkingIntoView_TriggersStandEntryBothDirections()
    {
        // Walker at (100,100), other PC at (115,100) → distance 15 (out of range).
        // Walker steps one cell east → distance 14 (in range). Each side should
        // get exactly one STANDENTRY about the other.
        var ctx = NewContext();
        var walker = ctx.AddPlayer(100, 100, charId: 1, name: "Walker");
        var other = ctx.AddPlayer(115, 100, charId: 2, name: "Other");
        // Move walker in the registry to the new cell so the diff scan
        // sees the new spatial position.
        ctx.Registry.Move(walker.Id, 101, 100);

        ctx.Service.NotifyMoveDiff(walker, 100, 100, 101, 100);

        var toWalker = ctx.Dispatcher.Sent
            .Where(s => s.sessionId == walker.SessionId && s.packet is ZC_NOTIFY_STANDENTRY)
            .ToList();
        var toOther = ctx.Dispatcher.Sent
            .Where(s => s.sessionId == other.SessionId && s.packet is ZC_NOTIFY_STANDENTRY)
            .ToList();
        Assert.Single(toWalker);
        Assert.Single(toOther);
        Assert.Equal(other.CharacterId, ((ZC_NOTIFY_STANDENTRY)toWalker[0].packet).CharacterOrEntityId);
        Assert.Equal(walker.CharacterId, ((ZC_NOTIFY_STANDENTRY)toOther[0].packet).CharacterOrEntityId);
    }

    [Fact]
    public void NotifyMoveDiff_WalkingOutOfView_TriggersVanishBothDirections()
    {
        // Walker at (101,100) starts within range (14) of other at (115,100).
        // Walker steps west to (100,100) → distance 15, out of range. Both
        // sides get exactly one VANISH.
        var ctx = NewContext();
        var walker = ctx.AddPlayer(101, 100, charId: 1);
        var other = ctx.AddPlayer(115, 100, charId: 2);
        ctx.Registry.Move(walker.Id, 100, 100);

        ctx.Service.NotifyMoveDiff(walker, 101, 100, 100, 100);

        var toWalker = ctx.Dispatcher.Sent
            .Where(s => s.sessionId == walker.SessionId && s.packet is ZC_NOTIFY_VANISH)
            .Select(s => (ZC_NOTIFY_VANISH)s.packet).ToList();
        var toOther = ctx.Dispatcher.Sent
            .Where(s => s.sessionId == other.SessionId && s.packet is ZC_NOTIFY_VANISH)
            .Select(s => (ZC_NOTIFY_VANISH)s.packet).ToList();
        Assert.Single(toWalker);
        Assert.Single(toOther);
        Assert.Equal(VanishReason.Outsight, toWalker[0].Reason);
        Assert.Equal(VanishReason.Outsight, toOther[0].Reason);
        Assert.Equal(other.Id.Value, toWalker[0].EntityId);
        Assert.Equal(walker.Id.Value, toOther[0].EntityId);
    }

    [Fact]
    public void NotifyMoveDiff_StayingInView_BroadcastsNothing()
    {
        // Both players stay well within view. One step shouldn't trigger
        // anything since the view sets are identical.
        var ctx = NewContext();
        var walker = ctx.AddPlayer(100, 100, charId: 1);
        var stayer = ctx.AddPlayer(102, 100, charId: 2);
        ctx.Registry.Move(walker.Id, 101, 100);

        ctx.Service.NotifyMoveDiff(walker, 100, 100, 101, 100);

        Assert.Empty(ctx.Dispatcher.Sent);
    }

    [Fact]
    public void NotifyMoveDiff_NonPcWalkerMovingIntoView_NotifiesViewersOnly()
    {
        // Mob walks into a viewer's range. Viewer gets STANDENTRY; mob
        // (no session) gets nothing.
        var ctx = NewContext();
        var mob = new MobEntity(new EntityId(400_000_001), 1002, "Poring", ctx.MapId, 100, 100);
        ctx.Registry.Add(mob);
        var viewer = ctx.AddPlayer(115, 100, charId: 2);
        ctx.Registry.Move(mob.Id, 101, 100);

        ctx.Service.NotifyMoveDiff(mob, 100, 100, 101, 100);

        var standEntries = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_STANDENTRY).ToList();
        Assert.Single(standEntries);
        Assert.Equal(viewer.SessionId, standEntries[0].sessionId);
        Assert.Equal((byte)5, ((ZC_NOTIFY_STANDENTRY)standEntries[0].packet).ObjectType); // mob
    }

    [Fact]
    public void NotifySpawnedToArea_ForMob_SendsMobStandEntry()
    {
        var ctx = NewContext();
        var mob = new MobEntity(new EntityId(400_000_001), 1002, "Poring", ctx.MapId, 100, 100);
        ctx.Registry.Add(mob);
        var viewer = ctx.AddPlayer(105, 100, charId: 9);

        ctx.Service.NotifySpawnedToArea(mob);

        var p = (ZC_NOTIFY_STANDENTRY)ctx.Dispatcher.Sent
            .Single(s => s.packet is ZC_NOTIFY_STANDENTRY && s.sessionId == viewer.SessionId)
            .packet;
        Assert.Equal((byte)5, p.ObjectType);
        Assert.Equal(400_000_001, p.AccountId);
        Assert.Equal((short)1002, p.Job);
        Assert.Equal("Poring", p.Name);
    }

    private static TestContext NewContext()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, LargeMap, LargeMap, new byte[LargeMap * LargeMap]);
        var world = new StubWorldRegistry(map);
        var registry = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var service = new VisibilityService(registry, dispatcher);
        return new TestContext(service, registry, dispatcher, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        VisibilityService Service,
        EntityRegistry Registry,
        RecordingDispatcher Dispatcher,
        uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y, int charId, string name = "Player")
        {
            var p = new PlayerEntity(charId, charId * 10, name, Guid.NewGuid(), MapId, x, y);
            Registry.Add(p);
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
}
