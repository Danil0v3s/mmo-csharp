using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Entities;
using Map.Server.Combat;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-16 — renewal weapon size-fix table + weapon-type resolution + bow
/// arrow_atk. Renewal <c>size_fix.yml</c>: only Knuckle/Whip take 75% vs Large;
/// all else 100%. The item_db SubType is a NAME, so weapon-type resolution must
/// go through <see cref="WeaponTypeCodes"/> (the old int-parse left it at 0).
/// </summary>
public class Combat16WeaponSizeBowTests
{
    // ---- size_fix: Knuckle/Whip ×Large = 75, everything else 100 ----

    [Theory]
    [InlineData(WeaponTypeCodes.Knuckle, BattleSize.Large, 75)]
    [InlineData(WeaponTypeCodes.Whip, BattleSize.Large, 75)]
    [InlineData(WeaponTypeCodes.Knuckle, BattleSize.Small, 100)]
    [InlineData(WeaponTypeCodes.Knuckle, BattleSize.Medium, 100)]
    [InlineData(WeaponTypeCodes.OneHandSword, BattleSize.Large, 100)]
    [InlineData(WeaponTypeCodes.Bow, BattleSize.Large, 100)]
    [InlineData(WeaponTypeCodes.Fist, BattleSize.Large, 100)]
    public void SizeMod_matches_renewal_size_fix(int weaponType, BattleSize size, int expected)
        => Assert.Equal(expected, BattleCalculator.SizeMod(weaponType, size));

    // ---- weapon-type resolution from the item_db SubType NAME ----

    [Theory]
    [InlineData("Knuckle", WeaponTypeCodes.Knuckle)]
    [InlineData("Whip", WeaponTypeCodes.Whip)]
    [InlineData("Bow", WeaponTypeCodes.Bow)]
    [InlineData("1hSword", WeaponTypeCodes.OneHandSword)]
    [InlineData("2hStaff", WeaponTypeCodes.TwoHandStaff)]
    [InlineData("Huuma", WeaponTypeCodes.Huuma)]
    [InlineData(null, WeaponTypeCodes.Fist)]
    [InlineData("nonsense", WeaponTypeCodes.Fist)]
    public void WeaponTypeCodes_resolve_subtype_name(string? subtype, int expected)
        => Assert.Equal(expected, WeaponTypeCodes.FromSubtype(subtype));

    // ---- aggregator: WeaponType + bow arrow_atk ----

    [Fact]
    public void Aggregate_resolves_weapon_type_and_folds_arrow_atk_for_bows()
    {
        var catalog = new StubCatalog
        {
            [100] = Weapon("Bow", atk: 50, level: 4),
            [200] = Ammo(atk: 30),
        };
        var inv = new List<InventoryItem>
        {
            new() { NameId = 100, Equip = EquipBonusAggregator.EquipRightHand, Amount = 1 },
            new() { NameId = 200, Equip = EquipBonusAggregator.EquipAmmo, Amount = 50 },
        };

        var s = EquipBonusAggregator.Aggregate(inv, catalog);

        Assert.Equal(WeaponTypeCodes.Bow, s.WeaponType);
        Assert.Equal(80, s.WeaponAtkMin); // 50 bow + 30 arrow
        Assert.Equal(80, s.WeaponAtkMax);
        Assert.Equal(4, s.WeaponLevel);
    }

    [Fact]
    public void Aggregate_ignores_ammo_atk_for_non_ammo_weapons()
    {
        var catalog = new StubCatalog
        {
            [100] = Weapon("1hSword", atk: 100, level: 3),
            [200] = Ammo(atk: 30), // arrow equipped but a sword doesn't use it
        };
        var inv = new List<InventoryItem>
        {
            new() { NameId = 100, Equip = EquipBonusAggregator.EquipRightHand, Amount = 1 },
            new() { NameId = 200, Equip = EquipBonusAggregator.EquipAmmo, Amount = 50 },
        };

        var s = EquipBonusAggregator.Aggregate(inv, catalog);

        Assert.Equal(WeaponTypeCodes.OneHandSword, s.WeaponType);
        Assert.Equal(100, s.WeaponAtkMin); // ammo ATK NOT folded
    }

    // ---- helpers ----

    private static ItemEntity Weapon(string subtype, ushort atk, byte level) => new()
    {
        Type = "Weapon", Subtype = subtype, Attack = atk, WeaponLevel = level, Range = 1,
    };

    private static ItemEntity Ammo(ushort atk) => new()
    {
        Type = "Ammo", Subtype = "Arrow", Attack = atk,
    };

    private sealed class StubCatalog : IItemCatalog
    {
        private readonly Dictionary<uint, ItemEntity> _items = new();
        public ItemEntity this[uint id] { set => _items[id] = value; }
        public int Count => _items.Count;
        public ItemEntity? Get(uint id) => _items.GetValueOrDefault(id);
        public ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<ItemEntity> All() => _items.Values;
        public void Reload() { }
    }
}
