using System;
using System.Collections.Generic;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// SKILL-01 — the status-change resist pipeline (renewal status_get_sc_def +
/// the rate-aware Start roll). Pins stat resistance, level-diff scaling, boss
/// immunity, Curse LUK-0 immunity, the NoAvoid bypass, duration reduction, and
/// the seeded apply roll.
/// </summary>
public class Skill01ScDefTests
{
    // ---- GetScDef: stat resistance ----

    [Fact]
    public void HighVit_ResistsStun_MoreThanLowVit()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var lowVit = ctx.AddPlayer(2, 100, 100); lowVit.Level = 50; lowVit.Stats.Vit = 1;
        var highVit = ctx.AddPlayer(3, 100, 100); highVit.Level = 50; highVit.Stats.Vit = 99;

        // rate 3000 (=30%), same level → levelAdv 0. sc_def = vit*100.
        var (rLow, _) = ctx.Service.GetScDef(src, lowVit, StatusType.Stun, 3000, 3000, ScStartFlag.None);
        var (rHigh, _) = ctx.Service.GetScDef(src, highVit, StatusType.Stun, 3000, 3000, ScStartFlag.None);

        // low: 3000 - 3000*100/10000 = 2970. high: 3000 - 3000*9900/10000 = 30.
        Assert.Equal(2970, rLow);
        Assert.Equal(30, rHigh);
        Assert.True(rHigh < rLow);
    }

    // ---- SC-IMMUNE: bResEff effect-resist cards ----

    [Fact]
    public void EffectResistCard_ReducesScRateAndDuration()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var plain = ctx.AddPlayer(2, 100, 100); plain.Level = 50; plain.Stats.Vit = 1;
        var resist = ctx.AddPlayer(3, 100, 100); resist.Level = 50; resist.Stats.Vit = 1;
        resist.EquipBonuses.ResEff[StatusType.Stun] = 5000; // bonus2 bResEff, Eff_Stun, 5000 → 50%

        var (rPlain, dPlain) = ctx.Service.GetScDef(src, plain, StatusType.Stun, 3000, 3000, ScStartFlag.None);
        var (rResist, dResist) = ctx.Service.GetScDef(src, resist, StatusType.Stun, 3000, 3000, ScStartFlag.None);

        Assert.Equal(2970, rPlain);    // vit*100 sc_def only: 3000 - 30
        Assert.Equal(1490, rResist);   // + 50% item resist: 2970 → 1485 → Aegis-rounded up to 1490
        Assert.True(dResist < dPlain); // renewal: item resistance also cuts the duration
    }

    [Fact]
    public void EffectResistCard_OnlyAffectsTheMatchingSc()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Vit = 1;
        t.EquipBonuses.ResEff[StatusType.Stun] = 5000; // resist Stun only
        var noCard = ctx.AddPlayer(3, 100, 100); noCard.Level = 50; noCard.Stats.Vit = 1; // identical, no card

        var (rStun, _) = ctx.Service.GetScDef(src, t, StatusType.Stun, 3000, 3000, ScStartFlag.None);
        var (rStunPlain, _) = ctx.Service.GetScDef(src, noCard, StatusType.Stun, 3000, 3000, ScStartFlag.None);
        var (rSleepCarded, _) = ctx.Service.GetScDef(src, t, StatusType.Sleep, 3000, 3000, ScStartFlag.None);
        var (rSleepPlain, _) = ctx.Service.GetScDef(src, noCard, StatusType.Sleep, 3000, 3000, ScStartFlag.None);

        Assert.True(rStun < rStunPlain);          // the Stun card resists Stun
        Assert.Equal(rSleepPlain, rSleepCarded);  // …but does NOT affect Sleep
    }

    [Fact]
    public void BResEff_bonus_parses_into_the_reseff_map()
    {
        var b = new Map.Server.Inventory.EquipBonusBundle();
        Map.Server.Inventory.BonusScriptExtractor.ApplyIndexedBonus(b, "ResEff", "Eff_Stun", 5000);
        Map.Server.Inventory.BonusScriptExtractor.ApplyIndexedBonus(b, "ResEff", "Eff_Stun", 1000); // stacks
        Map.Server.Inventory.BonusScriptExtractor.ApplyIndexedBonus(b, "ResEff", "Eff_Poison", 2000);
        Assert.Equal(6000, b.ResEff[StatusType.Stun]);
        Assert.Equal(2000, b.ResEff[StatusType.Poison]);
    }

    [Fact]
    public void Freeze_ResistedByMdef()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Mdef = 20;
        // sc_def = mdef*100 = 2000 → rate 5000 - 5000*2000/10000 = 4000.
        var (rate, _) = ctx.Service.GetScDef(src, t, StatusType.Freeze, 5000, 5000, ScStartFlag.None);
        Assert.Equal(4000, rate);
    }

    [Fact]
    public void LevelDifference_HighLevelAttacker_BypassesResist()
    {
        var ctx = Build();
        var lowSrc = ctx.AddPlayer(1, 100, 100); lowSrc.Level = 1;
        var hiSrc = ctx.AddPlayer(2, 100, 100); hiSrc.Level = 99;
        var t = ctx.AddPlayer(3, 100, 100); t.Level = 1; t.Stats.Vit = 50;

        // same-level: sc_def = 5000 → rate 3000 - 1500 = 1500.
        var (rSame, _) = ctx.Service.GetScDef(lowSrc, t, StatusType.Stun, 3000, 3000, ScStartFlag.None);
        // lv99 vs lv1: levelAdv = (98^2/5)*100 = 192000 → sc_def clamps to 0 → no resist.
        var (rHi, _) = ctx.Service.GetScDef(hiSrc, t, StatusType.Stun, 3000, 3000, ScStartFlag.None);

        Assert.Equal(1500, rSame);
        Assert.Equal(3000, rHi);
        Assert.True(rHi > rSame); // higher-level caster lands more often
    }

    // ---- Boss / MVP immunity ----

    [Fact]
    public void BossMob_ImmuneToStun_NormalMobNot()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var normal = ctx.AddMob(100, 100); normal.Level = 50; normal.Stats.Vit = 1;
        var boss = ctx.AddMob(100, 100); boss.Level = 50; boss.Stats.Vit = 1;
        boss.Stats.Mode |= MobMode.StatusImmune;

        var (rNormal, _) = ctx.Service.GetScDef(src, normal, StatusType.Stun, 3000, 3000, ScStartFlag.None);
        var (rBoss, _) = ctx.Service.GetScDef(src, boss, StatusType.Stun, 3000, 3000, ScStartFlag.None);

        Assert.True(rNormal > 0);
        Assert.Equal(0, rBoss); // immune
    }

    [Fact]
    public void NoAvoid_BypassesBossImmunity_ButRateDefStillApplies()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var boss = ctx.AddMob(100, 100); boss.Level = 50; boss.Stats.Vit = 99;
        boss.Stats.Mode |= MobMode.StatusImmune;

        // NoAvoid bypasses the boss-immunity gate (rAthena status_change_start:9897),
        // but the VIT rate-reduction is gated by NoRateDef, not NoAvoid — so the SC
        // can land yet is still resisted: 3000 - 3000*9900/10000 = 30.
        var (rateNoAvoid, _) = ctx.Service.GetScDef(src, boss, StatusType.Stun, 3000, 3000, ScStartFlag.NoAvoid);
        Assert.Equal(30, rateNoAvoid); // not immune (>0), but resisted

        // NoAvoid + NoRateDef → bypasses both immunity and rate resistance.
        var (rateBoth, _) = ctx.Service.GetScDef(src, boss, StatusType.Stun, 3000, 3000,
            ScStartFlag.NoAvoid | ScStartFlag.NoRateDef);
        Assert.Equal(3000, rateBoth);
    }

    [Fact]
    public void Curse_ImmuneWhenTargetLukZero()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Luk = 0;
        var (rate, _) = ctx.Service.GetScDef(src, t, StatusType.Curse, 5000, 5000, ScStartFlag.None);
        Assert.Equal(0, rate);
    }

    // ---- Duration reduction ----

    [Fact]
    public void Duration_ReducedByResist_ThenTickDef2()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Vit = 50;
        // tick_def = sc_def = 5000 → 3000 - 3000*5000/10000 = 1500; tick_def2=-500 → 1500-(-500)=2000.
        var (_, dur) = ctx.Service.GetScDef(src, t, StatusType.Stun, 3000, 3000, ScStartFlag.None);
        Assert.Equal(2000, dur);
    }

    [Fact]
    public void NoTickDef_KeepsRawDuration()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Vit = 50;
        var (_, dur) = ctx.Service.GetScDef(src, t, StatusType.Stun, 3000, 3000, ScStartFlag.NoTickDef);
        Assert.Equal(3000, dur);
    }

    // ---- Start: seeded roll ----

    [Fact]
    public void Start_AppliesWhenRollUnderResistedRate()
    {
        var ctx = Build(roll: 0); // roll 0 < any positive rate
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Vit = 1;

        var sc = ctx.Service.Start(t, StatusType.Stun, rate: 3000, 1, 0, 0, 0, durationMs: 3000, source: src);
        Assert.NotNull(sc);
        Assert.NotNull(ctx.Service.Get(t, StatusType.Stun));
    }

    [Fact]
    public void Start_FailsWhenRollAtOrAboveResistedRate()
    {
        var ctx = Build(roll: 9999); // 9999 >= 2970
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Vit = 1;

        var sc = ctx.Service.Start(t, StatusType.Stun, rate: 3000, 1, 0, 0, 0, durationMs: 3000, source: src);
        Assert.Null(sc);
        Assert.Null(ctx.Service.Get(t, StatusType.Stun));
    }

    [Fact]
    public void Start_NoRateWrapper_IsGuaranteed_EvenOnHighRoll()
    {
        var ctx = Build(roll: 9999); // would fail any rolled proc
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var t = ctx.AddPlayer(2, 100, 100); t.Level = 50; t.Stats.Agi = 99;

        // The legacy no-rate Start (self-buff path) must always apply.
        var sc = ctx.Service.Start(t, StatusType.IncreaseAgi, 5, 0, 0, 0, durationMs: 10000, source: src);
        Assert.NotNull(sc);
    }

    [Fact]
    public void Start_BossImmune_DebuffDoesNotLand()
    {
        var ctx = Build(roll: 0);
        var src = ctx.AddPlayer(1, 100, 100); src.Level = 50;
        var boss = ctx.AddMob(100, 100); boss.Level = 50; boss.Stats.Vit = 1;
        boss.Stats.Mode |= MobMode.StatusImmune;

        var sc = ctx.Service.Start(boss, StatusType.Stun, rate: 10000, 1, 0, 0, 0, durationMs: 3000, source: src);
        Assert.Null(sc);
    }

    // ---------- rig ----------

    private static TestContext Build(int roll = 0)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyItemCatalog(), itemDrops,
            movement, visibility, ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var service = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance, rng: new FixedRandom(roll));
        return new TestContext(service, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        StatusChangeService Service, EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 1000 };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
        }
    }

    private sealed class FixedRandom : Random
    {
        private readonly int _v;
        public FixedRandom(int v) => _v = v;
        public override int Next(int maxValue) => _v;
        public override int Next(int minValue, int maxValue) => _v;
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string n) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
