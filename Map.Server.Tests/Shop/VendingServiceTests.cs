using Core.Server.IPC;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Shop.Vending;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Shop;

/// <summary>
/// FEATURE-11 — vending real transfer (zeny + item from the vendor cart to the buyer), gates, tax,
/// anti-desync, and sold-out auto-close.
/// </summary>
public class VendingServiceTests
{
    private const uint PotionId = 501;
    private const int VendorAcc = 10;
    private const int Slot = 5;

    private sealed record Ctx(VendingService Svc, PlayerEntity Vendor, MapSessionData VendorS,
        PlayerEntity Buyer, MapSessionData BuyerS, FakeSessions Sessions);

    private static Ctx Build(uint buyerZeny = 100_000, uint cartAmount = 10)
    {
        var sessions = new FakeSessions();
        var vendor = NewPc(charId: 1, acc: VendorAcc, "Vendor");
        var vendorS = NewSession(vendor, acc: VendorAcc);
        vendorS.Cart = new List<InventoryItem> { new() { Id = 1, ServerIndex = Slot, NameId = PotionId, Amount = cartAmount, Identified = true } };
        vendorS.CharacterData = new CharacterDataResponse { Zeny = 0 };
        sessions.Register(vendor.Id, VendorAcc, vendorS);

        var buyer = NewPc(charId: 2, acc: 20, "Buyer");
        var buyerS = NewSession(buyer, acc: 20);
        buyerS.Inventory = new List<InventoryItem>();
        buyerS.CharacterData = new CharacterDataResponse { Zeny = buyerZeny };
        sessions.Register(buyer.Id, 20, buyerS);

        var svc = new VendingService(NullLogger<VendingService>.Instance, sessions);
        return new Ctx(svc, vendor, vendorS, buyer, buyerS, sessions);
    }

    private static void Open(Ctx c, int qty = 10, int price = 1000)
        => c.Svc.Update(c.Vendor, "shop", new[] { ((short)Slot, (short)qty, price) });

    private static long Vid(Ctx c) => c.Svc.VenderIdOf(c.Vendor.Id)!.Value;

    [Fact]
    public void Purchase_transfers_zeny_and_item()
    {
        var c = Build(buyerZeny: 100_000, cartAmount: 10);
        Open(c, qty: 10, price: 1000);

        Assert.True(c.Svc.PurchaseReq(c.Buyer, VendorAcc, Vid(c), new[] { ((short)Slot, (short)3) }));

        Assert.Equal(100_000u - 3000u, c.BuyerS.CharacterData!.Zeny);     // buyer paid 3*1000
        Assert.Equal(3000u, c.VendorS.CharacterData!.Zeny);              // vendor received (tax 0)
        Assert.Equal(3u, c.BuyerS.Inventory!.Single(i => i.NameId == PotionId).Amount); // item delivered
        Assert.Equal(7u, c.VendorS.Cart!.Single().Amount);              // cart debited
    }

    [Fact]
    public void Tax_reduces_vendor_gain()
    {
        var c = Build();
        c.Svc.VendingTaxBp = 500; // 5%
        Open(c, qty: 10, price: 1000);

        Assert.True(c.Svc.PurchaseReq(c.Buyer, VendorAcc, Vid(c), new[] { ((short)Slot, (short)2) }));
        Assert.Equal(100_000u - 2000u, c.BuyerS.CharacterData!.Zeny); // buyer pays full 2000
        Assert.Equal(2000u - 100u, c.VendorS.CharacterData!.Zeny);    // vendor gets 2000 - 5%
    }

    [Fact]
    public void Insufficient_buyer_zeny_rejects_without_mutation()
    {
        var c = Build(buyerZeny: 100);
        Open(c, qty: 10, price: 1000);

        Assert.False(c.Svc.PurchaseReq(c.Buyer, VendorAcc, Vid(c), new[] { ((short)Slot, (short)3) }));
        Assert.Equal(100u, c.BuyerS.CharacterData!.Zeny);
        Assert.Equal(10u, c.VendorS.Cart!.Single().Amount); // untouched
        Assert.Empty(c.BuyerS.Inventory!);
    }

    [Fact]
    public void Stale_vender_id_rejects()
    {
        var c = Build();
        Open(c);
        Assert.False(c.Svc.PurchaseReq(c.Buyer, VendorAcc, venderId: 9999, new[] { ((short)Slot, (short)1) }));
        Assert.Equal(10u, c.VendorS.Cart!.Single().Amount);
    }

    [Fact]
    public void Buying_more_than_stock_rejects()
    {
        var c = Build(cartAmount: 2);
        Open(c, qty: 2);
        Assert.False(c.Svc.PurchaseReq(c.Buyer, VendorAcc, Vid(c), new[] { ((short)Slot, (short)5) }));
        Assert.Equal(2u, c.VendorS.Cart!.Single().Amount);
    }

    [Fact]
    public void Selling_out_auto_closes_the_stall()
    {
        var c = Build(cartAmount: 5);
        Open(c, qty: 5, price: 1000);

        Assert.True(c.Svc.PurchaseReq(c.Buyer, VendorAcc, Vid(c), new[] { ((short)Slot, (short)5) }));
        Assert.Null(c.Svc.VenderIdOf(c.Vendor.Id)); // stall closed
        Assert.Empty(c.VendorS.Cart!);              // cart slot emptied
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
