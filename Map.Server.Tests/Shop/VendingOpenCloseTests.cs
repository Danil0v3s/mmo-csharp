using System.Collections.Concurrent;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Shop;
using Map.Server.Inventory;
using Map.Server.Session;
using Map.Server.Shop.Vending;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Shop;

/// <summary>
/// GP-VEND — open/close vending bridge: CZ_REQ_OPENSTORE2 → vending_openvending (offers validated
/// against the cart) → ZC_STORE_ENTRY stall sign + ZC_ACK_OPENSTORE2; CZ_REQ_CLOSESTORE → close →
/// ZC_DISAPPEAR_ENTRY.
/// </summary>
public class VendingOpenCloseTests
{
    // --- handler routing / offer validation (recording service) ---

    [Fact]
    public async Task OpenStore_validates_offers_against_cart_and_converts_index()
    {
        var (handler, session, vending) = BuildOpen(
            Item(serverIndex: 0, nameId: 501, amount: 10),
            Item(serverIndex: 1, nameId: 909, amount: 3));

        // client index = server index + 2; second offer asks more than stock → dropped.
        await handler.HandleAsync(session, Open("MyShop",
            (index: 2, amount: 5, price: 100),   // server slot 0, ok
            (index: 3, amount: 99, price: 50)));  // server slot 1, amount > stock → filtered

        Assert.Equal("MyShop", vending.LastTitle);
        var offer = Assert.Single(vending.LastOffers);
        Assert.Equal((short)0, offer.index); // converted to server index
        Assert.Equal((short)5, offer.qty);
        Assert.Equal(100, offer.price);
    }

    [Fact]
    public async Task OpenStore_with_empty_name_does_not_open()
    {
        var (handler, session, vending) = BuildOpen(Item(0, 501, 10));
        await handler.HandleAsync(session, Open("", (2, 5, 100)));
        Assert.False(vending.Updated);
    }

    [Fact]
    public async Task OpenStore_with_no_valid_offers_does_not_open()
    {
        var (handler, session, vending) = BuildOpen(Item(0, 501, 10));
        await handler.HandleAsync(session, Open("Shop", (99, 5, 100))); // slot not in cart
        Assert.False(vending.Updated);
    }

    [Fact]
    public async Task CloseStore_routes_to_close()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = Pc(registry);
        var vending = new RecordingVending();
        var handler = new CloseStoreHandler(registry, vending, NullLogger<CloseStoreHandler>.Instance);

        await handler.HandleAsync(Session(pc), new CZ_REQ_CLOSESTORE());

