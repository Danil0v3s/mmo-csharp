using Core.Server.IPC;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Shop.Buying;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Shop;

/// <summary>
/// FEATURE-12 — buying store: escrow on open, real seller→buyer transfer (zeny from escrow), refund
/// on close, gates + auto-close.
/// </summary>
public class BuyingStoreServiceTests
{
    private const uint PotionId = 501;
    private const int BuyerAcc = 10;
    private const int SellerSlot = 3;

    private sealed record Ctx(BuyingStoreService Svc, PlayerEntity Buyer, MapSessionData BuyerS,
        PlayerEntity Seller, MapSessionData SellerS);

    private static Ctx Build(uint buyerZeny = 100_000, uint sellerStock = 10)
    {
        var sessions = new FakeSessions();
        var buyer = NewPc(1, BuyerAcc, "Buyer");
        var buyerS = NewSession(buyer, BuyerAcc);
        buyerS.Inventory = new List<InventoryItem>();
        buyerS.CharacterData = new CharacterDataResponse { Zeny = buyerZeny };
        sessions.Register(buyer.Id, BuyerAcc, buyerS);

        var seller = NewPc(2, 20, "Seller");
        var sellerS = NewSession(seller, 20);
        sellerS.Inventory = new List<InventoryItem> { new() { Id = 1, ServerIndex = SellerSlot, NameId = PotionId, Amount = sellerStock, Identified = true } };
        sellerS.CharacterData = new CharacterDataResponse { Zeny = 0 };
        sessions.Register(seller.Id, 20, sellerS);

        var svc = new BuyingStoreService(NullLogger<BuyingStoreService>.Instance, sessions);
        return new Ctx(svc, buyer, buyerS, seller, sellerS);
    }

    private static bool OpenStore(Ctx c, long zenyLimit = 50_000, short amount = 10, int price = 1000)
    {
        c.Svc.Open(c.Buyer, 0);
        return c.Svc.Update(c.Buyer, "buy potions", zenyLimit,
            new[] { ((int)PotionId, amount, price) });
    }

    private static uint Sid(Ctx c) => c.Svc.StoreIdOf(c.Buyer.Id)!.Value;

    [Fact]
    public void Open_escrows_the_buyer_zeny()
    {
        var c = Build(buyerZeny: 100_000);
        Assert.True(OpenStore(c, zenyLimit: 50_000));
        Assert.Equal(50_000u, c.BuyerS.CharacterData!.Zeny); // 100k - 50k escrowed
    }

    [Fact]
    public void Cant_afford_limit_rejects_the_open()
    {
        var c = Build(buyerZeny: 100);
        Assert.False(OpenStore(c, zenyLimit: 50_000));
        Assert.Equal(100u, c.BuyerS.CharacterData!.Zeny); // not debited
        Assert.Null(c.Svc.StoreIdOf(c.Buyer.Id));         // stall torn down
    }

    [Fact]
    public void Close_refunds_the_unspent_escrow()
    {
        var c = Build(buyerZeny: 100_000);
        OpenStore(c, zenyLimit: 50_000);
        c.Svc.Close(c.Buyer);
        Assert.Equal(100_000u, c.BuyerS.CharacterData!.Zeny); // full refund
    }

    [Fact]
    public void Trade_transfers_item_to_buyer_and_zeny_to_seller_from_escrow()
    {
        var c = Build(buyerZeny: 100_000, sellerStock: 10);
        OpenStore(c, zenyLimit: 50_000, amount: 10, price: 1000);

        Assert.True(c.Svc.Trade(c.Seller, BuyerAcc, Sid(c), new[] { ((short)SellerSlot, (short)3) }));

        Assert.Equal(7u, c.SellerS.Inventory!.Single().Amount);                 // seller gave 3
        Assert.Equal(3u, c.BuyerS.Inventory!.Single(i => i.NameId == PotionId).Amount); // buyer got 3
        Assert.Equal(3000u, c.SellerS.CharacterData!.Zeny);                     // seller paid 3*1000 from escrow
        Assert.Equal(50_000u, c.BuyerS.CharacterData!.Zeny);                    // buyer zeny unchanged (already escrowed)

        // The held escrow dropped by 3000 → closing refunds 47000.
        c.Svc.Close(c.Buyer);
        Assert.Equal(50_000u + 47_000u, c.BuyerS.CharacterData!.Zeny);
    }

    [Fact]
    public void Stale_store_id_rejects()
    {
        var c = Build();
        OpenStore(c);
        Assert.False(c.Svc.Trade(c.Seller, BuyerAcc, storeId: 9999, new[] { ((short)SellerSlot, (short)1) }));
        Assert.Equal(10u, c.SellerS.Inventory!.Single().Amount);
    }

    [Fact]
    public void Seller_selling_more_than_held_rejects()
    {
        var c = Build(sellerStock: 100);
        OpenStore(c, zenyLimit: 2000, amount: 100, price: 1000); // escrow only 2000 → covers 2 items
        Assert.False(c.Svc.Trade(c.Seller, BuyerAcc, Sid(c), new[] { ((short)SellerSlot, (short)5) }));
        Assert.Equal(100u, c.SellerS.Inventory!.Single().Amount); // untouched
    }

    [Fact]
    public void Exhausting_the_escrow_auto_closes_and_refunds_zero()
    {
        var c = Build(buyerZeny: 100_000, sellerStock: 10);
        OpenStore(c, zenyLimit: 3000, amount: 10, price: 1000); // escrow 3000 = exactly 3 items

        Assert.True(c.Svc.Trade(c.Seller, BuyerAcc, Sid(c), new[] { ((short)SellerSlot, (short)3) }));
        Assert.Null(c.Svc.StoreIdOf(c.Buyer.Id));               // auto-closed (escrow spent)
        Assert.Equal(3000u, c.SellerS.CharacterData!.Zeny);
        Assert.Equal(100_000u - 3000u, c.BuyerS.CharacterData!.Zeny); // 3000 escrowed + spent, nothing to refund
    }

    // --- helpers / fakes ---

    private static PlayerEntity NewPc(int charId, int acc, string name)
        => new(charId, acc, name, Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1 };

    private static MapSessionData NewSession(PlayerEntity pc, int acc)
    {
        var sockets = TestSocketFactory.CreateSocketPair();
        return new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = acc, CharacterId = pc.CharacterId, EntityId = pc.Id };
    }

    private sealed class FakeSessions : ISessionManagerAccessor
    {
        private readonly Dictionary<int, MapSessionData> _byEntity = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public void Register(EntityId id, int acc, MapSessionData s) { _byEntity[id.Value] = s; _byAcc[acc] = s; }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEntity.GetValueOrDefault(entityId.Value);
        public MapSessionData? GetByAccountId(int accountId) => _byAcc.GetValueOrDefault(accountId);
    }
}
