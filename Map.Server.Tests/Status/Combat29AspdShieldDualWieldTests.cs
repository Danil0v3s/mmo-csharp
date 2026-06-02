using System;
using System.Collections.Generic;
using Core.Database.Entities;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-29 — dual-wield + shield ASPD base terms. rAthena
/// status_base_amotion_pc (status.cpp:2321): aspd_base[wt1] + aspd_base[Shield]
/// (shield) or + aspd_base[wt2]/4 (dual wield).
/// </summary>
public class Combat29AspdShieldDualWieldTests
{
    // ---- aggregator: HasShield detection ----

    [Fact]
    public void Aggregate_flags_offhand_shield_but_not_an_offhand_weapon()
    {
        var catalog = new StubCatalog
        {
            [100] = Weapon("Dagger", 50, 3),
            [300] = Shield(20),
            [200] = Weapon("Dagger", 40, 2),
        };

        var withShield = EquipBonusAggregator.Aggregate(new List<InventoryItem>
        {
            new() { NameId = 100, Equip = EquipBonusAggregator.EquipRightHand, Amount = 1 },
            new() { NameId = 300, Equip = EquipBonusAggregator.EquipLeftHand, Amount = 1 },
        }, catalog);
        Assert.True(withShield.HasShield);
        Assert.Equal(WeaponTypeCodes.Fist, withShield.LeftWeaponType); // a shield is not a weapon

        var dualWield = EquipBonusAggregator.Aggregate(new List<InventoryItem>
        {
            new() { NameId = 100, Equip = EquipBonusAggregator.EquipRightHand, Amount = 1 },
            new() { NameId = 200, Equip = EquipBonusAggregator.EquipLeftHand, Amount = 1 },
        }, catalog);
        Assert.False(dualWield.HasShield);
        Assert.Equal(WeaponTypeCodes.Dagger, dualWield.LeftWeaponType);
    }

    // ---- CalcPc: the base terms raise amotion ----

    [Fact]
    public void Shield_and_dual_wield_raise_base_amotion()
    {
        var aspd = new StubJobAspd(fist: 40, shield: 100, dagger: 80);
        var calc = new StatusCalcService(aspd);

        var single = Amotion(calc, hasShield: false, leftWeapon: WeaponTypeCodes.Fist);
        var shield = Amotion(calc, hasShield: true, leftWeapon: WeaponTypeCodes.Fist);
        var dual = Amotion(calc, hasShield: false, leftWeapon: WeaponTypeCodes.Dagger);

        Assert.True(shield > single); // shield base added → slower
        Assert.True(dual > single);   // dual-wield aspd_base[wt2]/4 added → slower
    }

    private static int Amotion(StatusCalcService calc, bool hasShield, int leftWeapon)
    {
        var pc = new PlayerEntity(1, 1, "Sin", Guid.NewGuid(), 0, 0, 0);
        calc.CalcPc(pc, new PcBaseInputs(
            BaseLevel: 99, JobLevel: 50,
            Str: 1, Agi: 50, Vit: 1, Int: 1, Dex: 50, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
            AttackRange: 1, WeaponLevel: 0, WeaponType: 0,
            LeftWeaponType: leftWeapon, HasShield: hasShield));
        return pc.Stats.Amotion;
    }

    // ---- helpers ----

    private static ItemEntity Weapon(string subtype, ushort atk, byte level) => new()
    { Type = "Weapon", Subtype = subtype, Attack = atk, WeaponLevel = level, Range = 1 };

    private static ItemEntity Shield(ushort def) => new() { Type = "Armor", Subtype = "Shield", Defense = def };

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

    /// <summary>Job-ASPD stub: fist (weapon 0), shield (99), dagger (1) rows for
    /// every job id; exact lookup returns 0 for anything else.</summary>
    private sealed class StubJobAspd : IJobAspdCacheService
    {
        private readonly Dictionary<int, int> _rows;
        public StubJobAspd(int fist, int shield, int dagger)
            => _rows = new Dictionary<int, int> { [0] = fist, [99] = shield, [WeaponTypeCodes.Dagger] = dagger };
        public int GetBaseAspd(string jobAegis, int weaponType) => _rows.GetValueOrDefault(weaponType, _rows[0]);
        public int GetBaseAspdByJobId(int jobId, int weaponType) => _rows.GetValueOrDefault(weaponType, _rows[0]);
        public int GetBaseAspdExactByJobId(int jobId, int weaponType) => _rows.GetValueOrDefault(weaponType, 0);
    }
}
