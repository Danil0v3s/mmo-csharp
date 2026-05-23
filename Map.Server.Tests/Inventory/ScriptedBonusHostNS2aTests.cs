using Core.Database.Entities;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Items;

namespace Map.Server.Tests.Inventory;

/// <summary>
/// NS-2a acceptance: the six methods wired on
/// <see cref="ScriptedBonusHost"/> to drain the Proxy fallback —
/// <c>getenchantgrade</c>, <c>getequipweaponlv</c>,
/// <c>getequiparmorlv</c>, <c>getitempos</c>, <c>vip_status</c>,
/// <c>gettime</c>. Each test exercises one method against a hand-built
/// equipped set + a stub <see cref="IItemCatalog"/>.
///
/// Per NS-1b, <c>getenchantgrade</c> alone was 1,239 of 1,390 (89%)
/// of all unknown-method Proxy fallbacks. Wiring it returns real
/// grade values to combo scripts gated on enchant tier.
/// </summary>
public class ScriptedBonusHostNS2aTests
{
    private sealed class StubCatalog : IItemCatalog
    {
        private readonly Dictionary<uint, ItemEntity> _byId = new();
        public void Add(ItemEntity e) => _byId[e.Id] = e;
        public int Count => _byId.Count;
        public ItemEntity? Get(uint id) => _byId.GetValueOrDefault(id);
        public ItemEntity? GetByAegisName(string aegis) => null;
        public IEnumerable<ItemEntity> All() => _byId.Values;
        public void Reload() { }
    }

    private static PlayerEntity MakePc() =>
        new(characterId: 1, accountId: 1, name: "Tester",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0);

    [Fact]
    public void getenchantgrade_returns_grade_of_item_in_slot()
    {
        var pc = MakePc();
        var equipped = new[]
        {
            new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand, EnchantGrade = 3 },
            new InventoryItem { NameId = 2301, Equip = EquipBonusAggregator.EquipArmor,     EnchantGrade = 1 },
        };
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(), equipped);
        Assert.Equal(3, host.getenchantgrade("EQI_HAND_R"));
        Assert.Equal(1, host.getenchantgrade("EQI_ARMOR"));
        // Default slot (no arg) = EQI_HAND_R, matches rAthena.
        Assert.Equal(3, host.getenchantgrade());
    }

    [Fact]
    public void getenchantgrade_returns_zero_for_empty_slot()
    {
        var pc = MakePc();
        var equipped = new[]
        {
            new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand, EnchantGrade = 4 },
        };
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(), equipped);
        Assert.Equal(0, host.getenchantgrade("EQI_SHOES"));
    }

    [Fact]
    public void getequipweaponlv_reads_WeaponLevel_from_catalog()
    {
        var pc = MakePc();
        var catalog = new StubCatalog();
        catalog.Add(new ItemEntity { Id = 1601, NameAegis = "Rod_C", WeaponLevel = 3 });
        var equipped = new[]
        {
            new InventoryItem { NameId = 1601, Equip = EquipBonusAggregator.EquipRightHand, Refine = 7 },
        };
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(), equipped,
            catalog: catalog);
        Assert.Equal(3, host.getequipweaponlv("EQI_HAND_R"));
        Assert.Equal(3, host.getequipweaponlv()); // default slot
    }

    [Fact]
    public void getequipweaponlv_returns_zero_without_catalog_or_item()
    {
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: Array.Empty<InventoryItem>());
        Assert.Equal(0, host.getequipweaponlv("EQI_HAND_R"));
    }

    [Fact]
    public void getequiparmorlv_reads_ArmorLevel_from_catalog()
    {
        var pc = MakePc();
        var catalog = new StubCatalog();
        catalog.Add(new ItemEntity { Id = 15001, NameAegis = "Armor_C", ArmorLevel = 2 });
        var equipped = new[]
        {
            new InventoryItem { NameId = 15001, Equip = EquipBonusAggregator.EquipArmor },
        };
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(), equipped,
            catalog: catalog);
        Assert.Equal(2, host.getequiparmorlv("EQI_ARMOR"));
        Assert.Equal(2, host.getequiparmorlv()); // default slot
    }

    [Fact]
    public void getitempos_returns_equip_bits_for_equipped_item()
    {
        var pc = MakePc();
        var equipped = new[]
        {
            new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand },
            new InventoryItem { NameId = 2301, Equip = EquipBonusAggregator.EquipArmor },
        };
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(), equipped);
        Assert.Equal((int)EquipBonusAggregator.EquipRightHand, host.getitempos(1201));
        Assert.Equal((int)EquipBonusAggregator.EquipArmor, host.getitempos(2301));
        // Not equipped → 0.
        Assert.Equal(0, host.getitempos(99999));
    }

    [Fact]
    public void getitempos_returns_zero_when_no_args()
    {
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: Array.Empty<InventoryItem>());
        Assert.Equal(0, host.getitempos());
    }

    [Fact]
    public void vip_status_returns_zero_placeholder()
    {
        // Placeholder wire — no live VIP state on PlayerEntity today.
        // Documents the silent-Proxy → documented-stub transition.
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        Assert.Equal(0, host.vip_status(1)); // is VIP?
        Assert.Equal(0, host.vip_status(2)); // expiration unix
        Assert.Equal(0, host.vip_status(3)); // remaining seconds
    }

    [Fact]
    public void gettime_dispatches_DT_constants_to_UtcNow_fields()
    {
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        var now = DateTime.UtcNow;
        // Reads can race the clock at second granularity — use tolerant
        // checks on second/minute, exact on day/month/year.
        Assert.InRange(host.gettime(1), 0, 59);          // DT_SECOND
        Assert.InRange(host.gettime(2), 0, 59);          // DT_MINUTE
        Assert.InRange(host.gettime(3), 0, 23);          // DT_HOUR
        Assert.InRange(host.gettime(4), 0, 6);           // DT_DAYOFWEEK
        Assert.Equal(now.Day, host.gettime(5));          // DT_DAYOFMONTH
        Assert.Equal(now.Month, host.gettime(6));        // DT_MONTH
        Assert.Equal(now.Year, host.gettime(7));         // DT_YEAR
        Assert.InRange(host.gettime(8), 1, 366);         // DT_DAYOFYEAR
        Assert.Equal(0, host.gettime(99));               // unknown type
        Assert.Equal(0, host.gettime());                 // no args
    }

    [Fact]
    public void getequipid_returns_NameId_of_equipped_item()
    {
        // Re-tested here because the refactor moved the body through
        // FindEquippedInSlot — confirm parity with the pre-NS-2a stub.
        var pc = MakePc();
        var equipped = new[]
        {
            new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand },
        };
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(), equipped);
        Assert.Equal(1201, host.getequipid("EQI_HAND_R"));
        Assert.Equal(0, host.getequipid("EQI_SHOES"));
        Assert.Equal(0, host.getequipid()); // no args, was 0 before NS-2a too
    }
}
