using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-77 — the attacker's Res-ignore sources lower the target's EFFECTIVE Res before the
/// renewal physical reduction curve (battle.cpp:7820-7846): bonus2 bIgnoreResRace[race] +
/// [RC_ALL] + SC_A_TELUM.val2 + SC_POTENT_VENOM.val2, clamped to max_res_mres_ignored (50).
///
/// Fixtures pin the base per-hand damage to 100 (same as COMBAT-61). Reference values:
///   res 100, ignore 0  → 100 * (5000+100)/(5000+1000)   = 100 * 5100/6000 = 85
///   res 100, ignore 50 → res 50 → 100 * (5000+50)/(5000+500) = 100 * 5050/5500 = 91
/// </summary>
public class Combat77ResIgnoreTests
{
    private const long Base = 100, NoIgnore = 85, Ignore50 = 91;
    private static readonly BattleRace TargetRace = BattleRace.Brute; // index 2

    [Fact]
    public void Baseline_no_ignore_uses_raw_res()
    {
        Assert.Equal(NoIgnore, Swing(res: 100));
        Assert.Equal(Base, Swing(res: 0));
    }

    [Fact]
    public void IgnoreResRace_by_target_race_lowers_effective_res()
    {
        var pc = MakeSwinger();
        pc.EquipBonuses.IgnoreResRace[(int)TargetRace] = 50;
        Assert.Equal(Ignore50, SwingWith(pc, res: 100));
    }

    [Fact]
    public void IgnoreResRace_RC_ALL_applies_regardless_of_race()
    {
        var pc = MakeSwinger();
        pc.EquipBonuses.IgnoreResRace[(int)BattleRace.All] = 50;
        Assert.Equal(Ignore50, SwingWith(pc, res: 100));
    }

    [Fact]
    public void Sc_a_telum_pierces_res()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = MakeSwinger();
        sc.Start(pc, StatusType.ATelum, val1: 10, val2: 50, val3: 0, val4: 0, durationMs: 60_000, pc);
        Assert.Equal(Ignore50, SwingWith(pc, res: 100, sc: sc));
    }

    [Fact]
    public void Sc_potent_venom_pierces_res()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = MakeSwinger();
        sc.Start(pc, StatusType.PotentVenom, val1: 25, val2: 50, val3: 0, val4: 0, durationMs: 60_000, pc);
        Assert.Equal(Ignore50, SwingWith(pc, res: 100, sc: sc));
    }

    [Fact]
    public void Race_rc_all_and_sc_sum_before_the_clamp()
    {
        // 20 (race) + 10 (RC_ALL) + 20 (telum) = 50 → effective res 50 → 91.
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = MakeSwinger();
        pc.EquipBonuses.IgnoreResRace[(int)TargetRace] = 20;
        pc.EquipBonuses.IgnoreResRace[(int)BattleRace.All] = 10;
        sc.Start(pc, StatusType.ATelum, val1: 4, val2: 20, val3: 0, val4: 0, durationMs: 60_000, pc);
        Assert.Equal(Ignore50, SwingWith(pc, res: 100, sc: sc));
    }

    [Fact]
    public void Ignore_is_clamped_to_max_res_mres_ignored()
    {
        // race 50 + RC_ALL 50 = 100; unclamped → res 0 → 100 dmg. Clamped to 50 → 91.
        var pc = MakeSwinger();
        pc.EquipBonuses.IgnoreResRace[(int)TargetRace] = 50;
        pc.EquipBonuses.IgnoreResRace[(int)BattleRace.All] = 50;
        Assert.Equal(Ignore50, SwingWith(pc, res: 100));
    }

    // ---- helpers ----

    private static long Swing(short res)
        => SwingWith(MakeSwinger(), res);

    private static long SwingWith(PlayerEntity pc, short res, IStatusChangeService? sc = null)
    {
        var target = MakeTarget();
        target.Stats.Res = res;
        var calc = new BattleCalculator(rng: new ZeroRandom(), sc: sc);
        return calc.CalcWeaponAttack(pc, target).Damage;
    }

    private static PlayerEntity MakeSwinger()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { WeaponType = 0 };
        pc.Stats.WeaponLevel = 0;
        pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100;
        pc.Stats.Dex = 100;
        pc.Stats.Batk = 0; pc.Stats.Cri = 0; pc.Stats.Hit = 10000;
        pc.Stats.Patk = 0;
        pc.Stats.WeaponElement = 0;
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Res = 0;
        m.Stats.Race = TargetRace;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }

    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