        Assert.True(vending.Closed);
    }

    // --- emit (real service + client) ---

    [Fact]
    public void Update_emits_stall_sign_to_area_and_ack_to_vendor()
    {
        var (svc, pc, session, vis) = BuildEmit();
        svc.Update(pc, "Potions4U", new (short, short, int)[] { (0, 5, 100) });

        var sign = vis.AreaPackets.OfType<Core.Server.Packets.Out.ZC.ZC_STORE_ENTRY>().Single();
        Assert.Equal((uint)pc.AccountId, sign.MakerAccountId);
        Assert.Equal("Potions4U", sign.StoreName);
        Assert.Contains(Outbound(session), x => Header(x) == (ushort)PacketHeader.ZC_ACK_OPENSTORE2 && x[2] == 0);
    }

    [Fact]
    public void CloseVending_emits_disappear_to_area()
    {
        var (svc, pc, _, vis) = BuildEmit();
        svc.Update(pc, "Shop", new (short, short, int)[] { (0, 5, 100) });
        vis.AreaPackets.Clear();

        svc.CloseVending(pc);

        var gone = vis.AreaPackets.OfType<Core.Server.Packets.Out.ZC.ZC_DISAPPEAR_ENTRY>().Single();
        Assert.Equal((uint)pc.Id.Value, gone.OwnerId);
    }

    // --- helpers ---

    private static (OpenStoreHandler handler, MapSessionData session, RecordingVending vending) BuildOpen(params InventoryItem[] cart)
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = Pc(registry);
        var vending = new RecordingVending();
        var handler = new OpenStoreHandler(registry, vending, NullLogger<OpenStoreHandler>.Instance);
        var session = Session(pc);
        session.Cart = cart.ToList();
        return (handler, session, vending);
    }

    private static (VendingService svc, PlayerEntity pc, MapSessionData session, RecordingVisibility vis) BuildEmit()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = Pc(registry);
        var session = Session(pc);
        var sessions = new FakeSessions(pc.Id, pc.AccountId, session);
        var vis = new RecordingVisibility();
        var client = new VendingClientService(vis, sessions, NullLogger<VendingClientService>.Instance);
        var svc = new VendingService(NullLogger<VendingService>.Instance, sessions, client);
        return (svc, pc, session, vis);
    }

    private static PlayerEntity Pc(EntityRegistry registry)
    {
        var pc = new PlayerEntity(1, 1001, "Vendor", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        return pc;
    }

    private static InventoryItem Item(int serverIndex, uint nameId, uint amount) =>
        new() { Id = serverIndex + 1, ServerIndex = serverIndex, NameId = nameId, Amount = amount, Identified = true };

    private static MapSessionData Session(PlayerEntity pc)
        => new(TestSocketFactory.CreateSocketPair().ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = pc.AccountId, CharacterId = pc.CharacterId, AuthState = MapAuthState.Spawned, EntityId = pc.Id };

    private static CZ_REQ_OPENSTORE2 Open(string name, params (int index, int amount, int price)[] offers)
    {
        var p = new CZ_REQ_OPENSTORE2();
        typeof(CZ_REQ_OPENSTORE2).GetProperty("StoreName")!.SetValue(p, name);
        var list = offers.Select(o => new VendOffer((short)o.index, (short)o.amount, o.price)).ToArray();
        typeof(CZ_REQ_OPENSTORE2).GetProperty("Offers")!.SetValue(p, (IReadOnlyList<VendOffer>)list);
        return p;
    }

    private static ushort Header(byte[] b) => (ushort)(b[0] | (b[1] << 8));
    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private sealed class RecordingVending : IVendingService
    {
        public bool Updated; public bool Closed;
        public string? LastTitle; public IReadOnlyList<(short index, short qty, int price)> LastOffers = Array.Empty<(short, short, int)>();
        public void Update(PlayerEntity vendor, string title, IReadOnlyList<(short index, short qty, int price)> items)
        { Updated = true; LastTitle = title; LastOffers = items; }
        public void CloseVending(PlayerEntity vendor) => Closed = true;
        public void Reopen(PlayerEntity vendor) { }
        public void VendingListReq(PlayerEntity buyer, int vendorAccountId) { }
        public bool PurchaseReq(PlayerEntity buyer, int vendorAccountId, long venderId, IReadOnlyList<(short index, short qty)> items) => false;
        public bool Search(PlayerEntity searcher, int nameId) => false;
        public bool SearchAll(PlayerEntity searcher, int nameId) => false;
        public void InitAutotrade() { }
    }

    private sealed class FakeSessions(EntityId id, int accountId, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
        public MapSessionData? GetByAccountId(int acc) => acc == accountId ? session : null;
    }

    private sealed class RecordingVisibility : IVisibilityService
    {
        public readonly List<Core.Server.Packets.OutgoingPacket> AreaPackets = new();
        public void SendToArea(Entity src, Core.Server.Packets.OutgoingPacket packet, SendTarget target = SendTarget.Area) => AreaPackets.Add(packet);
        public void SendToSelf(PlayerEntity player, Core.Server.Packets.OutgoingPacket packet) { }
        public void NotifySpawnedToArea(Entity entered) { }
        public void NotifyVanishedToArea(Entity gone, Core.Server.Packets.Out.ZC.VanishReason reason) { }
        public void NotifyMoveToArea(Entity walker, short fromX, short fromY, short toX, short toY, uint startTime) { }
        public void SendCurrentViewToSelf(PlayerEntity self) { }
        public void NotifyMoveDiff(Entity walker, short fromX, short fromY, short toX, short toY) { }
        public IReadOnlyList<Entity> NewlyVisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> NewlyInvisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) => _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
