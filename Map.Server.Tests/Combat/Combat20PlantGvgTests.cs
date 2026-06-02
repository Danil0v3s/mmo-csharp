using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Entities;
using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
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
/// COMBAT-20 — plant 1-damage (battle_calc_attack_plant / is_infinite_defense,
/// battle.cpp:7074/2823) + GvG/BG zone scaling (battle_calc_gvg/bg_damage,
/// battle.cpp:2121/2046).
/// </summary>
public class Combat20PlantGvgTests
{
    // ---- is_infinite_defense (plant predicate) ----

    [Theory]
    [InlineData(MobMode.IgnoreMelee, BattleAttackType.Weapon, true, true)]    // melee vs melee-immune → plant
    [InlineData(MobMode.IgnoreMelee, BattleAttackType.Weapon, false, false)]  // ranged vs melee-immune → not plant
    [InlineData(MobMode.IgnoreRanged, BattleAttackType.Weapon, false, true)]  // ranged vs ranged-immune → plant
    [InlineData(MobMode.IgnoreMelee, BattleAttackType.Magic, true, false)]    // magic vs melee-immune → not plant
    [InlineData(MobMode.IgnoreMagic, BattleAttackType.Magic, false, true)]    // magic vs magic-immune → plant
    [InlineData(MobMode.IgnoreMisc, BattleAttackType.Misc, false, true)]      // misc vs misc-immune → plant
    [InlineData(MobMode.None, BattleAttackType.Weapon, true, false)]          // normal mob → not plant
    public void IsInfiniteDefense_matches_mode_and_lane(MobMode mode, BattleAttackType lane, bool isShort, bool expected)
    {
        var target = MakeMob();
        target.Stats.Mode = mode;
        Assert.Equal(expected, BattleCalculator.IsInfiniteDefense(target, lane, isShort));
    }

    // ---- plant clamp through the magic path ----

    [Fact]
    public void Magic_on_plant_deals_one()
    {
        var calc = new BattleCalculator(new Random(0));
        var caster = NewMage(matk: 500);
        var plant = MakeMob();
        plant.Stats.Mode = MobMode.IgnoreMagic;

        var dmg = calc.CalcMagicAttack(caster, plant, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100);
        Assert.Equal(1, dmg.Damage);
    }

    [Fact]
    public void Magic_on_melee_immune_mob_is_not_clamped()
    {
        // A Flora ignores melee, not magic → Fire Bolt deals full damage.
        var calc = new BattleCalculator(new Random(0));
        var caster = NewMage(matk: 500);
        var mob = MakeMob();
        mob.Stats.Mode = MobMode.IgnoreMelee;

        var dmg = calc.CalcMagicAttack(caster, mob, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100);
        Assert.Equal(500, dmg.Damage); // (500+500)/2 × 100%
    }

    [Fact]
    public void Auto_attack_on_plant_deals_one_hp()
    {
        var ctx = NewDamageContext(gvg: false);
        var attacker = NewSwinger(swing: 80);
        ctx.Place(attacker, 50, 50);
        var plant = ctx.AddMob(52, 50, hp: 1000, mode: MobMode.IgnoreMelee);

        var result = ctx.Service.PerformMeleeAttack(attacker, plant);

        Assert.Equal(1, result.Damage);
        Assert.Equal(999, plant.Hp);
    }

    // ---- GvG / BG zone scaling ----

    [Fact]
    public void Zone_scale_gvg_skill_uses_lane_rate()
    {
        var zone = NewZone(gvg: true);
        var src = NewMage(matk: 0);
        src.MapId = (uint)"woe".GetHashCode();
        // Magic skill on a GvG map → gvg_magic_damage_rate default 60%.
        Assert.Equal(60, zone.Scale(BattleAttackType.Magic, src, src, 100, isSkill: true, isShortRange: false, skillId: 0));
        // Weapon skill → gvg_weapon 60%.
        Assert.Equal(60, zone.Scale(BattleAttackType.Weapon, src, src, 100, isSkill: true, isShortRange: false, skillId: 0));
    }

