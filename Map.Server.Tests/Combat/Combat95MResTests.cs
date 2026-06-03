using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-95 — the MRes trait-stat magic reduction (battle.cpp:9278, renewal) mirrors the physical
/// Res curve and is applied BEFORE MDEF: <c>damage * (5000+mres) / (5000+10*mres)</c>, with the
/// effective MRes first lowered by the attacker's ignore % — bonus2 bIgnoreMResRace[race] + [RC_ALL]
/// + SC_A_VITA.val2, clamped to max_res_mres_ignored (50).
///
/// Base magic damage is pinned to 100 (MatkMin/Max 100, rate 100, MDEF 0). Reference values:
///   mres 100, ignore 0  → 100 * (5000+100)/(5000+1000)   = 100 * 5100/6000 = 85
///   mres 100, ignore 50 → mres 50 → 100 * (5000+50)/(5000+500) = 100 * 5050/5500 = 91
/// </summary>
public class Combat95MResTests
{
    private const long Base = 100, NoIgnore = 85, Ignore50 = 91;
    private static readonly BattleRace TargetRace = BattleRace.Brute; // index 2

    [Fact]
    public void Baseline_no_ignore_uses_raw_mres()
    {
        Assert.Equal(NoIgnore, Cast(mres: 100));
        Assert.Equal(Base, Cast(mres: 0)); // no MRes → no reduction
    }

    [Fact]
    public void IgnoreMResRace_by_target_race_lowers_effective_mres()
    {
        var pc = MakeCaster();
        pc.EquipBonuses.IgnoreMResRace[(int)TargetRace] = 50;
        Assert.Equal(Ignore50, CastWith(pc, mres: 100));
    }

    [Fact]
    public void IgnoreMResRace_RC_ALL_applies_regardless_of_race()
    {
        var pc = MakeCaster();
        pc.EquipBonuses.IgnoreMResRace[(int)BattleRace.All] = 50;
        Assert.Equal(Ignore50, CastWith(pc, mres: 100));
    }

    [Fact]
    public void Sc_a_vita_pierces_mres()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = MakeCaster();
        sc.Start(pc, StatusType.AVita, val1: 10, val2: 50, val3: 0, val4: 0, durationMs: 60_000, pc);
        Assert.Equal(Ignore50, CastWith(pc, mres: 100, sc: sc));
    }

    [Fact]
    public void Race_rc_all_and_sc_sum_before_the_clamp()
    {
        // 20 (race) + 10 (RC_ALL) + 20 (a_vita) = 50 → effective mres 50 → 91.
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = MakeCaster();
        pc.EquipBonuses.IgnoreMResRace[(int)TargetRace] = 20;
        pc.EquipBonuses.IgnoreMResRace[(int)BattleRace.All] = 10;
        sc.Start(pc, StatusType.AVita, val1: 4, val2: 20, val3: 0, val4: 0, durationMs: 60_000, pc);
        Assert.Equal(Ignore50, CastWith(pc, mres: 100, sc: sc));
    }

    [Fact]
    public void Ignore_is_clamped_to_max_res_mres_ignored()
    {
        // race 50 + RC_ALL 50 = 100; unclamped → mres 0 → 100 dmg. Clamped to 50 → 91.
        var pc = MakeCaster();
        pc.EquipBonuses.IgnoreMResRace[(int)TargetRace] = 50;
        pc.EquipBonuses.IgnoreMResRace[(int)BattleRace.All] = 50;
        Assert.Equal(Ignore50, CastWith(pc, mres: 100));
    }

    [Fact]
    public void Sc_a_vita_registration_materializes_val2_as_five_times_val1()
    {
        // COMBAT-95 — the registry sets Val2 = 5*Val1 (Res/MRes pierce %) so the combat reader works.
        var reg = new StatusEffectRegistry();
        var handler = reg.Get(StatusType.AVita);
        Assert.NotNull(handler);
        var entry = new StatusChange { Type = StatusType.AVita, Val1 = 10 };
        handler!.OnStart(MakeCaster(), entry, null);
        Assert.Equal(50, entry.Val2);
    }

    // ---- helpers ----

    private static long Cast(short mres) => CastWith(MakeCaster(), mres);

    private static long CastWith(PlayerEntity pc, short mres, IStatusChangeService? sc = null)
    {
        var target = MakeTarget();
        target.Stats.Mres = mres;
        var calc = new BattleCalculator(rng: new ZeroRandom(), sc: sc);
        return calc.CalcMagicAttack(pc, target, skillId: 0, skillLevel: 1, ratePerLevel: 100).Damage;
    }

    private static PlayerEntity MakeCaster()
    {
        var pc = new PlayerEntity(1, 1, "Mage", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.MatkMin = 100; pc.Stats.MatkMax = 100;
        pc.Stats.WeaponElement = 0; // Neutral → element table 100% vs Neutral target
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Mdef = 0; m.Stats.Mdef2 = 0; m.Stats.Mres = 0;
        m.Stats.Race = TargetRace;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium;
        return m;
    }

    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
