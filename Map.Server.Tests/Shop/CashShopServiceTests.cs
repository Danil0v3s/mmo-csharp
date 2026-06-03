using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Shop.Cash;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DbItem = Core.Database.Entities.ItemEntity;

namespace Map.Server.Tests.Shop;

/// <summary>
/// FEATURE-13 — cash shop: real catalog-driven purchase (kafra/cash point split debit, item grant),
/// active-sale discounted pricing, and all-or-nothing rejection on insufficient points / inventory
/// full / unknown item / bad amount.
/// </summary>
public class CashShopServiceTests
{
    private const uint PotionId = 501;   // stackable
    private const uint SwordId = 1101;   // non-stackable (Weapon)
    private const int Acc = 10;
    private const int TabNew = 0;
    private const int TabSale = 8;

    private sealed record Ctx(CashShopService Svc, PlayerEntity Pc, MapSessionData Session);

    private static Ctx Build(int cash = 100_000, int kafra = 0, IServiceScopeFactory? scopes = null)
    {
        var sessions = new FakeSessions();
        var items = new FakeItems();
        var inv = new FakeInventory(items);

        var pc = new PlayerEntity(1, Acc, "Buyer", Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1, CashPoints = cash, KafraPoints = kafra };
        var session = NewSession(pc);
        session.Inventory = new List<InventoryItem>();
        sessions.Register(pc.Id, Acc, session);

        var svc = new CashShopService(NullLogger<CashShopService>.Instance, scopes, items, sessions, inv);
        return new Ctx(svc, pc, session);
    }

