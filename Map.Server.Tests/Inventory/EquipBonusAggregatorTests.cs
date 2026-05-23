using Core.Database.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Status;

namespace Map.Server.Tests.Inventory;

/// <summary>
/// Tests for <see cref="EquipBonusAggregator.Aggregate"/> — the static
/// stat-summary path (weapon ATK / DEF / range / element). The
/// <see cref="EquipBonusAggregator.BuildBundle"/> behavior moved to
/// hook dispatch in CONV-5; coverage for that path lives in
/// <see cref="ItemHookDispatcherTests"/> (per-item onEquip) and
/// <see cref="ComboDispatcherTests"/> (combo onActive).
/// </summary>
public class EquipBonusAggregatorTests
{
    [Fact]
    public void NoItems_ReturnsEmptySummary()
    {
        var summary = EquipBonusAggregator.Aggregate(null, new StubCatalog());
        Assert.Equal(0, summary.WeaponAtkMin);
        Assert.Equal(0, summary.EquipDef);
        Assert.Equal(1, summary.AttackRange);
        Assert.Equal(BattleElement.Neutral, summary.WeaponElement);
    }

    [Fact]
    public void EquippedKnifeAndShirt_SumsAtkAndDef()
    {
        var catalog = new StubCatalog
        {
            [1201] = new ItemEntity { Id = 1201, NameAegis = "Knife", Attack = 17, Range = 1 },
            [2301] = new ItemEntity { Id = 2301, NameAegis = "Cotton_Shirt", Defense = 10 },
        };
        var inv = new[]
        {
            new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand },
            new InventoryItem { NameId = 2301, Equip = EquipBonusAggregator.EquipArmor },
        };

        var summary = EquipBonusAggregator.Aggregate(inv, catalog);

        Assert.Equal(17, summary.WeaponAtkMin);
        Assert.Equal(17, summary.WeaponAtkMax);
        Assert.Equal(10, summary.EquipDef);
        Assert.Equal(1, summary.AttackRange);
    }

    [Fact]
    public void UnequippedItems_Ignored()
    {
        var catalog = new StubCatalog
        {
            [1201] = new ItemEntity { Id = 1201, NameAegis = "Knife", Attack = 17, Range = 1 },
        };
        // Equip flag = 0 → in bag but not worn.
        var inv = new[] { new InventoryItem { NameId = 1201, Equip = 0 } };
        var summary = EquipBonusAggregator.Aggregate(inv, catalog);
        Assert.Equal(0, summary.WeaponAtkMin);
    }

    [Fact]
    public void LongRangeWeapon_SetsRange()
    {
        var catalog = new StubCatalog
        {
            [1718] = new ItemEntity { Id = 1718, NameAegis = "Bow", Attack = 40, Range = 9 },
        };
        var inv = new[] { new InventoryItem { NameId = 1718, Equip = EquipBonusAggregator.EquipRightHand } };
        var summary = EquipBonusAggregator.Aggregate(inv, catalog);
        Assert.Equal(40, summary.WeaponAtkMin);
        Assert.Equal(9, summary.AttackRange);
    }

    [Fact]
    public void BuildBundle_WithoutHookDispatcher_LeavesBundleEmpty()
    {
        // Per-item bonus accumulation moved to hook dispatch in CONV-5.
        // Without an IItemHookDispatcher the aggregator only walks the
        // equipped set + resets the bundle; the bundle stays empty.
        // Coverage for the hook-driven accumulation: ItemHookDispatcherTests.
        var catalog = new StubCatalog
        {
            [1201] = new ItemEntity { Id = 1201, NameAegis = "Knife", Script = "bonus bAtk,10;" },
        };
        var inv = new[]
        {
            new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand },
        };
        var bundle = new EquipBonusBundle();

        EquipBonusAggregator.BuildBundle(inv, catalog, bundle);

        Assert.Equal(0, bundle.FlatAtk);
    }

    private sealed class StubCatalog : Dictionary<uint, ItemEntity>, IItemCatalog
    {
        public int Count => base.Count;
        public ItemEntity? Get(uint id) => TryGetValue(id, out var r) ? r : null;
        public ItemEntity? GetByAegisName(string n) => null;
        public new IEnumerable<ItemEntity> All() => Values;
        public void Reload() { }
    }
}
