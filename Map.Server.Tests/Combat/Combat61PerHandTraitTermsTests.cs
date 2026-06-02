using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-61 — the per-hand renewal trait-stat terms that rAthena applies in
/// <c>battle_calc_weapon_attack</c> (battle.cpp:7635): the <c>patk</c> %
/// (7775, before the mastery add), the <c>crit_atk_rate</c> bump on a critical
/// (7787, normal-attack /100 branch), and the <c>(5000+res)/(5000+10*res)</c>
/// physical reduction (7845). Each is verified per hand against hand-computed
/// rAthena reference values.
///
/// Fixtures are built so the base per-hand damage is a clean 100: bare weapon
/// roll pinned to 100 (WatkMin==WatkMax, Dex==atkMax), Batk 0, neutral element,
/// 0 DEF, no cards, no SC — so each trait term's exact integer arithmetic is
/// isolated.
/// </summary>
public class Combat61PerHandTraitTermsTests
{
    // ---- patk (battle.cpp:7775) ----

    [Fact]
    public void Patk_scales_the_hand_before_mastery()
    {
        // baseline (patk 0) = 100; patk 50 → 100 * (100+50)/100 = 150.
        long baseline = SwingDamage(patk: 0);
        long withPatk = SwingDamage(patk: 50);

        Assert.Equal(100, baseline);
        Assert.Equal(150, withPatk);
    }

    // ---- res reduction (battle.cpp:7845) ----

    [Fact]
    public void Res_reduces_the_hand_by_the_renewal_curve()
    {
        // res 100 → 100 * (5000+100)/(5000+1000) = 100 * 5100/6000 = 85.
        long baseline = SwingDamage(targetRes: 0);
        long withRes = SwingDamage(targetRes: 100);

        Assert.Equal(100, baseline);
        Assert.Equal(85, withRes);
    }

    [Fact]
    public void Patk_applies_before_res_in_rathena_order()
    {
        // patk first: 100 * 150/100 = 150; then res: 150 * 5100/6000 = 127 (trunc).
        long combined = SwingDamage(patk: 50, targetRes: 100);
        Assert.Equal(127, combined);
    }

    // ---- crit_atk_rate (battle.cpp:7787, normal-attack /100 branch) ----

    [Fact]
    public void CritAtkRate_bumps_a_critical_hand_by_percent_over_100()
    {
        // critical base = atkMax(100) * 14/10 = 140 (no patk/res).
        // crit_atk_rate 50 → 140 + 140*50/100 = 210.
        long critNoBonus = CritSwingDamage(critAtkRate: 0);
        long critWithBonus = CritSwingDamage(critAtkRate: 50);

        Assert.Equal(140, critNoBonus);
        Assert.Equal(210, critWithBonus);
    }

    [Fact]
    public void CritAtkRate_is_ignored_on_a_non_critical_swing()
    {
        // Non-crit swing: crit_atk_rate must not fire — stays at the 100 baseline.
        long nonCrit = SwingDamage(patk: 0, critAtkRate: 50);
        Assert.Equal(100, nonCrit);
    }

    // ---- dual-wield: both hands receive patk + res (Done criterion) ----

    [Fact]
    public void DualWield_applies_patk_and_res_to_both_hands()
    {
        var pc = MakeSwinger();
        pc.Stats.Patk = 50;
        // off-hand weapon present → is_attack_left_handed true.
        pc.WeaponType = Map.Server.Inventory.WeaponTypeCodes.Dagger;
        pc.Stats.WeaponLevel = 1;
        pc.Stats.LeftWatkMin = 100; pc.Stats.LeftWatkMax = 100;
        pc.Stats.LeftWeaponLevel = 1;
        pc.Stats.LeftWeaponType = Map.Server.Inventory.WeaponTypeCodes.Dagger;
        pc.Stats.LeftWeaponElement = 0;

        var target = MakeTarget();
        target.Stats.Res = 100;

        var calc = new BattleCalculator(rng: new ZeroRandom());
        var wd = calc.CalcWeaponAttack(pc, target);

        // Each hand: 100 → patk 150 → res 150*5100/6000 = 127. A plain
        // (non-thief/kagerou) dual-wield leaves the left/right split as identity.
        Assert.Equal(127, wd.Damage);
        Assert.Equal(127, wd.Damage2);
    }

    // ---- helpers ----

    private static long SwingDamage(short patk = 0, short targetRes = 0, int critAtkRate = 0)
    {
        var pc = MakeSwinger();
        pc.Stats.Patk = patk;
        if (critAtkRate != 0) pc.EquipBonuses.CritAtkRate = critAtkRate;
        var target = MakeTarget();
        target.Stats.Res = targetRes;
        var calc = new BattleCalculator(rng: new ZeroRandom());
        return calc.CalcWeaponAttack(pc, target).Damage;
    }

    private static long CritSwingDamage(int critAtkRate)
    {
        var pc = MakeSwinger();
        pc.Stats.Cri = 1000; // guaranteed crit vs luk 0 with the zero roll
        if (critAtkRate != 0) pc.EquipBonuses.CritAtkRate = critAtkRate;
        var calc = new BattleCalculator(rng: new ZeroRandom());
        return calc.CalcWeaponAttack(pc, MakeTarget()).Damage;
    }

    private static PlayerEntity MakeSwinger()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { WeaponType = 0 };
        pc.Stats.WeaponLevel = 0;          // bare-handed → atkMin = Dex
        pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100; // pinned roll
        pc.Stats.Dex = 100;                // atkMin = 100 = atkMax → deterministic
        pc.Stats.Batk = 0; pc.Stats.Cri = 0; pc.Stats.Hit = 10000;
        pc.Stats.Patk = 0;
        pc.Stats.WeaponElement = 0;        // neutral
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Res = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }

    /// <summary>Deterministic: every roll returns its minimum (0 / minValue).</summary>
    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