    [Fact]
    public void BuyList_debits_cash_points_and_grants_item()
    {
        var c = Build(cash: 100_000);
        c.Svc.SeedCatalogForTest(TabNew, (PotionId, 1000));

        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)PotionId, 3, (byte)TabNew) });

        Assert.Equal(CashShopResult.Success, r);
        Assert.Equal(100_000 - 3000, c.Pc.CashPoints);
        Assert.Equal(3u, c.Session.Inventory!.Single(i => i.NameId == PotionId).Amount);
    }

    [Fact]
    public void BuyList_spends_kafra_first_then_cash()
    {
        var c = Build(cash: 100_000, kafra: 2000);
        c.Svc.SeedCatalogForTest(TabNew, (PotionId, 1000));

        var r = c.Svc.BuyList(c.Pc, kafraPay: 5000, new[] { ((int)PotionId, 3, (byte)TabNew) }); // cost 3000

        Assert.Equal(CashShopResult.Success, r);
        Assert.Equal(0, c.Pc.KafraPoints);          // 2000 kafra (capped to balance) spent first
        Assert.Equal(100_000 - 1000, c.Pc.CashPoints); // remaining 1000 from cash
    }

    [Fact]
    public void Active_sale_item_charged_the_sale_tab_price()
    {
        var c = Build(cash: 100_000);
        c.Svc.SeedCatalogForTest(TabNew, (PotionId, 1000));
        c.Svc.SeedCatalogForTest(TabSale, (PotionId, 700)); // discounted price lives in the Sale tab
        c.Svc.SaleAddItem((int)PotionId, amount: 5, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(1));

        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)PotionId, 2, (byte)TabSale) });

        Assert.Equal(CashShopResult.Success, r);
        Assert.Equal(100_000 - 1400, c.Pc.CashPoints);  // 2 × 700, not 2 × 1000
        var sale = c.Svc.ActiveSales().Single();
        Assert.Equal(700, sale.price);
        Assert.Equal(3, sale.amount);                    // sale stock 5 → 3
    }

    [Fact]
    public void Sale_tab_without_active_sale_is_rejected()
    {
        var c = Build(cash: 100_000);
        c.Svc.SeedCatalogForTest(TabSale, (PotionId, 700)); // listed but no active sale window
        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)PotionId, 1, (byte)TabSale) });
        Assert.Equal(CashShopResult.PurchaseFail, r);
        Assert.Equal(100_000, c.Pc.CashPoints);
    }

    [Fact]
    public void Insufficient_points_rejects_with_no_mutation()
    {
        var c = Build(cash: 100);
        c.Svc.SeedCatalogForTest(TabNew, (PotionId, 1000));
        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)PotionId, 3, (byte)TabNew) });
        Assert.Equal(CashShopResult.Money, r);
        Assert.Equal(100, c.Pc.CashPoints);
        Assert.Empty(c.Session.Inventory!);
    }

    [Fact]
    public void Unknown_item_for_tab_is_rejected()
    {
        var c = Build(cash: 100_000);
        c.Svc.SeedCatalogForTest(TabNew, (PotionId, 1000));
        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { (9999, 1, (byte)TabNew) });
        Assert.Equal(CashShopResult.PurchaseFail, r);
        Assert.Equal(100_000, c.Pc.CashPoints);
        Assert.Empty(c.Session.Inventory!);
    }

    [Fact]
    public void Over_quantity_is_rejected()
    {
        var c = Build(cash: 100_000_0);
        c.Svc.SeedCatalogForTest(TabNew, (PotionId, 1));
        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)PotionId, 100, (byte)TabNew) }); // >99
        Assert.Equal(CashShopResult.Amount, r);
    }

    [Fact]
    public void Inventory_full_is_rejected_for_non_stackable()
    {
        var c = Build(cash: 100_000);
        c.Svc.SeedCatalogForTest(TabNew, (SwordId, 100));
        // Fill 99 of 100 slots; buying 3 swords needs 3 fresh slots → over.
        for (var i = 0; i < 99; i++)
            c.Session.Inventory!.Add(new InventoryItem { ServerIndex = i, NameId = 700 + (uint)i, Amount = 1, Identified = true });

        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)SwordId, 3, (byte)TabNew) });
        Assert.Equal(CashShopResult.InventoryWeight, r);
        Assert.Equal(100_000, c.Pc.CashPoints);
    }

    [Fact]
    public void Non_stackable_buy_creates_one_slot_per_instance()
    {
        var c = Build(cash: 100_000);
        c.Svc.SeedCatalogForTest(TabNew, (SwordId, 100));
        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)SwordId, 3, (byte)TabNew) });
        Assert.Equal(CashShopResult.Success, r);
        Assert.Equal(3, c.Session.Inventory!.Count(i => i.NameId == SwordId)); // 3 separate slots
    }

    [Fact]
    public void Catalog_loads_from_stubbed_repository()
    {
        var repo = new FakeCashRepo();
        repo.Add("New", "Red_Potion", PotionId, 1000);
        var provider = new ServiceCollection()
            .AddScoped<IItemCashDbRepository>(_ => repo)
            .BuildServiceProvider();

        var c = Build(cash: 100_000, scopes: provider.GetRequiredService<IServiceScopeFactory>());
        // Catalog hydrated in the ctor (Reload) — buy the catalogued potion.
        var r = c.Svc.BuyList(c.Pc, kafraPay: 0, new[] { ((int)PotionId, 1, (byte)TabNew) });
        Assert.Equal(CashShopResult.Success, r);
        Assert.Equal(100_000 - 1000, c.Pc.CashPoints);
    }

    // --- helpers / fakes ---

    private static MapSessionData NewSession(PlayerEntity pc)
    {
        var sockets = TestSocketFactory.CreateSocketPair();
        return new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = pc.AccountId, CharacterId = pc.CharacterId, EntityId = pc.Id };
    }

    private sealed class FakeSessions : ISessionManagerAccessor
    {
        private readonly Dictionary<int, MapSessionData> _byEntity = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public void Register(EntityId id, int acc, MapSessionData s) { _byEntity[id.Value] = s; _byAcc[acc] = s; }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEntity.GetValueOrDefault(entityId.Value);
        public MapSessionData? GetByAccountId(int accountId) => _byAcc.GetValueOrDefault(accountId);
    }

    private sealed class FakeItems : IItemCatalog
    {
        private readonly Dictionary<uint, DbItem> _byId = new()
        {
            [PotionId] = new DbItem { Id = PotionId, NameAegis = "Red_Potion", NameEnglish = "Red Potion", Type = "Usable", Weight = 70 },
            [SwordId] = new DbItem { Id = SwordId, NameAegis = "Sword", NameEnglish = "Sword", Type = "Weapon", Weight = 500 },
        };
        public int Count => _byId.Count;
        public DbItem? Get(uint itemId) => _byId.GetValueOrDefault(itemId);
        public DbItem? GetByAegisName(string aegisName)
            => _byId.Values.FirstOrDefault(i => string.Equals(i.NameAegis, aegisName, StringComparison.OrdinalIgnoreCase));
        public IEnumerable<DbItem> All() => _byId.Values;
        public void Reload() { }
    }

    private sealed class FakeInventory : IInventoryService
    {
        private readonly IItemCatalog _items;
        public FakeInventory(IItemCatalog items) => _items = items;
        public Task LoadAsync(MapSessionData session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void SendInventoryList(MapSessionData session) { }
        public bool GiveItem(MapSessionData session, uint nameId, int amount)
        {
            if (amount <= 0 || session.Inventory == null) return false;
            var def = _items.Get(nameId);
            var stackable = def != null && def.Type is "Usable" or "Healing" or "Etc";
            if (stackable)
            {
                var slot = session.Inventory.FirstOrDefault(i => i.NameId == nameId);
                if (slot != null) { slot.Amount += (uint)amount; return true; }
            }
            var next = session.Inventory.Count == 0 ? 0 : session.Inventory.Max(i => i.ServerIndex) + 1;
            session.Inventory.Add(new InventoryItem { ServerIndex = next, NameId = nameId, Amount = (uint)amount, Identified = true });
            return true;
        }
    }

    private sealed class FakeCashRepo : IItemCashDbRepository
    {
        private readonly List<ItemCashDbEntity> _tabs = new();
        private readonly List<ItemCashEntryDbEntity> _entries = new();
        public void Add(string tab, string aegis, uint id, int price)
        {
            if (_tabs.All(t => t.Tab != tab)) _tabs.Add(new ItemCashDbEntity { Tab = tab });
            _entries.Add(new ItemCashEntryDbEntity { Tab = tab, ItemAegis = aegis, Price = price });
        }
        public Task<IReadOnlyList<ItemCashDbEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemCashDbEntity>>(_tabs);
        public Task<IReadOnlyList<ItemCashEntryDbEntity>> GetEntriesAsync(string tab, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemCashEntryDbEntity>>(_entries.Where(e => e.Tab == tab).ToList());
    }
}
