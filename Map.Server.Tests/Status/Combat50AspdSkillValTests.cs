using System;
using System.Collections.Generic;
using System.Linq;
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
using WT = Map.Server.Inventory.WeaponTypeCodes;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-50 — renewal ASPD skill-val terms (status.cpp:2343-2353), FREECAST
/// (status.cpp:6156), and the exotic status_calc_fix_aspd / rate SCs (status.cpp:6172).
/// </summary>
public class Combat50AspdSkillValTests
{
    // ---- skill val terms (ComputeSkillAspdVal) ----

    [Fact]
    public void GsSingleAction_adds_val_only_with_a_gun()
    {
        var pc = NewPc();
        pc.LearnedSkills[SkillIds.GS_SINGLEACTION] = 5;
        // (5+1)/2 = 3 with a revolver…
        Assert.Equal(3, StatusCalcService.ComputeSkillAspdVal(pc, WT.Revolver));
        Assert.Equal(3, StatusCalcService.ComputeSkillAspdVal(pc, WT.Grenade));
        // …0 with a non-gun.
        Assert.Equal(0, StatusCalcService.ComputeSkillAspdVal(pc, WT.Dagger));
    }

    [Fact]
    public void AdvancedBook_adds_val_only_with_a_book()
    {
        var pc = NewPc();
        pc.LearnedSkills[SkillIds.SA_ADVANCEDBOOK] = 5;
        Assert.Equal(3, StatusCalcService.ComputeSkillAspdVal(pc, WT.Book)); // (5-1)/2+1
        Assert.Equal(0, StatusCalcService.ComputeSkillAspdVal(pc, WT.Staff));
    }

    [Fact]
    public void Riding_penalty_offset_by_cavalier_mastery()
    {
        var pc = NewPc();
        pc.Option |= PlayerOption.Riding;
        Assert.Equal(-50, StatusCalcService.ComputeSkillAspdVal(pc, WT.OneHandSpear));

        pc.LearnedSkills[SkillIds.KN_CAVALIERMASTERY] = 5; // −50 + 10*5 = 0
        Assert.Equal(0, StatusCalcService.ComputeSkillAspdVal(pc, WT.OneHandSpear));
    }

    [Fact]
    public void Dragon_riding_penalty_offset_by_dragon_training()
    {
        var pc = NewPc();
        pc.Option |= PlayerOption.Dragon1;
        Assert.Equal(-25, StatusCalcService.ComputeSkillAspdVal(pc, WT.OneHandSword));

        pc.LearnedSkills[SkillIds.RK_DRAGONTRAINING] = 5; // −25 + 5*5 = 0
        Assert.Equal(0, StatusCalcService.ComputeSkillAspdVal(pc, WT.OneHandSword));
    }

    [Fact]
    public void SgDevil_adds_val_only_for_a_star_emperor()
    {
        var pc = NewPc();
        pc.LearnedSkills[SkillIds.SG_DEVIL] = 3;
        // Plain Novice → gated off.
        Assert.Equal(0, StatusCalcService.ComputeSkillAspdVal(pc, WT.Fist));
        // Star Emperor (Taekwon 3rd-class) → +1 + lv.
        pc.ClassMask = MapidClass.Taekwon | MapidClass.ThirdClass;
        Assert.Equal(4, StatusCalcService.ComputeSkillAspdVal(pc, WT.Fist));
    }

    // ---- FREECAST (RenewalPcAmotion) ----

    [Fact]
    public void Freecast_slows_attacks_while_casting_below_max_level()
    {
        var baseline = StatusCalcService.RenewalPcAmotion(40, 50, 100, WT.Staff, 0, 0);
        var lv5 = StatusCalcService.RenewalPcAmotion(40, 50, 100, WT.Staff, 0, 0, freecastLv: 5);
        var lv10 = StatusCalcService.RenewalPcAmotion(40, 50, 100, WT.Staff, 0, 0, freecastLv: 10);
        Assert.True(lv5 > baseline);     // 75% ASPD → slower while casting
        Assert.Equal(baseline, lv10);    // lv10 = 100% ASPD → unchanged
    }

    [Fact]
    public void SkillVal_speeds_up_attacks()
    {
        var baseline = StatusCalcService.RenewalPcAmotion(40, 50, 100, WT.Revolver, 0, 0);
        var withVal = StatusCalcService.RenewalPcAmotion(40, 50, 100, WT.Revolver, 0, 0, skillVal: 3);
        Assert.True(withVal < baseline);
    }

    // ---- exotic SC integration (CalcPc) ----

    [Fact]
    public void IncAspdRate_sc_speeds_up_attacks()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var unbuffed = pc.Stats.Amotion;

        sc.Start(pc, StatusType.Incaspdrate, val1: 10, 0, 0, 0, durationMs: 60_000);
        calc.CalcPc(pc, Inputs());
        Assert.True(pc.Stats.Amotion < unbuffed);
    }

    [Fact]
    public void SoulShadow_sc_speeds_up_attacks()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var unbuffed = pc.Stats.Amotion;

        // fixAspd += 10*val2 → flat amotion reduction.
        sc.Start(pc, StatusType.Soulshadow, val1: 1, val2: 5, 0, 0, durationMs: 60_000);
        calc.CalcPc(pc, Inputs());
        Assert.Equal(unbuffed - 50, pc.Stats.Amotion);
    }

    [Fact]
    public void OveredBoost_overrides_amotion_to_fixed_aspd()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        // val3 = 190 ASPD → amotion = 2000 - 1900 = 100.
        sc.Start(pc, StatusType.OveredBoost, val1: 1, val2: 0, val3: 190, val4: 0, durationMs: 60_000);
        calc.CalcPc(pc, Inputs());
        Assert.Equal(100, pc.Stats.Amotion);
    }

    // ---- helpers ----

    private static PlayerEntity NewPc() => new(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);

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
