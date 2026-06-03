using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Entities;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors.Acolyte;
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
/// COMBAT-44 — bonus tail: per-skill <c>bSkillHeal</c> and on-hit HP/SP vanish.
/// (race2 / SubDefEle / bonus3-5 / pc_sub_skillatk are COMBAT-63 / COMBAT-64.)
/// </summary>
public class Combat44BonusTailTests
{
    // ---- extractor ----

    [Fact]
    public void Extractor_parses_skillheal()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bSkillHeal,AL_HEAL,10;", b);
        Assert.Equal(10, b.SkillHeal.GetValueOrDefault(SkillIds.AL_HEAL));
    }

    [Fact]
    public void Extractor_parses_hp_and_sp_vanish_rate()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bHPVanishRate,1000,5; bonus2 bSPVanishRate,500,3;", b);
        Assert.Equal(1000, b.HpVanishRate);
        Assert.Equal(5, b.HpVanishPer);
        Assert.Equal(500, b.SpVanishRate);
        Assert.Equal(3, b.SpVanishPer);
    }

    // ---- bSkillHeal consumer (renewal heal formula) ----

    [Fact]
    public void SkillHeal_boosts_heal_for_the_matching_skill()
    {
        var caster = NewPc(level: 50, intStat: 50);
        var target = NewPc(level: 50, intStat: 50);
        var heal = new Heal();

        Assert.Equal(600, heal.CalcRenewalHealForTest(caster, target, 10)); // base
        caster.EquipBonuses.SkillHeal[SkillIds.AL_HEAL] = 10;
        Assert.Equal(660, heal.CalcRenewalHealForTest(caster, target, 10)); // 600 × 1.10
    }

    // ---- on-hit vanish consumer ----

    [Fact]
    public void Hp_vanish_drains_a_percentage_of_target_max_hp_on_hit()
    {
        var ctx = NewContext();
        var attacker = ctx.AddPc(50, 50);
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 100;
        attacker.Stats.Dex = 100; attacker.Stats.Hit = 10000; attacker.Stats.Cri = 0;

        var noVanish = ctx.AddMob(52, 50, hp: 10000);
        ctx.Service.PerformMeleeAttack(attacker, noVanish);
        var plainDrop = 10000 - noVanish.Hp;

        attacker.EquipBonuses.HpVanishRate = 1000; // guaranteed
        attacker.EquipBonuses.HpVanishPer = 10;     // 10% of 10000 = 1000
        var withVanish = ctx.AddMob(54, 50, hp: 10000);
        ctx.Service.PerformMeleeAttack(attacker, withVanish);
        var vanishDrop = 10000 - withVanish.Hp;

        Assert.Equal(plainDrop + 1000, vanishDrop);
    }

    [Fact]
    public void Vanish_does_not_fire_when_rate_is_zero()
    {
        var ctx = NewContext();
        var attacker = ctx.AddPc(50, 50);
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 100;
        attacker.Stats.Dex = 100; attacker.Stats.Hit = 10000; attacker.Stats.Cri = 0;
        attacker.EquipBonuses.HpVanishPer = 10; // per set but rate 0 → no proc

        var mob = ctx.AddMob(52, 50, hp: 10000);
        ctx.Service.PerformMeleeAttack(attacker, mob);
        // The whole drop is just the weapon swing; no +1000 vanish.
        Assert.True(10000 - mob.Hp < 1000);
    }

    // ---- COMBAT-83: flag-gated vanish (bonus3 bHPVanishRate,x,n,bf) ----

    [Fact]
    public void Flag_gated_vanish_fires_only_when_the_attack_flag_matches()
    {
        var ctx = NewContext();
        var attacker = ctx.AddPc(50, 50);
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 100;
        attacker.Stats.Dex = 100; attacker.Stats.Hit = 10000; attacker.Stats.Cri = 0;
        attacker.Stats.AttackRange = 1; // melee → the attack is BF_SHORT
        attacker.EquipBonuses.HpVanishRate = 1000; // guaranteed roll
        attacker.EquipBonuses.HpVanishPer = 10;     // 10% of 10000 = 1000

        // bSubEle-style flag gate on BF_LONG → a melee (short) swing must NOT vanish.
        attacker.EquipBonuses.HpVanishFlag = BattleFlags.Default(BattleFlags.Long);
        var noFire = ctx.AddMob(52, 50, hp: 10000);
        var plainDrop = PlainSwingDrop(ctx, attacker);
        ctx.Service.PerformMeleeAttack(attacker, noFire);
        Assert.Equal(plainDrop, 10000 - noFire.Hp); // no +1000

        // Gate on BF_SHORT → the melee swing vanishes.
        attacker.EquipBonuses.HpVanishFlag = BattleFlags.Default(BattleFlags.Short);
        var fires = ctx.AddMob(56, 50, hp: 10000);
        ctx.Service.PerformMeleeAttack(attacker, fires);
        Assert.Equal(plainDrop + 1000, 10000 - fires.Hp);
    }

    private static int PlainSwingDrop(Ctx ctx, PlayerEntity attacker)
    {
        var probe = ctx.AddMob(40, 40, hp: 10000);
        var saved = attacker.EquipBonuses.HpVanishPer;
        attacker.EquipBonuses.HpVanishPer = 0; // suppress vanish for the probe
        ctx.Service.PerformMeleeAttack(attacker, probe);
        attacker.EquipBonuses.HpVanishPer = saved;
        return 10000 - probe.Hp;
    }

    // ---- helpers ----

    private static PlayerEntity NewPc(int level, int intStat)
    {
        var pc = new PlayerEntity(1, 1, "Heal", Guid.NewGuid(), 0, 0, 0) { Level = level };
        pc.Stats.IntStat = (short)intStat;
        return pc;
    }

    private sealed record Ctx(DamageService Service, EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity AddPc(short x, short y)
        {
            var pc = new PlayerEntity(1, 1, "Atk", Guid.NewGuid(), MapId, x, y);
            pc.Stats.MaxHp = pc.Hp = 5000;
            Entities.Add(pc);
            return pc;
        }
        public MobEntity AddMob(short x, short y, int hp)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = hp };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var m = new MobEntity(new EntityId(1000 + x), db, origin, MapId, x, y);
            m.MaxHp = m.Hp = hp;
            m.Stats.Def = 0; m.Stats.Def2 = 0;
            m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
            m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0; m.Stats.Flee2 = 0;
            Entities.Add(m);
            return m;
        }
    }

    private static Ctx NewContext()
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
        var service = new DamageService(visibility, mobSpawn, entities, new BattleCalculator(new Random(0)),
            NullLogger<DamageService>.Instance);
        return new Ctx(service, entities, ids, (uint)mapName.GetHashCode());
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
