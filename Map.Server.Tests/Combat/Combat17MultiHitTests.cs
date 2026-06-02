using System;
using System.Linq;
using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Thief;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-17 — multi-hit div (battle_calc_multi_attack + ZC_NOTIFY_ACT3 div wire).
/// Covers the auto-attack double-attack roll (battle.cpp:4438), the skill-plugin
/// hit count (Sonic Blow → 8, rAthena skill_db <c>num</c> -8 magnitude), and the
/// BroadcastAct div wiring (clif_damage's <c>div</c> arg).
/// </summary>
public class Combat17MultiHitTests
{
    // ---- skill-plugin hit count (rAthena skill_get_num) ----

    [Fact]
    public void SonicBlow_reports_eight_hits()
        => Assert.Equal(8, new SonicBlow().GetMultiHitCount(skillLevel: 5));

    [Fact]
    public void DoubleAttack_reports_two_hits()
        // COMBAT-39: TF_DOUBLE HitCount = 2, now sourced from the SkillHitCounts
        // table via the GetMultiHitCount default (was a stale "single-hit" example).
        => Assert.Equal(2, new DoubleAttack().GetMultiHitCount(skillLevel: 1));

    // ---- auto-attack double attack (battle_calc_multi_attack) ----

    [Fact]
    public void DoubleAttack_proc_sets_two_hits_and_doubles_damage()
    {
        // double_rate = 100 → maxRate 100 → roll always succeeds, so the
        // single swing becomes a 2-hit positive-div multi (damage ×2).
        var pc = MakeSwinger(weapon: WeaponTypeCodes.Dagger, swing: 50);
        pc.EquipBonuses.DoubleRate = 100;

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(2, dmg.Hits);
        Assert.Equal(100, dmg.Damage);            // 50 per-hit × 2
        Assert.Equal(DamageActionType.MultiHit, dmg.Type);
    }

    [Fact]
    public void DoubleAttack_via_TfDouble_requires_dagger()
    {
        // TF_DOUBLE learned but a sword equipped → the dagger gate fails and
        // no other trigger is set → single hit, full single damage.
        var pc = MakeSwinger(weapon: WeaponTypeCodes.OneHandSword, swing: 50);
        pc.LearnedSkills[Map.Server.Skills.SkillIds.TF_DOUBLE] = 10;

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(1, dmg.Hits);
        Assert.Equal(50, dmg.Damage);
    }

    [Fact]
    public void DoubleAttack_via_TfDouble_with_dagger_procs_at_full_rate()
    {
        // 7 * lv10 = 70 %; pin the rng so the < 70 roll passes deterministically.
        var pc = MakeSwinger(weapon: WeaponTypeCodes.Dagger, swing: 50);
        pc.LearnedSkills[Map.Server.Skills.SkillIds.TF_DOUBLE] = 10;

        var dmg = new BattleCalculator(new SeqRandom(hitRoll: 0, multiRoll: 0))
            .CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(2, dmg.Hits);
        Assert.Equal(100, dmg.Damage);
    }

    [Fact]
    public void DoubleRate_fails_bare_handed()
    {
        // double_rate trigger is gated on weapontype1 != W_FIST.
        var pc = MakeSwinger(weapon: WeaponTypeCodes.Fist, swing: 50);
        pc.EquipBonuses.DoubleRate = 100;

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(1, dmg.Hits);
        Assert.Equal(50, dmg.Damage);
    }

    [Fact]
    public void NoTrigger_is_single_hit()
    {
        var pc = MakeSwinger(weapon: WeaponTypeCodes.Dagger, swing: 50);

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(pc, MakeTarget());

        Assert.Equal(1, dmg.Hits);
    }

    [Fact]
    public void Mob_source_never_double_attacks()
    {
        // CalcMultiAttack is PC-only (sd && !skill_id); a mob auto-swing stays
        // single even though the calc path is shared.
        var mob = MakeTarget();
        mob.Stats.WatkMin = mob.Stats.WatkMax = 50;
        mob.Stats.Hit = 10000;
        mob.Stats.Cri = 0;

        var dmg = new BattleCalculator(new Random(0)).CalcWeaponAttack(mob, MakeTarget());

        Assert.Equal(1, dmg.Hits);
    }

    // ---- wire: BroadcastAct emits Div == hits ----

    [Fact]
    public void BroadcastAct_emits_div_equal_to_hits()
    {
        var ctx = NewDamageContext();
        var caller = ctx.AddPlayer(50, 50, 1);
        var mob = ctx.AddMob(52, 50, hp: 1000);

        // Skill-style call: total damage already resolved, displayed as 8 hits.
        ctx.Service.ApplyDamage(mob, 80, caller, hits: 8);

        var act = (ZC_NOTIFY_ACT3)ctx.Dispatcher.Sent
            .Single(s => s.packet is ZC_NOTIFY_ACT3).packet;
        Assert.Equal((short)8, act.Div);
        Assert.Equal(80, act.Damage);   // HP delta is the full total, not per-hit
        Assert.Equal(920, mob.Hp);
    }

    [Fact]
    public void BroadcastAct_defaults_div_to_one()
    {
        var ctx = NewDamageContext();
        var caller = ctx.AddPlayer(50, 50, 1);
        var mob = ctx.AddMob(52, 50, hp: 100);

        ctx.Service.ApplyDamage(mob, 30, caller);

        var act = (ZC_NOTIFY_ACT3)ctx.Dispatcher.Sent
            .Single(s => s.packet is ZC_NOTIFY_ACT3).packet;
        Assert.Equal((short)1, act.Div);
    }

    // ---- helpers ----

    private static PlayerEntity MakeSwinger(int weapon, int swing)
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.WeaponType = weapon;
        // PC atkmin is DEX-derived (battle.cpp:2453); pin DEX >= atkMax so the
        // floor equals the max → no roll, swing == `swing` deterministically.
        pc.Stats.Dex = (short)swing;
        pc.Stats.WeaponLevel = 0;          // weaponLv 0 → atkmin = DEX (capped at atkMax)
        pc.Stats.WatkMin = (ushort)swing;
        pc.Stats.WatkMax = (ushort)swing;  // atkMax == atkMin == swing
        pc.Stats.Batk = 0;
        pc.Stats.Cri = 0;                   // no critical roll
        pc.Stats.Hit = 10000;              // always hits
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

    /// <summary>Deterministic rng: returns fixed values for the hit roll then
    /// the multi-attack roll, so a <c>Next(100) &lt; rate</c> test is stable.</summary>
    private sealed class SeqRandom : Random
    {
        private readonly int[] _values;
        private int _i;
        public SeqRandom(int hitRoll, int multiRoll) => _values = new[] { hitRoll, multiRoll };
        public override int Next(int maxValue) => _i < _values.Length ? _values[_i++] : 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }

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
        public PlayerEntity AddPlayer(short x, short y, int charId)
        {
            var p = new PlayerEntity(charId, charId * 10, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            Entities.Add(p);
            return p;
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
        private readonly System.Collections.Generic.Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) => _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public System.Collections.Generic.IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string aegisName) => null;
        public System.Collections.Generic.IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class EmptyCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint itemId) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string aegisName) => null;
        public System.Collections.Generic.IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }
}
