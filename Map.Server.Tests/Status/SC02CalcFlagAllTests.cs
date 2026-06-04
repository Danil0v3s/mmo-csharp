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

    // ---- SC-MAGNITUDE: SC_DORAM_MATK (status.cpp:7215) — matk += Val1, NOT Batk += Val1 ----

    [Fact]
    public void DoramMatk_addsFlatMatk_byVal1_notBatk()
    {
        var mob = FreshMob();           // MatkMin=200, MatkMax=240
        mob.Stats.Batk = 100;
        var sc = new StatusChange { Type = StatusType.DoramMatk, Val1 = 99 }; // Val1 = caster base_level

        Apply(StatusType.DoramMatk, sc, mob);
        Assert.Equal(299, mob.Stats.MatkMin);   // +99
        Assert.Equal(339, mob.Stats.MatkMax);
        Assert.Equal(100, mob.Stats.Batk);      // Batk untouched (generator had wrongly used it)

        _reg.Get(StatusType.DoramMatk)!.OnEnd!(mob, sc);
        Assert.Equal(200, mob.Stats.MatkMin);   // restored
        Assert.Equal(240, mob.Stats.MatkMax);
    }

    [Fact]
    public void DoramMatk_isConverted_notGeneratorDefault()
        => Assert.DoesNotContain(StatusType.DoramMatk, _reg.GeneratedStatModDefaultTypes);

    // ---- SC-MAGNITUDE: MATK SCs previously mis-applied to Batk (physical) — fixed to Matk ----

    [Fact]
    public void Izayoi_addsMatk_by25xVal1_notBatk()
    {
        // status.cpp:7237 — matk += 25*val1. Was wrongly +Val1 to Batk (did nothing for magic).
        var mob = FreshMob();           // MatkMin=200, MatkMax=240
        mob.Stats.Batk = 100;
        var sc = new StatusChange { Type = StatusType.Izayoi, Val1 = 3 };

        Apply(StatusType.Izayoi, sc, mob);
        Assert.Equal(75, sc.Val2);              // 25*3
        Assert.Equal(275, mob.Stats.MatkMin);   // +75
        Assert.Equal(315, mob.Stats.MatkMax);
        Assert.Equal(100, mob.Stats.Batk);      // physical untouched

        _reg.Get(StatusType.Izayoi)!.OnEnd!(mob, sc);
        Assert.Equal(200, mob.Stats.MatkMin);
        Assert.Equal(240, mob.Stats.MatkMax);
    }

    [Fact]
    public void Soulfairy_addsMatk_byVal2_10xVal1_notBatk()
    {
        // status.cpp:7223 — matk += val2; val2 = 10*val1. Was wrongly +Val1 to Batk.
        var mob = FreshMob();
        mob.Stats.Batk = 100;
        var sc = new StatusChange { Type = StatusType.Soulfairy, Val1 = 5 };

        Apply(StatusType.Soulfairy, sc, mob);
        Assert.Equal(50, sc.Val2);              // 10*5
        Assert.Equal(250, mob.Stats.MatkMin);   // +50
        Assert.Equal(290, mob.Stats.MatkMax);
        Assert.Equal(100, mob.Stats.Batk);      // physical untouched

        _reg.Get(StatusType.Soulfairy)!.OnEnd!(mob, sc);
        Assert.Equal(200, mob.Stats.MatkMin);
        Assert.Equal(240, mob.Stats.MatkMax);
    }

    // ---- SC-MAGNITUDE: SC_SHIELDSPELL_ATK — Val2 = 150 flat to BOTH Watk and Matk ----

    [Fact]
    public void ShieldspellAtk_addsFlat150_toWatkAndMatk_notBatk()
    {
        // status.cpp:7139 watk += val2, :7227 matk += val2, start arm val2 = 150 (skill level was the
        // wrong source). Was +Val1 (=3) to Batk only.
        var mob = FreshMob();
        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340; mob.Stats.Batk = 100;
        var sc = new StatusChange { Type = StatusType.ShieldspellAtk, Val1 = 3 };

        Apply(StatusType.ShieldspellAtk, sc, mob);
        Assert.Equal(150, sc.Val2);
        Assert.Equal(450, mob.Stats.WatkMin);   // +150
        Assert.Equal(490, mob.Stats.WatkMax);
        Assert.Equal(350, mob.Stats.MatkMin);   // +150
        Assert.Equal(390, mob.Stats.MatkMax);
        Assert.Equal(100, mob.Stats.Batk);      // base ATK untouched

        // OnRecalc must re-apply both Watk and Matk (CalcPc rebuilds both from base each recalc).
        Apply(StatusType.ShieldspellAtk, sc, mob);     // re-start for the recalc check
        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340; mob.Stats.MatkMin = 200; mob.Stats.MatkMax = 240;
        _reg.Get(StatusType.ShieldspellAtk)!.OnRecalc!(mob, sc);
        Assert.Equal(450, mob.Stats.WatkMin);
        Assert.Equal(350, mob.Stats.MatkMin);

        _reg.Get(StatusType.ShieldspellAtk)!.OnEnd!(mob, sc);
        Assert.Equal(300, mob.Stats.WatkMin);
        Assert.Equal(200, mob.Stats.MatkMin);
    }

    // ---- SC-MAGNITUDE: flat-Matk handlers must survive a CalcPc recalc (re-apply via OnRecalc) ----

    // ---- SC-MAGNITUDE: generator-default primary-stat fixes (wrong sign / wrong magnitude) ----

    [Fact]
    public void Battleorders_addsFlat5_toStrIntDex_only()
    {
        // status.cpp:6530 str += 5 (+ Int/Dex); status.yml CalcFlags Str/Int/Dex. NOT +Val1.
        var mob = FreshMob();   // all base stats = 50
        var sc = new StatusChange { Type = StatusType.Battleorders, Val1 = 1 };
        Apply(StatusType.Battleorders, sc, mob);
        Assert.Equal(55, mob.Stats.Str);
        Assert.Equal(55, mob.Stats.IntStat);
        Assert.Equal(55, mob.Stats.Dex);
        Assert.Equal(50, mob.Stats.Agi);   // untouched
        Assert.Equal(50, mob.Stats.Vit);
        Assert.Equal(50, mob.Stats.Luk);

        _reg.Get(StatusType.Battleorders)!.OnEnd!(mob, sc);
        Assert.Equal(50, mob.Stats.Str);
    }

    [Fact]
    public void AllStatDown_subtractsVal2_fromAllSix_notAdds()
    {
        // status.cpp: all six base stats −= val2; val2 = 20*Val1 (−10 if Val1 < max 5). A DEBUFF.
        var mob = FreshMob();   // all = 50
        var sc = new StatusChange { Type = StatusType.AllStatDown, Val1 = 2 };
        Apply(StatusType.AllStatDown, sc, mob);
        Assert.Equal(30, sc.Val2);          // 20*2 − 10
        Assert.Equal(20, mob.Stats.Str);    // 50 − 30 (reduced, NOT +Val1)
        Assert.Equal(20, mob.Stats.Agi);
        Assert.Equal(20, mob.Stats.Luk);

        _reg.Get(StatusType.AllStatDown)!.OnEnd!(mob, sc);
        Assert.Equal(50, mob.Stats.Str);    // restored
    }

    [Fact]
    public void Stomachache_subtractsVal1_fromAllSix_notAdds()
    {
        // status.cpp:6561-6907 — all six base stats −= val1 (food-poison debuff). Generator added +Val1.
        var mob = FreshMob();   // all = 50
        var sc = new StatusChange { Type = StatusType.Stomachache, Val1 = 7 };
        Apply(StatusType.Stomachache, sc, mob);
        Assert.Equal(43, mob.Stats.Str);    // 50 − 7 (reduced)
        Assert.Equal(43, mob.Stats.Agi);
        Assert.Equal(43, mob.Stats.Luk);

        _reg.Get(StatusType.Stomachache)!.OnEnd!(mob, sc);
        Assert.Equal(50, mob.Stats.Str);
    }

    [Fact]
    public void Cheerup_addsFlat3_toAllSixStats()
    {
        var mob = FreshMob();   // all = 50
        var sc = new StatusChange { Type = StatusType.Cheerup, Val1 = 1 };
        Apply(StatusType.Cheerup, sc, mob);
        Assert.Equal(53, mob.Stats.Str);
        Assert.Equal(53, mob.Stats.Vit);
        Assert.Equal(53, mob.Stats.Luk);
        _reg.Get(StatusType.Cheerup)!.OnEnd!(mob, sc);
        Assert.Equal(50, mob.Stats.Str);
    }

    [Fact]
    public void BananaBomb_subtracts75Luk_andRestoresExactly()
    {
        var mob = FreshMob();
        mob.Stats.Luk = 100;
        var sc = new StatusChange { Type = StatusType.BananaBomb, Val1 = 1 };
        Apply(StatusType.BananaBomb, sc, mob);
        Assert.Equal(25, mob.Stats.Luk);            // 100 − 75
        _reg.Get(StatusType.BananaBomb)!.OnEnd!(mob, sc);
        Assert.Equal(100, mob.Stats.Luk);

        // Luk < 75: the snapshot makes the restore exact (no over-restore from the 0-clamp).
        mob.Stats.Luk = 40;
        Apply(StatusType.BananaBomb, sc, mob);
        Assert.Equal(0, mob.Stats.Luk);             // clamped
        _reg.Get(StatusType.BananaBomb)!.OnEnd!(mob, sc);
        Assert.Equal(40, mob.Stats.Luk);            // exact restore via Val2 snapshot
    }

    [Fact]
    public void Swordclan_adds1StrVit_and30Hp10Sp_flat()
    {
        var mob = FreshMob();   // Str=Vit=50, MaxHp=1000
        mob.Stats.MaxSp = 200; mob.Stats.Sp = 200;
        var sc = new StatusChange { Type = StatusType.Swordclan, Val1 = 1 };
        Apply(StatusType.Swordclan, sc, mob);
        Assert.Equal(51, mob.Stats.Str);
        Assert.Equal(51, mob.Stats.Vit);
        Assert.Equal(1030, mob.Stats.MaxHp);   // +30 flat (NOT +Val1)
        Assert.Equal(210, mob.Stats.MaxSp);    // +10 flat

        // OnRecalcPool re-applies the flat pool adds after CalcPc rebuilds.
        mob.Stats.MaxHp = 1000; mob.Stats.MaxSp = 200;
        _reg.Get(StatusType.Swordclan)!.OnRecalcPool!(mob, sc);
        Assert.Equal(1030, mob.Stats.MaxHp);
        Assert.Equal(210, mob.Stats.MaxSp);

        _reg.Get(StatusType.Swordclan)!.OnEnd!(mob, sc);
        Assert.Equal(50, mob.Stats.Str);
        Assert.Equal(1000, mob.Stats.MaxHp);
    }

    [Fact]
    public void Crossbowclan_targetsAgiDex_notVit()
    {
        // rAthena: Crossbowclan = Agi+1, Dex+1 (the C# StatusCalcFlagDefaults had Vit — wrong).
        var mob = FreshMob();
        var sc = new StatusChange { Type = StatusType.Crossbowclan, Val1 = 1 };
        Apply(StatusType.Crossbowclan, sc, mob);
        Assert.Equal(51, mob.Stats.Agi);
        Assert.Equal(51, mob.Stats.Dex);
        Assert.Equal(50, mob.Stats.Vit);   // untouched
    }

    [Theory]
    [InlineData(StatusType.Battleorders)]
    [InlineData(StatusType.AllStatDown)]
    [InlineData(StatusType.Stomachache)]
    [InlineData(StatusType.Cheerup)]
    [InlineData(StatusType.BananaBomb)]
    [InlineData(StatusType.Swordclan)]
    [InlineData(StatusType.Arcwandclan)]
    [InlineData(StatusType.Goldenmaceclan)]
    [InlineData(StatusType.Crossbowclan)]
    public void PrimaryStatFix_isConverted_notGeneratorDefault(StatusType t)
        => Assert.DoesNotContain(t, _reg.GeneratedStatModDefaultTypes);

    // ---- SC-DERIVED-RECALC: the per-field-care handlers (percent / coupling / pool) survive recalc ----

    [Fact]
    public void GtChange_batkPercent_survivesRecalc()
    {
        var mob = FreshMob();
        mob.Stats.Batk = 1000;
        var h = _reg.Get(StatusType.GtChange)!;
        var sc = new StatusChange { Type = StatusType.GtChange, Val1 = 10 };  // Val2 = 80
        h.OnStart(mob, sc, null);
        Assert.Equal(1800, mob.Stats.Batk);          // +80% of 1000
        mob.Stats.Batk = 1000;                        // rebuild
        h.OnRecalc!(mob, sc);
        Assert.Equal(1800, mob.Stats.Batk);          // % re-applied on rebuilt base
    }

    [Fact]
    public void Fleet_batkPercent_survivesRecalc()
    {
        var mob = FreshMob();
        mob.Stats.Batk = 1000;
        var h = _reg.Get(StatusType.Fleet)!;
        var sc = new StatusChange { Type = StatusType.Fleet, Val1 = 5 };       // 30%
        h.OnStart(mob, sc, null);
        Assert.Equal(1300, mob.Stats.Batk);
        mob.Stats.Batk = 1000;
        h.OnRecalc!(mob, sc);
        Assert.Equal(1300, mob.Stats.Batk);
    }

    [Fact]
    public void Magicpower_smatkPercent_survivesRecalc()
    {
        var mob = FreshMob();
        mob.Stats.Smatk = 200;
        var h = _reg.Get(StatusType.Magicpower)!;
        var sc = new StatusChange { Type = StatusType.Magicpower, Val1 = 5 };  // 25%
        h.OnStart(mob, sc, null);
        Assert.Equal(250, mob.Stats.Smatk);
        mob.Stats.Smatk = 200;
        h.OnRecalc!(mob, sc);
        Assert.Equal(250, mob.Stats.Smatk);
    }

    [Fact]
    public void Truesight_criHit_survivesRecalc_statsRideParamBase()
    {
        var mob = FreshMob();
        mob.Stats.Cri = 30; mob.Stats.Hit = 80;
        var h = _reg.Get(StatusType.Truesight)!;
        var sc = new StatusChange { Type = StatusType.Truesight, Val1 = 5 };   // Val2=50 Cri, Val3=15 Hit
        h.OnStart(mob, sc, null);
        Assert.Equal(80, mob.Stats.Cri); Assert.Equal(95, mob.Stats.Hit);
        mob.Stats.Cri = 30; mob.Stats.Hit = 80;                                // CalcPc rebuild (Cri/Hit only)
        h.OnRecalc!(mob, sc);
        Assert.Equal(80, mob.Stats.Cri); Assert.Equal(95, mob.Stats.Hit);     // re-applied; +5 stats not double-counted
    }

    [Fact]
    public void Neutralbarrier_defMdefPercent_survivesRecalc()
    {
        var mob = FreshMob();
        mob.Stats.Def = 100; mob.Stats.Mdef = 50;
        var h = _reg.Get(StatusType.Neutralbarrier)!;
        var sc = new StatusChange { Type = StatusType.Neutralbarrier, Val1 = 5 };  // Val2 = 35%
        h.OnStart(mob, sc, null);
        Assert.Equal(135, mob.Stats.Def); Assert.Equal(67, mob.Stats.Mdef);   // +35% / +35%(17)
        mob.Stats.Def = 100; mob.Stats.Mdef = 50;
        h.OnRecalc!(mob, sc);
        Assert.Equal(135, mob.Stats.Def); Assert.Equal(67, mob.Stats.Mdef);
    }

    [Fact]
    public void Berserk_batkFleeDefZero_survivesRecalc_andRestoresOnEnd()
    {
        var mob = FreshMob();
        mob.Stats.MaxHp = 1000; mob.Stats.Hp = 1000; mob.Stats.Batk = 200; mob.Stats.Flee = 100;
        mob.Stats.Def = 80; mob.Stats.Def2 = 20; mob.Stats.Mdef = 40; mob.Stats.Mdef2 = 10;
        var h = _reg.Get(StatusType.Berserk)!;
        var sc = new StatusChange { Type = StatusType.Berserk, Val1 = 1 };
        h.OnStart(mob, sc, null);
        Assert.Equal(400, mob.Stats.Batk);           // +200
        Assert.Equal(50, mob.Stats.Flee);            // halved
        Assert.Equal(0, mob.Stats.Def);              // zeroed

        // CalcPc rebuilds Batk/Flee/Def/Def2/Mdef/Mdef2 to base; OnRecalc re-applies Berserk's axes.
        mob.Stats.Batk = 200; mob.Stats.Flee = 100;
        mob.Stats.Def = 80; mob.Stats.Def2 = 20; mob.Stats.Mdef = 40; mob.Stats.Mdef2 = 10;
        h.OnRecalc!(mob, sc);
        Assert.Equal(400, mob.Stats.Batk);
        Assert.Equal(50, mob.Stats.Flee);
        Assert.Equal(0, mob.Stats.Def); Assert.Equal(0, mob.Stats.Mdef);

        // OnEnd restores the re-snapshotted base (the rebuilt values).
        h.OnEnd!(mob, sc);
        Assert.Equal(200, mob.Stats.Batk);
        Assert.Equal(100, mob.Stats.Flee);
        Assert.Equal(80, mob.Stats.Def); Assert.Equal(20, mob.Stats.Def2);
        Assert.Equal(40, mob.Stats.Mdef); Assert.Equal(10, mob.Stats.Mdef2);
    }

    // ---- SC-DERIVED-RECALC: subtract-debuffs re-apply their reduction on recalc (zero-base on a PC,
    //      so tested on a mob with a set base) ----

    [Fact]
    public void ToxinOfMandara_resReduction_survivesRecalc()
    {
        var mob = FreshMob();
        mob.Stats.Res = 100;
        var h = _reg.Get(StatusType.ToxinOfMandara)!;
        var sc = new StatusChange { Type = StatusType.ToxinOfMandara, Val1 = 5 };
        h.OnStart(mob, sc, null);
        Assert.Equal(95, mob.Stats.Res);             // −Val1

        mob.Stats.Res = 100;                          // simulate CalcPc rebuild
        Assert.NotNull(h.OnRecalc);
        h.OnRecalc!(mob, sc);
        Assert.Equal(95, mob.Stats.Res);             // reduction re-applied
    }

    [Fact]
    public void Curse_batkQuarterDrop_survivesRecalc()
    {
        var mob = FreshMob();
        mob.Stats.Batk = 400;
        var h = _reg.Get(StatusType.Curse)!;
        var sc = new StatusChange { Type = StatusType.Curse, Val1 = 1 };
        h.OnStart(mob, sc, null);
        Assert.Equal(100, sc.Val3);                  // Batk/4 snapshot
        Assert.Equal(300, mob.Stats.Batk);           // −Val3

        mob.Stats.Batk = 400;                         // simulate CalcPc rebuild
        Assert.NotNull(h.OnRecalc);
        h.OnRecalc!(mob, sc);
        Assert.Equal(300, mob.Stats.Batk);           // drop re-applied (uses the snapshot, not re-quartered)
    }

    // ---- SC-MAGNITUDE turn 9: element-spirit option + Sunstance Watk/Matk buffs must survive recalc ----

    [Theory]
    // Each option SC adds its fixed Val2 to MatkMin/Max; OnRecalc must re-apply after CalcPc rebuilds it.
    [InlineData(StatusType.AquaplayOption, 40)]
    [InlineData(StatusType.BlastOption, 20)]
    [InlineData(StatusType.ChillyAirOption, 120)]
    [InlineData(StatusType.CoolerOption, 80)]
    public void MatkOption_survivesRecalc_viaOnRecalc(StatusType t, int expectedVal2)
    {
        var mob = FreshMob();                 // MatkMin=200, MatkMax=240
        var h = _reg.Get(t)!;
        var sc = new StatusChange { Type = t };
        h.OnStart(mob, sc, null);
        Assert.Equal(expectedVal2, sc.Val2);
        Assert.Equal(200 + expectedVal2, mob.Stats.MatkMin);

        mob.Stats.MatkMin = 200; mob.Stats.MatkMax = 240;  // simulate CalcPc rebuild
        Assert.NotNull(h.OnRecalc);
        h.OnRecalc!(mob, sc);
        Assert.Equal(200 + expectedVal2, mob.Stats.MatkMin);
        Assert.Equal(240 + expectedVal2, mob.Stats.MatkMax);
    }

    [Theory]
    // Each option SC adds its fixed Val2 to WatkMin/Max; OnRecalc must re-apply after CalcPc rebuilds it.
    [InlineData(StatusType.HeaterOption, 120)]
    [InlineData(StatusType.PyrotechnicOption, 60)]
    [InlineData(StatusType.TropicOption, 180)]
    public void WatkOption_survivesRecalc_viaOnRecalc(StatusType t, int expectedVal2)
    {
        var mob = FreshMob();
        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340;
        var h = _reg.Get(t)!;
        var sc = new StatusChange { Type = t };
        h.OnStart(mob, sc, null);
        Assert.Equal(expectedVal2, sc.Val2);
        Assert.Equal(300 + expectedVal2, mob.Stats.WatkMin);

        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340;  // simulate CalcPc rebuild
        Assert.NotNull(h.OnRecalc);
        h.OnRecalc!(mob, sc);
        Assert.Equal(300 + expectedVal2, mob.Stats.WatkMin);
        Assert.Equal(340 + expectedVal2, mob.Stats.WatkMax);
    }

    [Fact]
    public void Inspiration_appliesWatkMatkStatsAndMaxHpPercent_notBatk()
    {
        // status.cpp: watk += val2 (:7141), matk += val2 (:7224), stats += val3 (:6558+),
        // MaxHp bonus += 4*Val1 % (:3170). Val2 = 40*Val1, Val3 = 6*Val1.
        var mob = FreshMob();
        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340; mob.Stats.Batk = 100;
        mob.Stats.MaxHp = 1000; mob.Stats.Hp = 1000;
        var sc = new StatusChange { Type = StatusType.Inspiration, Val1 = 5 };

        Apply(StatusType.Inspiration, sc, mob);
        Assert.Equal(200, sc.Val2);                 // 40*5
        Assert.Equal(30, sc.Val3);                  // 6*5
        Assert.Equal(500, mob.Stats.WatkMin);       // +200 (Watk, NOT Batk)
        Assert.Equal(540, mob.Stats.WatkMax);
        Assert.Equal(100, mob.Stats.Batk);          // Batk untouched
        Assert.Equal(400, mob.Stats.MatkMin);       // 200 + 200
        Assert.Equal(80, mob.Stats.Str);            // 50 + 30
        Assert.Equal(1200, mob.Stats.MaxHp);        // +20% (4*5) of 1000, not flat +20

        _reg.Get(StatusType.Inspiration)!.OnEnd!(mob, sc);
        Assert.Equal(300, mob.Stats.WatkMin);
        Assert.Equal(200, mob.Stats.MatkMin);
        Assert.Equal(50, mob.Stats.Str);
        Assert.Equal(1000, mob.Stats.MaxHp);
    }

    [Fact]
    public void Inspiration_watkMatkSurviveRecalc_maxHpViaPool()
    {
        var mob = FreshMob();
        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340; mob.Stats.MaxHp = 1000; mob.Stats.Hp = 1000;
        var h = _reg.Get(StatusType.Inspiration)!;
        var sc = new StatusChange { Type = StatusType.Inspiration, Val1 = 5 };
        h.OnStart(mob, sc, null);

        // CalcPc rebuilds Watk/Matk and the MaxHp pool from base…
        mob.Stats.WatkMin = 300; mob.Stats.WatkMax = 340; mob.Stats.MatkMin = 200; mob.Stats.MatkMax = 240;
        mob.Stats.MaxHp = 1000;
        Assert.NotNull(h.OnRecalc);
        h.OnRecalc!(mob, sc);
        Assert.Equal(500, mob.Stats.WatkMin);       // Watk re-applied
        Assert.Equal(400, mob.Stats.MatkMin);       // Matk re-applied
        Assert.NotNull(h.OnRecalcPool);
        h.OnRecalcPool!(mob, sc);
        Assert.Equal(1200, mob.Stats.MaxHp);        // MaxHp% re-folded on rebuilt pool
    }

    [Fact]
    public void Sunstance_watkPercent_survivesRecalc_viaOnRecalc()
    {
        var mob = FreshMob();
        mob.Stats.WatkMin = 1000; mob.Stats.WatkMax = 1000; mob.Stats.Batk = 1000;
        var sc = new StatusChange { Type = StatusType.Sunstance, Val1 = 8 };  // Val2 = 2+8 = 10%
        var h = _reg.Get(StatusType.Sunstance)!;
        h.OnStart(mob, sc, null);
        Assert.Equal(10, sc.Val2);
        Assert.Equal(1100, mob.Stats.WatkMin);   // +10%

        mob.Stats.WatkMin = 1000; mob.Stats.WatkMax = 1000; mob.Stats.Batk = 1000;  // rebuild
        Assert.NotNull(h.OnRecalc);
        h.OnRecalc!(mob, sc);
        Assert.Equal(1100, mob.Stats.WatkMin);   // % re-applied on rebuilt base
        Assert.Equal(1100, mob.Stats.Batk);
    }

    [Theory]
    [InlineData(StatusType.DoramMatk, 99, 99)]   // matk += Val1
    [InlineData(StatusType.Izayoi, 3, 75)]        // matk += 25*Val1
    [InlineData(StatusType.Soulfairy, 5, 50)]     // matk += 10*Val1
    public void FlatMatk_survivesRecalc_viaOnRecalc(StatusType t, int val1, int expectedDelta)
    {
        var mob = FreshMob();                 // MatkMin=200, MatkMax=240
        var h = _reg.Get(t)!;
        var sc = new StatusChange { Type = t, Val1 = val1 };
        h.OnStart(mob, sc, null);
        Assert.Equal(200 + expectedDelta, mob.Stats.MatkMin);

        // Simulate CalcPc rebuilding MatkMin/Max from base (wipes the buff)…
        mob.Stats.MatkMin = 200; mob.Stats.MatkMax = 240;
        Assert.NotNull(h.OnRecalc);
        h.OnRecalc!(mob, sc);                 // …then the derived-reapply pass restores it.
        Assert.Equal(200 + expectedDelta, mob.Stats.MatkMin);
        Assert.Equal(240 + expectedDelta, mob.Stats.MatkMax);
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
