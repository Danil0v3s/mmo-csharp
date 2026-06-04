using System.Collections.Concurrent;
using Core.Database.Entities;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Handlers.Shop;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Shop.Buying;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Shop;

/// <summary>
/// GP-BUYSTORE — open/close bridge: CZ_REQ_OPEN_BUYING_STORE → buyingstore_create (escrow) →
/// ZC_MYITEMLIST_BUYING_STORE + ZC_BUYING_STORE_ENTRY sign; CZ_REQ_CLOSE_BUYING_STORE → refund +
/// ZC_DISAPPEAR_BUYING_STORE_ENTRY.
/// </summary>
public class BuyingStoreOpenCloseTests
{
    private const int Potion = 501;

    [Fact]
    public void Open_escrows_zeny_and_emits_owner_list_and_sign()
    {
        var (svc, pc, session, vis) = Build(buyerZeny: 60000);

        var ok = svc.Update(pc, "BuyPotions", zenyLimit: 50000,
            new (int, short, int)[] { (Potion, 10, 1000) });

        Assert.True(ok);
        Assert.Equal(10000u, session.CharacterData!.Zeny);        // 60000 - 50000 escrowed
        var my = Outbound(session).Single(x => Header(x) == (ushort)PacketHeader.ZC_MYITEMLIST_BUYING_STORE);
        Assert.Equal((uint)pc.AccountId, BitConverter.ToUInt32(my, 4));  // AID
        Assert.Equal(50000, BitConverter.ToInt32(my, 8));               // zeny limit
        // offer at offset 12: price.L amount.W type.B nameId.W
        Assert.Equal(1000, BitConverter.ToInt32(my, 12));
        Assert.Equal(10, BitConverter.ToInt16(my, 16));
        var sign = vis.AreaPackets.OfType<ZC_BUYING_STORE_ENTRY>().Single();
        Assert.Equal("BuyPotions", sign.StoreName);
        Assert.Contains(Outbound(session), x => Header(x) == (ushort)PacketHeader.ZC_PAR_CHANGE); // zeny update
    }

    [Fact]
    public void Open_without_enough_zeny_fails_and_does_not_escrow()
    {
        var (svc, pc, session, _) = Build(buyerZeny: 100);
        var ok = svc.Update(pc, "Shop", zenyLimit: 50000, new (int, short, int)[] { (Potion, 1, 1000) });

        Assert.False(ok);
        Assert.Equal(100u, session.CharacterData!.Zeny); // untouched
        Assert.Contains(Outbound(session), x => Header(x) == (ushort)PacketHeader.ZC_FAILED_OPEN_BUYING_STORE);
    }

    [Fact]
    public void Close_refunds_unspent_escrow_and_removes_sign()
    {
        var (svc, pc, session, vis) = Build(buyerZeny: 60000);
        svc.Update(pc, "Shop", zenyLimit: 50000, new (int, short, int)[] { (Potion, 10, 1000) });
        vis.AreaPackets.Clear();

        svc.Close(pc);

        Assert.Equal(60000u, session.CharacterData!.Zeny); // full refund (no trades)
        Assert.Single(vis.AreaPackets.OfType<ZC_DISAPPEAR_BUYING_STORE_ENTRY>());
    }

    [Fact]
    public async Task OpenBuyingStoreHandler_routes_offers_to_service()
    {
        var (svc, pc, session, _) = Build(buyerZeny: 60000);
        var registry = RegistryWith(pc);
        var handler = new OpenBuyingStoreHandler(registry, svc, NullLogger<OpenBuyingStoreHandler>.Instance);

        var p = new CZ_REQ_OPEN_BUYING_STORE();
        typeof(CZ_REQ_OPEN_BUYING_STORE).GetProperty("StoreName")!.SetValue(p, "Shop");
        typeof(CZ_REQ_OPEN_BUYING_STORE).GetProperty("ZenyLimit")!.SetValue(p, 50000);
        typeof(CZ_REQ_OPEN_BUYING_STORE).GetProperty("Offers")!.SetValue(p,
            (IReadOnlyList<BuyOffer>)new[] { new BuyOffer((short)Potion, 10, 1000) });
        await handler.HandleAsync(session, p);

        Assert.Equal(10000u, session.CharacterData!.Zeny); // escrowed via the service
    }

    // --- helpers ---

    private static (BuyingStoreService svc, PlayerEntity pc, MapSessionData session, RecordingVisibility vis) Build(int buyerZeny)
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1001, "Buyer", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var session = new MapSessionData(TestSocketFactory.CreateSocketPair().ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = pc.AccountId, CharacterId = pc.CharacterId, AuthState = MapAuthState.Spawned, EntityId = pc.Id,
          CharacterData = new Core.Server.IPC.CharacterDataResponse { Zeny = (uint)buyerZeny } };
        var sessions = new FakeSessions(pc.Id, pc.AccountId, session);
        var vis = new RecordingVisibility();
        var client = new BuyingStoreClientService(vis, sessions, NullLogger<BuyingStoreClientService>.Instance);
        var svc = new BuyingStoreService(NullLogger<BuyingStoreService>.Instance, sessions, client, new FakeItems());
        return (svc, pc, session, vis);
    }

    private static EntityRegistry RegistryWith(PlayerEntity pc)
    {
        var reg = new EntityRegistry(new StubWorld(new MapData("test_map", 200, 200, new byte[200 * 200])));
        reg.Add(pc);
        return reg;
    }

    private static ushort Header(byte[] b) => (ushort)(b[0] | (b[1] << 8));
    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private sealed class FakeSessions(EntityId id, int acc, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
        public MapSessionData? GetByAccountId(int a) => a == acc ? session : null;
    }

    private sealed class FakeItems : IItemCatalog
    {
        public int Count => 1;
        public ItemEntity? Get(uint itemId) => new() { Id = itemId, Type = "Healing" };
        public ItemEntity? GetByAegisName(string aegisName) => null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class RecordingVisibility : IVisibilityService
    {
        public readonly List<Core.Server.Packets.OutgoingPacket> AreaPackets = new();
        public void SendToArea(Entity src, Core.Server.Packets.OutgoingPacket packet, SendTarget target = SendTarget.Area) => AreaPackets.Add(packet);
        public void SendToSelf(PlayerEntity player, Core.Server.Packets.OutgoingPacket packet) { }
        public void NotifySpawnedToArea(Entity entered) { }
        public void NotifyVanishedToArea(Entity gone, VanishReason reason) { }
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
