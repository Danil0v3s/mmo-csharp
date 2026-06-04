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
    // COMBAT-89 — bespoke OnRecalc tail (clean single-derived-field batch). Buffs add, debuffs
    // subtract; both observable off the non-zero bases (Str/Agi/Dex/EquipDef) in Inputs().
    [InlineData(StatusType.Aurablade, CalcStatField.Batk, 5)]
    [InlineData(StatusType.Chattering, CalcStatField.Batk, 5)]
    [InlineData(StatusType.Bloodlust, CalcStatField.Batk, 5)]
    [InlineData(StatusType.Pyroclastic, CalcStatField.Batk, 5)]
    // SC-MAGNITUDE turn 8: ShieldspellAtk moved off Batk to Watk+Matk (val2=150), which this Batk-only
    // Read() infra can't observe — recalc survival is covered by SC02CalcFlagAllTests instead.
    [InlineData(StatusType.Volcano, CalcStatField.Batk, 5)]
    [InlineData(StatusType.Explosionspirits, CalcStatField.Cri, 5)]
    [InlineData(StatusType.Laudaramus, CalcStatField.Cri, 5)]
    [InlineData(StatusType.Grooming, CalcStatField.Flee, 5)]
    [InlineData(StatusType.Hallucinationwalk, CalcStatField.Flee, 5)]
    [InlineData(StatusType.NpcHallucinationwalk, CalcStatField.Flee, 5)]
    [InlineData(StatusType.OveredBoost, CalcStatField.Flee, 5)]
    [InlineData(StatusType.Violentgale, CalcStatField.Flee, 5)]
    [InlineData(StatusType.Zephyr, CalcStatField.Flee, 5)]
    [InlineData(StatusType.MercHitup, CalcStatField.Hit, 5)]
    [InlineData(StatusType.Prestige, CalcStatField.Def, 5)]
    // debuffs (subtract from the non-zero base)
    [InlineData(StatusType.Illusiondoping, CalcStatField.Hit, 5)]
    [InlineData(StatusType.Laziness, CalcStatField.Flee, 5)]
    [InlineData(StatusType.Paralysis, CalcStatField.Def2, 5)]
    [InlineData(StatusType.Stripshield, CalcStatField.Def, 5)]
    [InlineData(StatusType.Stripweapon, CalcStatField.Batk, 5)]
    [InlineData(StatusType.TinderBreaker, CalcStatField.Flee, 5)]
    [InlineData(StatusType.TinderBreaker2, CalcStatField.Flee, 5)]
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

    // COMBAT-73 — MaxHp SC mods survive a recalc via the OnRecalcPool pass (runs after the
    // MaxHp block), recomputed on the rebuilt pool, without clobbering current Hp.
    [Theory]
    [InlineData(StatusType.Epiclesis, 2)]    // Val2 = 5*Val1  → +10%
    [InlineData(StatusType.GtRevitalize, 5)] // Val2 = 2*Val1  → +10%
    [InlineData(StatusType.FirmFaith, 5)]    // Val2 = 2*Val1  → +10%
    [InlineData(StatusType.Lunarstance, 8)]  // Val2 = 2+Val1  → +10%
    [InlineData(StatusType.FriggSong, 2)]    // Val2 = 5*Val1  → +10%
    [InlineData(StatusType.Forceofvanguard, 1)] // Val2 = 8+12*Val1 = 20% → +20%
    // COMBAT-90 — MaxHp re-fold tail (% snapshot, % inverse-revert, and flat).
    [InlineData(StatusType.Appleidun, 5)]        // Val2 = 5+2*Val1 = 15% (no caster ctx) → +15%
    [InlineData(StatusType.CursedSoilOption, 1)] // Val2 = 10 (fixed) → +10%
    [InlineData(StatusType.PetrologyOption, 1)]  // Val2 = 5  (fixed) → +5%
    [InlineData(StatusType.UpheavalOption, 1)]   // Val2 = 15 (fixed) → +15%
    [InlineData(StatusType.Leradsdew, 5)]        // Val3 = 2+3*Val1+min(3*Val2,25) = 17% (inverse-revert)
    [InlineData(StatusType.Deluge, 5)]           // Val2 = delugeEff[4] = 15% (inverse-revert)
    [InlineData(StatusType.PromoteHealthReserch, 1)] // Val3 = 1000*tier-500+lv*10/3 (flat MaxHp)
    [InlineData(StatusType.Eqc, 5)]              // flat MaxHp += Val1 (Wave-58 active arm)
    [InlineData(StatusType.PowerOfGaia, 5)]      // flat MaxHp += Val1 (Wave-58 active arm)
    [InlineData(StatusType.SolidSkinOption, 5)]  // flat MaxHp += Val1 (Wave-58 active arm)
    public void MaxHp_buff_survives_recalc_and_does_not_clobber_hp(StatusType type, int val1)
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var baseMax = pc.Stats.MaxHp;
        pc.Stats.Hp = baseMax / 2;                 // mid-HP — must not be clobbered
        var curHp = pc.Stats.Hp;

        sc.Start(pc, type, val1: val1, 0, 0, 0, durationMs: 60_000);
        var afterStart = pc.Stats.MaxHp;
        Assert.True(afterStart > baseMax);          // the buff raised MaxHp
        Assert.Equal(curHp, pc.Stats.Hp);           // OnStart didn't touch current Hp

        calc.CalcPc(pc, Inputs());                  // a recalc would wipe MaxHp without OnRecalcPool
        Assert.Equal(afterStart, pc.Stats.MaxHp);   // re-folded onto the rebuilt pool
        Assert.Equal(curHp, pc.Stats.Hp);           // current Hp preserved (added headroom)

        calc.CalcPc(pc, Inputs());                  // idempotent
        Assert.Equal(afterStart, pc.Stats.MaxHp);
    }

    // COMBAT-73 + COMBAT-90 — MaxSp buffs re-fold via OnRecalcPool without clobbering current Sp.
    [Theory]
    [InlineData(StatusType.MercSpup, 5)]            // Val2 = 5*Val1 = 25% (COMBAT-73)
    [InlineData(StatusType.Service4u, 5)]           // Val2 = 9+Val1 = 14% (COMBAT-90)
    [InlineData(StatusType.EnergyDrinkReserch, 1)]  // Val3 = lv/10+5*tier-10 (COMBAT-90)
    public void MaxSp_buff_survives_recalc_without_clobbering_current_sp(StatusType type, int val1)
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var baseMax = pc.Stats.MaxSp;
        pc.Stats.Sp = baseMax / 2;
        var curSp = pc.Stats.Sp;

        sc.Start(pc, type, val1: val1, 0, 0, 0, durationMs: 60_000);
        var afterStart = pc.Stats.MaxSp;
        Assert.True(afterStart > baseMax);          // the buff raised MaxSp
        Assert.Equal(curSp, pc.Stats.Sp);           // OnStart didn't touch current Sp

        calc.CalcPc(pc, Inputs());
        Assert.Equal(afterStart, pc.Stats.MaxSp);   // re-folded onto the rebuilt pool
        Assert.Equal(curSp, pc.Stats.Sp);

        calc.CalcPc(pc, Inputs());
        Assert.Equal(afterStart, pc.Stats.MaxSp);   // idempotent
    }

    // COMBAT-90 — Melodyofsink is a MaxSp DEBUFF (-Val3%) that also drops INT (a primary, which
    // survives via the COMBAT-10 param-base delta). The pool reduction must re-apply each recalc;
    // without OnRecalcPool the rebuilt pool would equal the un-reduced base.
    [Fact]
    public void Melodyofsink_maxsp_debuff_survives_recalc()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var baseMax = pc.Stats.MaxSp;

        sc.Start(pc, StatusType.Melodyofsink, val1: 5, 0, 0, 0, durationMs: 60_000); // Val3 = 2+2*Val1 = 12%
        var afterStart = pc.Stats.MaxSp;
        Assert.True(afterStart < baseMax);          // the debuff lowered MaxSp

        calc.CalcPc(pc, Inputs());
        // The rebuilt pool from the (debuff-lowered) INT, BEFORE the -12% re-fold.
        var unreduced = Math.Max(1, 10 + 99) * (100 + pc.Stats.IntStat) / 100;
        Assert.True(pc.Stats.MaxSp < unreduced);    // OnRecalcPool re-applied the -12% (else == unreduced)
        var rebuilt = pc.Stats.MaxSp;

        calc.CalcPc(pc, Inputs());
        Assert.Equal(rebuilt, pc.Stats.MaxSp);      // idempotent
    }

    // COMBAT-90 — Berserk triples MaxHp (Val2 = MaxHp*2) and fills HP on cast. The ×3 must survive
    // a recalc; the one-time HP-fill is NOT re-applied (CalcPc only re-clamps Hp to the new max).
    [Fact]
    public void Berserk_triple_maxhp_survives_recalc()
    {
        var (calc, sc) = Build();
        var pc = NewPc();
        calc.CalcPc(pc, Inputs());
        var baseMax = pc.Stats.MaxHp;

        sc.Start(pc, StatusType.Berserk, val1: 10, 0, 0, 0, durationMs: 60_000);
        var afterStart = pc.Stats.MaxHp;
        Assert.Equal(baseMax * 3, afterStart);      // ×3 MaxHp
        Assert.Equal(afterStart, pc.Stats.Hp);      // filled to full on cast

        calc.CalcPc(pc, Inputs());
        Assert.Equal(afterStart, pc.Stats.MaxHp);   // ×3 re-folded onto the rebuilt pool
        Assert.True(pc.Stats.Hp <= pc.Stats.MaxHp); // Hp re-clamped, not corrupted

        calc.CalcPc(pc, Inputs());
        Assert.Equal(afterStart, pc.Stats.MaxHp);   // idempotent
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
        CalcStatField.Def2 => pc.Stats.Def2,
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
