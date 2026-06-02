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
/// COMBAT-33 — derived-stat SC mods survive a CalcPc recalc. rAthena re-folds
/// every SCB_* contribution each status_calc_pc_; the C# port re-applies them via
/// each handler's OnRecalc after CalcMisc rebuilds the derived fields. Primary-
/// stat SC mods are preserved separately by the COMBAT-10 param-base delta, so the
/// re-fold must NOT touch them (no double-count).
/// </summary>
public class Combat33DerivedStatRefoldTests
{
    [Fact]
    public void Angelus_def2_bonus_survives_recalc_and_is_idempotent()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs(vit: 60));
        var baseDef2 = pc.Stats.Def2;

        sc.Start(pc, StatusType.Angelus, val1: 10, 0, 0, 0, durationMs: 60_000);
        // delta = vit/2 * (5*val1)/100 = 30 * 50/100 = 15
        var afterStart = pc.Stats.Def2;
        Assert.Equal(baseDef2 + 15, afterStart);

        calc.CalcPc(pc, Inputs(vit: 60));      // a recalc would wipe Def2 without the re-fold
        Assert.Equal(baseDef2 + 15, pc.Stats.Def2);

        calc.CalcPc(pc, Inputs(vit: 60));      // second recalc → no double-count
        Assert.Equal(baseDef2 + 15, pc.Stats.Def2);
    }

    [Fact]
    public void Provoke_batk_and_def_deltas_survive_recalc()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs(str: 60, equipDef: 100));
        var baseBatk = pc.Stats.Batk;
        var baseDef = pc.Stats.Def;
        Assert.Equal(100, baseDef);

        // val1=5 → batkPct=17, defPct=30. defDelta = 100*30/100 = 30.
        sc.Start(pc, StatusType.Provoke, val1: 5, 0, 0, 0, durationMs: 60_000);
        var batkDelta = pc.Stats.Batk - baseBatk;
        Assert.True(batkDelta > 0, "Provoke should raise Batk");
        Assert.Equal(baseDef - 30, pc.Stats.Def);

        calc.CalcPc(pc, Inputs(str: 60, equipDef: 100));
        Assert.Equal(baseBatk + batkDelta, pc.Stats.Batk); // preserved
        Assert.Equal(baseDef - 30, pc.Stats.Def);          // preserved

        calc.CalcPc(pc, Inputs(str: 60, equipDef: 100));   // idempotent
        Assert.Equal(baseBatk + batkDelta, pc.Stats.Batk);
        Assert.Equal(baseDef - 30, pc.Stats.Def);
    }

    [Fact]
    public void Concentration_hit_and_batk_and_def_survive_recalc()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs(str: 60, equipDef: 100));
        var baseHit = pc.Stats.Hit;
        var baseDef = pc.Stats.Def;

        sc.Start(pc, StatusType.Concentration, val1: 5, 0, 0, 0, durationMs: 60_000);
        var hitDelta = pc.Stats.Hit - baseHit; // val1*10 = 50
        Assert.Equal(50, hitDelta);
        var defAfter = pc.Stats.Def;
        Assert.True(defAfter < baseDef);

        calc.CalcPc(pc, Inputs(str: 60, equipDef: 100));
        Assert.Equal(baseHit + 50, pc.Stats.Hit);
        Assert.Equal(defAfter, pc.Stats.Def);
    }

    [Fact]
    public void Generated_scb_derived_mod_survives_recalc_and_is_idempotent()
    {
        // The generic generator-default path (the bulk SCB_* stat-mod set).
        // Adjustment tags {Hit, Flee} — both derived → wiped by CalcMisc without
        // the OnRecalc re-fold.
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var baseHit = pc.Stats.Hit;

        sc.Start(pc, StatusType.Adjustment, val1: 20, 0, 0, 0, durationMs: 60_000);
        Assert.Equal(baseHit + 20, pc.Stats.Hit);

        calc.CalcPc(pc, Inputs());
        Assert.Equal(baseHit + 20, pc.Stats.Hit); // re-folded, not wiped

        calc.CalcPc(pc, Inputs());
        Assert.Equal(baseHit + 20, pc.Stats.Hit); // no double
    }

    [Fact]
    public void Primary_stat_sc_mod_is_not_double_counted_on_recalc()
    {
        // Blessing adds +Val1 to STR/INT/DEX (primary) — preserved by the
        // COMBAT-10 param-base delta. The derived re-fold must NOT also re-add it
        // (that would double the primary bonus each recalc).
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var baseStr = pc.Stats.Str;

        sc.Start(pc, StatusType.Blessing, val1: 10, 0, 0, 0, durationMs: 60_000);
        var buffedStr = pc.Stats.Str;
        Assert.True(buffedStr > baseStr);

        calc.CalcPc(pc, Inputs());
        calc.CalcPc(pc, Inputs());
        Assert.Equal(buffedStr, pc.Stats.Str); // stable across recalcs, no double
    }

    // ---- harness ----

    private static PlayerEntity NewPc() => new(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);

    private static PcBaseInputs Inputs(int str = 1, int vit = 1, int equipDef = 0) => new(
        BaseLevel: 99, JobLevel: 50,
        Str: str, Agi: 50, Vit: vit, Int: 1, Dex: 50, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: equipDef, EquipMdef: 0,
        AttackRange: 1, WeaponLevel: 0, WeaponType: 1);

    private static (StatusCalcService calc, StatusChangeService sc) Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 100, 100, new byte[100 * 100]);
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
        var damage = new DamageService(visibility, mobSpawn, entities, new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        var calc = new StatusCalcService(sc: new Lazy<IStatusChangeService>(() => sc));
        return (calc, sc);
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
        public Core.Database.Entities.ItemEntity? Get(uint itemId) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string aegisName) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }
}
