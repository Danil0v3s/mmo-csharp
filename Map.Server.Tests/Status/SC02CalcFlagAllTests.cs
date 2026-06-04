using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-02 — the `CalcFlags: All` mis-mapped SCs (weapon endow / MATK% / resist /
/// random-ring) must apply their real rAthena effect, NOT a phantom +Val1 to all
/// six base stats. status.cpp refs in each test.
/// </summary>
public class SC02CalcFlagAllTests
{
    private static readonly StatusEffectRegistry _reg = new();

    private static MobEntity FreshMob()
    {
        var mob = new MobEntity(new EntityId(1), 1002, "Poring", mapId: 0, x: 0, y: 0);
        var s = mob.Stats;
        s.Str = s.Agi = s.Vit = s.IntStat = s.Dex = s.Luk = 50;
        s.MatkMin = 200; s.MatkMax = 240;
        s.WeaponElement = (byte)BattleElement.Neutral;
        s.MaxHp = 1000; s.Hp = 1000;
        return mob;
    }

    private static (short,short,short,short,short,short) Base(MobEntity m) =>
        (m.Stats.Str, m.Stats.Agi, m.Stats.Vit, m.Stats.IntStat, m.Stats.Dex, m.Stats.Luk);

    private static void Apply(StatusType t, StatusChange sc, MobEntity m)
        => _reg.Get(t)!.OnStart(m, sc, null);

    // ---- weapon endow: element override, no stat buff (status.cpp:8630) ----

    [Theory]
    [InlineData(StatusType.Fireweapon, BattleElement.Fire)]
    [InlineData(StatusType.Waterweapon, BattleElement.Water)]
    [InlineData(StatusType.Windweapon, BattleElement.Wind)]
    [InlineData(StatusType.Earthweapon, BattleElement.Earth)]
    public void Endow_setsWeaponElement_andLeavesStats(StatusType t, BattleElement ele)
    {
        var mob = FreshMob();
        var before = Base(mob);
        var sc = new StatusChange { Type = t, Val1 = 5 };
        Apply(t, sc, mob);
        Assert.Equal((byte)ele, mob.Stats.WeaponElement); // weapon now that element
        Assert.Equal(before, Base(mob));                  // NO phantom all-stat buff
    }

    [Fact]
    public void Endow_onEnd_restoresPreviousElement()
    {
        var mob = FreshMob();
        mob.Stats.WeaponElement = (byte)BattleElement.Holy; // e.g. an aspersio'd weapon
        var h = _reg.Get(StatusType.Fireweapon)!;
        var sc = new StatusChange { Type = StatusType.Fireweapon, Val1 = 5 };
        h.OnStart(mob, sc, null);
        Assert.Equal((byte)BattleElement.Fire, mob.Stats.WeaponElement);
        h.OnEnd(mob, sc);
        Assert.Equal((byte)BattleElement.Holy, mob.Stats.WeaponElement); // restored
    }

    // ---- MATK%: SP_MATK_RATE += val1 (status.cpp:4890) ----

    [Fact]
    public void Incmatkrate_raisesMatkByPercent_notBaseStats()
    {
        var mob = FreshMob();
        var before = Base(mob);
        var sc = new StatusChange { Type = StatusType.Incmatkrate, Val1 = 5 };
        Apply(StatusType.Incmatkrate, sc, mob);
        Assert.Equal(200 + 200 * 5 / 100, mob.Stats.MatkMin); // 210
        Assert.Equal(240 + 240 * 5 / 100, mob.Stats.MatkMax); // 252
        Assert.Equal(before, Base(mob));                       // base stats unchanged
        _reg.Get(StatusType.Incmatkrate)!.OnEnd(mob, sc);
        Assert.Equal(200, mob.Stats.MatkMin);                  // clean reversal
        Assert.Equal(240, mob.Stats.MatkMax);
    }

    // ---- Siegfried: Val2 = Val1*3 ele-resist, Val3 = Val1*5 status-resist
    //      (status.cpp:10728) ----

    [Fact]
    public void Siegfried_setsResistVals_notBaseStats()
    {
        var mob = FreshMob();
        var before = Base(mob);
        var sc = new StatusChange { Type = StatusType.Siegfried, Val1 = 5 };
        Apply(StatusType.Siegfried, sc, mob);
        Assert.Equal(15, sc.Val2); // 5*3 elemental resist
        Assert.Equal(25, sc.Val3); // 5*5 status-ailment resist
        Assert.Equal(before, Base(mob));
    }

    // ---- Nibelungen: Val2 = rnd() % RINGNBL_MAX (status.cpp:10725) ----

    [Fact]
    public void Nibelungen_rollsRandomRing_notBaseStats()
    {
        var mob = FreshMob();
        var before = Base(mob);
        var sc = new StatusChange { Type = StatusType.Nibelungen, Val1 = 5 };
        Apply(StatusType.Nibelungen, sc, mob);
        Assert.InRange(sc.Val2, 0, 8); // [0, RINGNBL_MAX=9)
        Assert.Equal(before, Base(mob));
    }

    [Fact]
    public void Nibelungen_doesNotRerollPresetVal2()
    {
        var mob = FreshMob();
        var sc = new StatusChange { Type = StatusType.Nibelungen, Val1 = 5, Val2 = 7 };
        Apply(StatusType.Nibelungen, sc, mob);
        Assert.Equal(7, sc.Val2); // deterministic caller-set ring preserved
    }

    // ---- Berserk: +200 flat Batk, MaxHp×3 (verify, not regressed) ----

    [Fact]
    public void Berserk_triplesMaxHp_andAddsFlatBatk()
    {
        var mob = FreshMob();
        mob.Stats.Batk = 200;
        var sc = new StatusChange { Type = StatusType.Berserk, Val1 = 1 };
        Apply(StatusType.Berserk, sc, mob);
        Assert.Equal(3000, mob.Stats.MaxHp);   // 1000 × 3
        Assert.Equal(400, mob.Stats.Batk);     // 200 + 200
    }

    // ---- SC-MAGNITUDE: SC_GUARD_STANCE (status.cpp:12445) — +DEF, −Watk (not +Val1 to both) ----

    [Fact]
    public void GuardStance_raisesDef_andLowersWatk_byTheRealFormula()
    {
        var mob = FreshMob();
        mob.Stats.Def = 100; mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340;
        var sc = new StatusChange { Type = StatusType.GuardStance, Val1 = 3 };

        Apply(StatusType.GuardStance, sc, mob);
        Assert.Equal(100 + (50 + 50 * 3), mob.Stats.Def);   // +Val2 = +200 → 300
        Assert.Equal(300 - 50 * 3, mob.Stats.WatkMin);      // −Val3 = −150 → 150
        Assert.Equal(340 - 50 * 3, mob.Stats.WatkMax);

        _reg.Get(StatusType.GuardStance)!.OnEnd!(mob, sc);
        Assert.Equal(100, mob.Stats.Def);                   // restored
        Assert.Equal(300, mob.Stats.WatkMin);
        Assert.Equal(340, mob.Stats.WatkMax);
    }

    [Fact]
    public void GuardStance_isConverted_notGeneratorDefault()
        => Assert.DoesNotContain(StatusType.GuardStance, _reg.GeneratedStatModDefaultTypes);

    // ---- SC-MAGNITUDE: SC_HISS (status.cpp:12301) — flat +50 Flee2, NOT +Val1 ----

    [Fact]
    public void Hiss_addsFlat50PerfectDodge()
    {
        var mob = FreshMob();
        mob.Stats.Flee2 = 30;
        var sc = new StatusChange { Type = StatusType.Hiss, Val1 = 3 };

        Apply(StatusType.Hiss, sc, mob);
        Assert.Equal(80, mob.Stats.Flee2); // 30 + flat 50 (NOT +Val1=3)

        _reg.Get(StatusType.Hiss)!.OnEnd!(mob, sc);
        Assert.Equal(30, mob.Stats.Flee2);
    }

    [Fact]
    public void Hiss_isConverted_notGeneratorDefault()
        => Assert.DoesNotContain(StatusType.Hiss, _reg.GeneratedStatModDefaultTypes);

    // ---- SC-MAGNITUDE: SC_GN_CARTBOOST — Val2 by level for the speed calc (effect in ComputeScSpeed) ----

    [Fact]
    public void GnCartboost_setsVal2ByLevel_forTheSpeedCalc()
    {
        // status.cpp:11939 — Val2 = 50 (<3) / 75 (3-4) / 100 (≥5). ComputeScSpeed reads Val2; the +Val1
        // generator default never set Val2 (so the speed-up read 0). This OnStart fixes that.
        var sc = new StatusChange { Type = StatusType.GnCartboost, Val1 = 3 };
        Apply(StatusType.GnCartboost, sc, FreshMob());
        Assert.Equal(75, sc.Val2);
    }

    [Fact]
    public void GnCartboost_isConverted_notGeneratorDefault()
        => Assert.DoesNotContain(StatusType.GnCartboost, _reg.GeneratedStatModDefaultTypes);

    // ---- SC-MAGNITUDE: the SC_MERC_* cluster — Val2 formulas, real per-stat targets ----

    [Fact]
    public void MercAtkup_addsWatk_byVal2_15xVal1_notBatk()
    {
        // status.cpp:7119 — watk += val2; val2 = 15*val1. (Generator wrongly used Batk.)
        var mob = FreshMob();
        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340; mob.Stats.Batk = 100;
        var sc = new StatusChange { Type = StatusType.MercAtkup, Val1 = 4 };

        Apply(StatusType.MercAtkup, sc, mob);
        Assert.Equal(60, sc.Val2);                  // 15*4
        Assert.Equal(360, mob.Stats.WatkMin);       // +60
        Assert.Equal(400, mob.Stats.WatkMax);
        Assert.Equal(100, mob.Stats.Batk);          // Batk untouched

        _reg.Get(StatusType.MercAtkup)!.OnEnd!(mob, sc);
        Assert.Equal(300, mob.Stats.WatkMin);       // restored
        Assert.Equal(340, mob.Stats.WatkMax);
    }

    [Fact]
    public void MercFleeup_addsFlee_byVal2_15xVal1()
    {
        var mob = FreshMob();
        mob.Stats.Flee = 90;
        var sc = new StatusChange { Type = StatusType.MercFleeup, Val1 = 5 };

        Apply(StatusType.MercFleeup, sc, mob);
        Assert.Equal(75, sc.Val2);                  // 15*5
        Assert.Equal(165, mob.Stats.Flee);

        _reg.Get(StatusType.MercFleeup)!.OnEnd!(mob, sc);
        Assert.Equal(90, mob.Stats.Flee);
    }

    [Fact]
    public void MercHpup_raisesMaxHpByPercent_andHealsFull()
    {
        // status.cpp:3160 maxhp bonus += val2 (=5*val1, a %); :12910 status_percent_heal(bl,100,0).
        var mob = FreshMob();
        mob.Stats.MaxHp = 1000; mob.Stats.Hp = 400;
        var sc = new StatusChange { Type = StatusType.MercHpup, Val1 = 5 };

        Apply(StatusType.MercHpup, sc, mob);
        Assert.Equal(25, sc.Val2);                  // 5*5 %
        Assert.Equal(1250, mob.Stats.MaxHp);        // +25% of 1000
        Assert.Equal(1250, mob.Stats.Hp);           // healed full

        _reg.Get(StatusType.MercHpup)!.OnEnd!(mob, sc);
        Assert.Equal(1000, mob.Stats.MaxHp);        // pool restored
        Assert.Equal(1000, mob.Stats.Hp);           // Hp clamped down to new max
    }

    [Theory]
    // ATKUP/FLEEUP/HPUP converted this turn; HITUP/SPUP already converted by COMBAT-73/89 — none of the
    // SC_MERC_* stat-bonus cluster should remain on the generator-default worklist.
    [InlineData(StatusType.MercAtkup)]
    [InlineData(StatusType.MercHitup)]
    [InlineData(StatusType.MercFleeup)]
    [InlineData(StatusType.MercHpup)]
    [InlineData(StatusType.MercSpup)]
    public void MercCluster_isConverted_notGeneratorDefault(StatusType t)
        => Assert.DoesNotContain(t, _reg.GeneratedStatModDefaultTypes);

    // ---- the reclassified SCs are no longer in the CalcFlag generator table ----

    [Theory]
    [InlineData(StatusType.Fireweapon)]
    [InlineData(StatusType.Waterweapon)]
    [InlineData(StatusType.Windweapon)]
    [InlineData(StatusType.Earthweapon)]
    [InlineData(StatusType.Siegfried)]
    [InlineData(StatusType.Nibelungen)]
    [InlineData(StatusType.Incmatkrate)]
    public void Reclassified_SCs_haveNoCalcFlagMapping(StatusType t)
        => Assert.Empty(StatusCalcFlagDefaults.For(t));
}
