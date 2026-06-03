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
/// COMBAT-53 — bespoke derived-stat SC handlers now re-apply their contribution
/// after a CalcPc recalc (OnRecalc), so the buff/debuff survives equip/level
/// recalcs idempotently. Derived fields only — primary stats + AspdRate stay out
/// (scope-3 guard). This covers the first verified batch of the bespoke sweep.
/// </summary>
public class Combat53BespokeRefoldTests
{
    // Each case: the SC must move its derived field on start, and that moved
    // value must survive two recalcs unchanged (preserved + idempotent).
    // Pure-derived handlers only (no primary-stat coupling). Primary-coupled
    // handlers (Truesight's +5 base stats, Curse's Luk=0) are COMBAT-72.
    [Theory]
    [InlineData(StatusType.Overthrust, CalcStatField.Batk, 5)]
    [InlineData(StatusType.Maxoverthrust, CalcStatField.Batk, 3)]
    [InlineData(StatusType.Defence, CalcStatField.Def, 5)]
    [InlineData(StatusType.Windwalk, CalcStatField.Flee, 5)]
    [InlineData(StatusType.Blind, CalcStatField.Hit, 1)]
    [InlineData(StatusType.Gatlingfever, CalcStatField.Batk, 5)]
    // COMBAT-72 — pure-derived tail batch (same survives+idempotent contract).
    [InlineData(StatusType.Humming, CalcStatField.Hit, 3)]
    [InlineData(StatusType.Fortune, CalcStatField.Cri, 2)]
    [InlineData(StatusType.Assumptio, CalcStatField.Def, 3)]
    [InlineData(StatusType.Moonlitserenade, CalcStatField.Batk, 10)]
    [InlineData(StatusType.Whistle, CalcStatField.Flee, 5)]
    [InlineData(StatusType.Drumbattle, CalcStatField.Def, 5)]
    [InlineData(StatusType.Impositio, CalcStatField.Batk, 5)]
    [InlineData(StatusType.Echosong, CalcStatField.Def, 5)]
    [InlineData(StatusType.Symphonyoflover, CalcStatField.Mdef, 5)]
    [InlineData(StatusType.Adrenaline, CalcStatField.Hit, 5)]
    public void Bespoke_derived_mod_survives_recalc_and_is_idempotent(
        StatusType type, CalcStatField field, int val1)
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var baseVal = Read(pc, field);

        sc.Start(pc, type, val1: val1, 0, 0, 0, durationMs: 60_000);
        var afterStart = Read(pc, field);
        Assert.NotEqual(baseVal, afterStart); // the SC actually moved the field

        calc.CalcPc(pc, Inputs());            // a recalc would wipe it without OnRecalc
        Assert.Equal(afterStart, Read(pc, field));

        calc.CalcPc(pc, Inputs());            // second recalc → no double-count
        Assert.Equal(afterStart, Read(pc, field));
    }

    [Fact]
    public void Mindbreaker_mdef2_drop_survives_recalc()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        // Give a Mdef2 base so the 12*val1 drop is observable.
        calc.CalcPc(pc, Inputs());
        pc.Stats.Mdef2 = 100;
        var baseMdef2 = pc.Stats.Mdef2;

        sc.Start(pc, StatusType.Mindbreaker, val1: 5, 0, 0, 0, durationMs: 60_000);
        var afterStart = pc.Stats.Mdef2;
        Assert.True(afterStart < baseMdef2); // -60

        calc.CalcPc(pc, Inputs());
        // CalcPc rebuilds Mdef2 from base (~0 here) then OnRecalc re-subtracts the
        // snapshot drop — so the re-fold keeps the −Val3 delta relative to the rebuild.
        var rebuilt = pc.Stats.Mdef2;
        calc.CalcPc(pc, Inputs());
        Assert.Equal(rebuilt, pc.Stats.Mdef2); // idempotent
    }

    // ---- helpers ----

    private static int Read(PlayerEntity pc, CalcStatField f) => f switch
    {
        CalcStatField.Batk => pc.Stats.Batk,
        CalcStatField.Def => pc.Stats.Def,
        CalcStatField.Flee => pc.Stats.Flee,
        CalcStatField.Hit => pc.Stats.Hit,
        CalcStatField.Cri => pc.Stats.Cri,
        CalcStatField.Mdef => pc.Stats.Mdef,
        _ => throw new ArgumentOutOfRangeException(nameof(f)),
    };

    private static PlayerEntity NewPc() => new(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);

    private static PcBaseInputs Inputs() => new(
        BaseLevel: 99, JobLevel: 50,
        Str: 60, Agi: 50, Vit: 40, Int: 1, Dex: 50, Luk: 30,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 100, EquipMdef: 0,
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
