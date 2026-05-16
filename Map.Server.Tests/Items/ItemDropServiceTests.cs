using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Items;

public class ItemDropServiceTests
{
    [Fact]
    public void DropOnFloor_RegistersEntityAndBroadcastsFallEntry()
    {
        var ctx = NewContext();
        var viewer = ctx.AddPlayer(50, 50, charId: 1);

        var itemId = ctx.Service.DropOnFloor(ctx.MapId, 50, 50, itemId: 501, amount: 5);

        var entity = ctx.Entities.Get(itemId) as ItemEntity;
        Assert.NotNull(entity);
        Assert.Equal(501, entity!.ItemId);
        Assert.Equal((short)5, entity.Amount);

        var fallEntry = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_ITEM_FALL_ENTRY)
            .Select(s => (sessionId: s.sessionId, p: (ZC_ITEM_FALL_ENTRY)s.packet))
            .ToList();
        Assert.Single(fallEntry);
        Assert.Equal(viewer.SessionId, fallEntry[0].sessionId);
        Assert.Equal(501, fallEntry[0].p.ItemId);
        Assert.Equal((short)5, fallEntry[0].p.Amount);
    }

    [Fact]
    public void TryPickup_Ok_RemovesEntityAndBroadcastsDisappear()
    {
        var ctx = NewContext();
        var picker = ctx.AddPlayer(50, 50, charId: 1);
        var viewer = ctx.AddPlayer(51, 50, charId: 2);
        var itemId = ctx.Service.DropOnFloor(ctx.MapId, 50, 50, itemId: 501, amount: 1);
        ctx.Dispatcher.Sent.Clear();

        var result = ctx.Service.TryPickup(picker, itemId, out var item);

        Assert.Equal(IItemDropService.PickupResult.Ok, result);
        Assert.NotNull(item);
        Assert.Equal(501, item!.ItemId);
        Assert.Null(ctx.Entities.Get(itemId));

        var disappears = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_ITEM_DISAPPEAR)
            .Select(s => s.sessionId).ToHashSet();
        Assert.Contains(picker.SessionId, disappears);
        Assert.Contains(viewer.SessionId, disappears);
    }

    [Fact]
    public void TryPickup_OutOfRange_LeavesItem()
    {
        var ctx = NewContext();
        var picker = ctx.AddPlayer(50, 50, charId: 1);
        var itemId = ctx.Service.DropOnFloor(ctx.MapId, 60, 50, itemId: 501, amount: 1);

        var result = ctx.Service.TryPickup(picker, itemId, out _);

        Assert.Equal(IItemDropService.PickupResult.OutOfRange, result);
        Assert.NotNull(ctx.Entities.Get(itemId));
    }

    [Fact]
    public void TryPickup_UnknownEntity_ReturnsNotFound()
    {
        var ctx = NewContext();
        var picker = ctx.AddPlayer(50, 50, charId: 1);

        var result = ctx.Service.TryPickup(picker, new EntityId(2_000_999_999), out _);

        Assert.Equal(IItemDropService.PickupResult.ItemNotFound, result);
    }

    [Fact]
    public void Tick_AfterLifetime_DespawnsItem()
    {
        // Lifetime of 0 means anything older than 0ms is expired — first
        // tick removes the item.
        var ctx = NewContext(lifetimeMs: 0);
        var viewer = ctx.AddPlayer(50, 50, charId: 1);
        var itemId = ctx.Service.DropOnFloor(ctx.MapId, 50, 50, itemId: 501, amount: 1);
        Assert.NotNull(ctx.Entities.Get(itemId));

        // Force a tick to advance Environment.TickCount64 past the drop time.
        Thread.Sleep(5);
        ctx.Service.Tick();

        Assert.Null(ctx.Entities.Get(itemId));
        Assert.Contains(ctx.Dispatcher.Sent,
            s => s.packet is ZC_ITEM_DISAPPEAR && s.sessionId == viewer.SessionId);
    }

    [Fact]
    public void Tick_BeforeLifetime_KeepsItem()
    {
        // Long lifetime: tick shouldn't remove anything.
        var ctx = NewContext(lifetimeMs: 60_000);
        ctx.AddPlayer(50, 50, charId: 1);
        var itemId = ctx.Service.DropOnFloor(ctx.MapId, 50, 50, itemId: 501, amount: 1);

        ctx.Service.Tick();

        Assert.NotNull(ctx.Entities.Get(itemId));
    }

    // ---- helpers ----

    private static TestContext NewContext(int lifetimeMs = IItemDropService.DefaultLifetimeMs)
    {
        const string mapName = "test_map";
        var cells = new byte[200 * 200];
        var map = new MapData(mapName, 200, 200, cells);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var ids = new EntityIdAllocator();
        var service = new ItemDropService(
            entities, ids, visibility,
            NullLogger<ItemDropService>.Instance, lifetimeMs);
        return new TestContext(service, entities, dispatcher, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        ItemDropService Service,
        EntityRegistry Entities,
        RecordingDispatcher Dispatcher,
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
}