    [Fact]
    public void Zone_scale_gvg_normal_attack_uses_range_rate()
    {
        var zone = NewZone(gvg: true);
        var src = NewSwinger(swing: 0);
        src.MapId = (uint)"woe".GetHashCode();
        // Normal melee (short) on GvG → gvg_short 80%.
        Assert.Equal(80, zone.Scale(BattleAttackType.Weapon, src, src, 100, isSkill: false, isShortRange: true, skillId: 0));
    }

    [Fact]
    public void Zone_scale_non_zone_map_is_unchanged()
    {
        var zone = NewZone(gvg: false);
        var src = NewSwinger(swing: 0);
        src.MapId = (uint)"prontera".GetHashCode();
        Assert.Equal(100, zone.Scale(BattleAttackType.Weapon, src, src, 100, isSkill: true, isShortRange: false, skillId: 0));
    }

    [Fact]
    public void Magic_skill_on_gvg_map_is_reduced()
    {
        var zone = NewZone(gvg: true);
        var calc = new BattleCalculator(rng: new Random(0), cards: null, sc: null, mado: null, elements: null, zone: zone);
        var caster = NewMage(matk: 500);
        caster.MapId = (uint)"woe".GetHashCode();
        var target = MakeMob();

        var dmg = calc.CalcMagicAttack(caster, target, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100);
        Assert.Equal(300, dmg.Damage); // 500 × 60%
    }

    // ---- helpers ----

    private static ZoneDamageService NewZone(bool gvg)
    {
        var map = new MapData(gvg ? "woe" : "prontera", 50, 50, new byte[50 * 50]);
        return new ZoneDamageService(new StubFlags(gvg ? "woe" : null), new StubWorld(map));
    }

    private static PlayerEntity NewMage(int matk)
    {
        var p = new PlayerEntity(1, 1, "Mage", Guid.NewGuid(), 0, 0, 0);
        p.Stats.MatkMin = p.Stats.MatkMax = (ushort)matk;
        return p;
    }

    private static PlayerEntity NewSwinger(int swing)
    {
        var p = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        p.Stats.Dex = (short)swing;
        p.Stats.WeaponLevel = 0;
        p.Stats.WatkMin = p.Stats.WatkMax = (ushort)swing;
        p.Stats.Batk = 0; p.Stats.Cri = 0; p.Stats.Hit = 10000;
        p.Stats.AttackRange = 1; // melee → short range
        return p;
    }

    private static MobEntity MakeMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Mdef = 0; m.Stats.Mdef2 = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0; m.Stats.Flee2 = 0;
        return m;
    }

    private sealed class StubFlags : IMapFlagService
    {
        private readonly string? _gvgMap;
        public StubFlags(string? gvgMap) => _gvgMap = gvgMap;
        public bool IsSet(string mapName, MapFlag flag)
            => flag == MapFlag.Gvg && _gvgMap != null && string.Equals(mapName, _gvgMap, StringComparison.OrdinalIgnoreCase);
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

    // ---- damage-service harness ----

    private DamageContext NewDamageContext(bool gvg)
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
        var zone = new ZoneDamageService(new StubFlags(gvg ? mapName : null), world);
        var service = new DamageService(visibility, mobSpawn, entities, new BattleCalculator(new Random(0)),
            NullLogger<DamageService>.Instance, zone: zone);
        return new DamageContext(service, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record DamageContext(DamageService Service, EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public void Place(PlayerEntity p, short x, short y) { p.MapId = MapId; p.X = x; p.Y = y; Entities.Add(p); }
        public MobEntity AddMob(short x, short y, int hp, MobMode mode)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y) { Hp = hp };
            m.Stats.Mode = mode;
            Entities.Add(m);
            return m;
        }
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
