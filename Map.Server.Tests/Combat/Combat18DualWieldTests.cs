using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Entities;
using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-18 — dual-wield left-hand damage (battle_calc_attack_left_right_hands,
/// battle.cpp:7150). Off-hand weapon ATK feeds <see cref="BattleDamage.Damage2"/>;
/// the thief AS_RIGHT/AS_LEFT (and kagerou KO_RIGHT/KO_LEFT) masteries reduce each
/// hand; katar normal attacks get a TF_DOUBLE-scaled off-hand fraction.
/// </summary>
public class Combat18DualWieldTests
{
    // ---- aggregator: off-hand weapon capture (weapon vs shield) ----

    [Fact]
    public void Aggregate_captures_offhand_weapon_atk()
    {
        var catalog = new StubCatalog
        {
            [100] = Weapon("Dagger", atk: 70, level: 3),
            [200] = Weapon("Dagger", atk: 40, level: 2),
        };
        var inv = new List<InventoryItem>
        {
            new() { NameId = 100, Equip = EquipBonusAggregator.EquipRightHand, Amount = 1 },
            new() { NameId = 200, Equip = EquipBonusAggregator.EquipLeftHand, Amount = 1 },
        };

        var s = EquipBonusAggregator.Aggregate(inv, catalog);

        Assert.Equal(70, s.WeaponAtkMin);
        Assert.Equal(40, s.LeftWeaponAtkMin);
        Assert.Equal(40, s.LeftWeaponAtkMax);
        Assert.Equal(2, s.LeftWeaponLevel);
        Assert.Equal(WeaponTypeCodes.Dagger, s.LeftWeaponType);
    }

    [Fact]
    public void Aggregate_ignores_offhand_shield_as_weapon()
    {
        var catalog = new StubCatalog
        {
            [100] = Weapon("Dagger", atk: 70, level: 3),
            [300] = Shield(def: 20),
        };
        var inv = new List<InventoryItem>
        {
            new() { NameId = 100, Equip = EquipBonusAggregator.EquipRightHand, Amount = 1 },
            new() { NameId = 300, Equip = EquipBonusAggregator.EquipLeftHand, Amount = 1 },
        };

        var s = EquipBonusAggregator.Aggregate(inv, catalog);

        Assert.Equal(0, s.LeftWeaponAtkMin); // shield → no lhw ATK
        Assert.Equal(20, s.EquipDef);        // shield Def still counts
    }

    // ---- CalcWeaponAttack: Damage2 + the AS_RIGHT/AS_LEFT split ----

    [Fact]
    public void SingleWeapon_has_no_offhand_damage()
    {
        var pc = MakeAssassin(rightSwing: 50, leftSwing: 0);
        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(50, dmg.Damage);
        Assert.Equal(0, dmg.Damage2);
    }

    [Fact]
    public void DualWield_assassin_splits_both_hands_by_mastery()
    {
        // AS_RIGHT 5 → right ×(50+50)/100 = 100%; AS_LEFT 5 → left ×(30+50)/100 = 80%.
        var pc = MakeAssassin(rightSwing: 50, leftSwing: 40);
        pc.LearnedSkills[SkillIds.AS_RIGHT] = 5;
        pc.LearnedSkills[SkillIds.AS_LEFT] = 5;

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(50, dmg.Damage);   // 50 × 100%
        Assert.Equal(32, dmg.Damage2);  // 40 × 80%
        Assert.Equal(82, dmg.Total);
    }

    [Fact]
    public void DualWield_without_mastery_uses_base_percentages()
    {
        // No AS_RIGHT/AS_LEFT → right ×50%, left ×30%.
        var pc = MakeAssassin(rightSwing: 50, leftSwing: 40);

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(25, dmg.Damage);   // 50 × 50%
        Assert.Equal(12, dmg.Damage2);  // 40 × 30%
    }

    [Fact]
    public void Katar_normal_attack_offhand_is_tf_double_fraction()
    {
        // Katar: damage2 = damage * (1 + 2*TF_DOUBLE) / 100; right hand unmodified.
        var pc = MakeAssassin(rightSwing: 100, leftSwing: 0, rightType: WeaponTypeCodes.Katar);
        pc.LearnedSkills[SkillIds.TF_DOUBLE] = 10;

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(100, dmg.Damage);              // right hand untouched
        Assert.Equal(21, dmg.Damage2);              // 100 × (1 + 20) / 100
    }

    [Fact]
    public void Mob_attacker_never_dual_wields()
    {
        // Mobs have no lhw and the split is PC-only — Damage2 stays 0 even if
        // left stats were somehow set.
        var mob = MakeTarget();
        mob.Stats.WatkMin = mob.Stats.WatkMax = 50;
        mob.Stats.LeftWatkMax = 40; // ignored: mob source skips the left path
        mob.Stats.Hit = 10000; mob.Stats.Cri = 0;

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(mob, MakeTarget());

        Assert.Equal(0, dmg.Damage2);
    }

    // ---- wire: ZC_NOTIFY_ACT3 carries Damage2 ----

    [Fact]
    public void PerformMeleeAttack_broadcasts_offhand_damage2()
    {
        var ctx = NewDamageContext();
        var attacker = MakeAssassin(rightSwing: 50, leftSwing: 40);
        attacker.LearnedSkills[SkillIds.AS_RIGHT] = 5;
        attacker.LearnedSkills[SkillIds.AS_LEFT] = 5;
        ctx.Place(attacker, 50, 50);
        var mob = ctx.AddMob(52, 50, hp: 1000);

        var result = ctx.Service.PerformMeleeAttack(attacker, mob);

        Assert.Equal(32, result.Damage2);
        var act = (ZC_NOTIFY_ACT3)ctx.Dispatcher.Sent.Single(s => s.packet is ZC_NOTIFY_ACT3).packet;
        Assert.Equal(32, act.Damage2);
        Assert.Equal(50, act.Damage);
        Assert.Equal(918, mob.Hp); // 1000 - (50 + 32)
    }

    // ---- helpers ----

    private static PlayerEntity MakeAssassin(int rightSwing, int leftSwing, int rightType = WeaponTypeCodes.Dagger)
    {
        var pc = new PlayerEntity(1, 1, "Sin", Guid.NewGuid(), 0, 0, 0);
        pc.ClassMask = MapidClass.Assassin; // thief base nibble → AS_RIGHT/AS_LEFT apply
        pc.WeaponType = rightType;
        // Pin DEX so atkmin == atkmax for both hands → deterministic swing.
        pc.Stats.Dex = (short)Math.Max(rightSwing, leftSwing);
        pc.Stats.Batk = 0;
        pc.Stats.Cri = 0;
        pc.Stats.Hit = 10000;
        pc.Stats.WeaponLevel = 0;
        pc.Stats.WatkMin = (ushort)rightSwing;
        pc.Stats.WatkMax = (ushort)rightSwing;
        pc.Stats.LeftWeaponLevel = 0;
        pc.Stats.LeftWeaponType = WeaponTypeCodes.Dagger;
        pc.Stats.LeftWatkMin = (ushort)leftSwing;
        pc.Stats.LeftWatkMax = (ushort)leftSwing;
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium;
        m.Stats.Flee = 0; m.Stats.Flee2 = 0;
        return m;
    }

    private static ItemEntity Weapon(string subtype, ushort atk, byte level) => new()
    {
        Type = "Weapon", Subtype = subtype, Attack = atk, WeaponLevel = level, Range = 1,
    };

    private static ItemEntity Shield(ushort def) => new()
    {
        Type = "Armor", Subtype = "Shield", Defense = def,
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

    // ---- damage-service harness (mirrors DamageServiceTests) ----

    private static DamageContext NewDamageContext()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility, new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyCatalog(), itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var service = new DamageService(visibility, mobSpawn, entities, new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        return new DamageContext(service, entities, dispatcher, ids, (uint)mapName.GetHashCode());
    }

    private sealed record DamageContext(
        DamageService Service, EntityRegistry Entities, RecordingDispatcher Dispatcher,
        EntityIdAllocator Ids, uint MapId)
    {
        public void Place(PlayerEntity p, short x, short y)
        {
            p.MapId = MapId; p.X = x; p.Y = y;
            Entities.Add(p);
        }

        public MobEntity AddMob(short x, short y, int hp)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y) { Hp = hp };
            Entities.Add(m);
            return m;
        }
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

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string aegisName) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class EmptyCatalog : IItemCatalog
    {
        public int Count => 0;
        public ItemEntity? Get(uint itemId) => null;
        public ItemEntity? GetByAegisName(string aegisName) => null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }
}
