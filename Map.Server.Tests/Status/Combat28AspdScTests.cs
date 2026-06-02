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
/// COMBAT-28 — SC ASPD contributions. rAthena status_calc_aspd (fixed/rate) +
/// status_calc_fix_aspd feed the renewal amotion formula.
/// </summary>
public class Combat28AspdScTests
{
    // ---- formula: each SC term moves amotion the right way ----

    [Fact]
    public void FixedSc_lowers_amotion()
    {
        // The (fixedSc * agi / 200) term raises aspd → lowers amotion (agi 100,
        // away from the 95ms floor).
        var baseline = StatusCalcService.RenewalPcAmotion(40, 50, 100, weaponType: 1, aspdRate2: 0, aspdAddVal: 0);
        var quickened = StatusCalcService.RenewalPcAmotion(40, 50, 100, weaponType: 1, aspdRate2: 0, aspdAddVal: 0, fixedSc: 7);
        Assert.True(quickened < baseline);
        Assert.True(baseline > 95); // not clamped, so the comparison is meaningful
    }

    [Fact]
    public void RateSc_reduction_raises_amotion()
    {
        // A negative rate term (e.g. Steel Body −25) reduces aspd → higher amotion.
        var baseline = StatusCalcService.RenewalPcAmotion(40, 50, 100, weaponType: 1, aspdRate2: 0, aspdAddVal: 0);
        var slowed = StatusCalcService.RenewalPcAmotion(40, 50, 100, weaponType: 1, aspdRate2: 0, aspdAddVal: 0, rateSc: -25);
        Assert.True(slowed > baseline);
    }

    [Fact]
    public void FixAspd_adds_flat_amotion_reduction()
    {
        var baseline = StatusCalcService.RenewalPcAmotion(40, 50, 100, weaponType: 1, aspdRate2: 0, aspdAddVal: 0);
        var heat = StatusCalcService.RenewalPcAmotion(40, 50, 100, weaponType: 1, aspdRate2: 0, aspdAddVal: 0, fixAspd: 30);
        Assert.Equal(baseline - 30, heat);
    }

    // ---- integration: live SC list drives CalcPc amotion ----

    [Fact]
    public void TwoHandQuicken_speeds_up_attacks_and_quagmire_gates_it()
    {
        var (calc, sc) = Build();
        var pc = NewPc();

        calc.CalcPc(pc, Inputs());
        var unbuffed = pc.Stats.Amotion;

        // Two-Hand Quicken → faster.
        sc.Start(pc, StatusType.Twohandquicken, val1: 1, 0, 0, 0, durationMs: 60_000);
        calc.CalcPc(pc, Inputs());
        Assert.True(pc.Stats.Amotion < unbuffed);
        var quickened = pc.Stats.Amotion;

        // Quagmire gates off the quicken bonus → amotion rises back above the
        // quickened value (Quagmire also slows via its AGI cut, so it lands at
        // or above the unbuffed rate).
        sc.Start(pc, StatusType.Quagmire, val1: 1, 0, 0, 0, durationMs: 60_000);
        calc.CalcPc(pc, Inputs());
        Assert.True(pc.Stats.Amotion > quickened); // quicken removed → slower
        Assert.True(pc.Stats.Amotion >= unbuffed);  // Quagmire raises amotion
    }

    [Fact]
    public void Berserk_grants_the_aspd_bonus()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var unbuffed = pc.Stats.Amotion;

        sc.Start(pc, StatusType.Berserk, val1: 1, 0, 0, 0, durationMs: 60_000);
        calc.CalcPc(pc, Inputs());
        Assert.True(pc.Stats.Amotion < unbuffed); // +15 fixed bonus
    }

    // ---- helpers ----

    private static PlayerEntity NewPc() => new(1, 1, "Sin", Guid.NewGuid(), 0, 0, 0);

    private static PcBaseInputs Inputs() => new(
        BaseLevel: 99, JobLevel: 50,
        Str: 1, Agi: 90, Vit: 1, Int: 1, Dex: 50, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
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
