using System;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Skills;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-65 — the two flag-form consumers (Unbreakable break mask, Intravision
/// see-hidden) + the SC speed-table core (<c>status_calc_speed</c>, status.cpp:7787).
/// </summary>
public class Combat65UnbreakableIntravisionSpeedTests
{
    // ---- Unbreakable break mask (skill_break_equip, skill.cpp:2840) ----

    [Fact]
    public void UnbreakableMask_maps_each_flag_to_its_slot_bit()
    {
        Assert.Equal(0u, SkillSideEffectService.UnbreakableMask(Pc(b => { })));
        Assert.Equal(EquipBits.Armor, SkillSideEffectService.UnbreakableMask(Pc(b => b.UnbreakableArmor = true)));
        Assert.Equal(EquipBits.HandR, SkillSideEffectService.UnbreakableMask(Pc(b => b.UnbreakableWeapon = true)));
        Assert.Equal(EquipBits.HandL, SkillSideEffectService.UnbreakableMask(Pc(b => b.UnbreakableShield = true)));
        Assert.Equal(EquipBits.Helm, SkillSideEffectService.UnbreakableMask(Pc(b => b.UnbreakableHelm = true)));
        Assert.Equal(EquipBits.Shoes, SkillSideEffectService.UnbreakableMask(Pc(b => b.UnbreakableShoes = true)));
        Assert.Equal(EquipBits.Garment, SkillSideEffectService.UnbreakableMask(Pc(b => b.UnbreakableGarment = true)));
    }

    [Fact]
    public void UnbreakableMask_combines_multiple_slots()
    {
        var mask = SkillSideEffectService.UnbreakableMask(Pc(b =>
        {
            b.UnbreakableWeapon = true;
            b.UnbreakableShield = true;
            b.UnbreakableArmor = true;
        }));
        Assert.Equal(EquipBits.HandR | EquipBits.HandL | EquipBits.Armor, mask);
        // A break of an armor-only mask against this wearer is fully masked out.
        Assert.Equal(0u, (uint)EquipBits.Armor & ~mask);
    }

    // ---- Intravision see-hidden (status.cpp:3756 special_state.intravision) ----

    [Fact]
    public void Intravision_pc_pierces_base_hide()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var target = Pc(_ => { });
        sc.Start(target, StatusType.Hiding, 1, 0, 0, 0, 60_000);

        var plain = Pc(_ => { });
        Assert.False(plain.CanSee(target, sc)); // ordinary PC can't see a hidden target

        var seer = Pc(b => b.Intravision = true);
        Assert.True(seer.CanSee(target, sc));   // intravision pierces base hide
    }

    [Fact]
    public void Intravision_does_not_pierce_perfect_hide()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var target = Pc(_ => { });
        sc.Start(target, StatusType.Cloakingexceed, 1, 0, 0, 0, 60_000);

        var seer = Pc(b => b.Intravision = true);
        Assert.False(seer.CanSee(target, sc)); // perfect-hide still blocks intravision
    }

    // ---- SC speed table ----

    [Fact]
    public void Baseline_speed_is_the_default_walk_speed()
        => Assert.Equal(150, SpeedWith((sc, pc) => { }));

    [Fact]
    public void IncreaseAgi_speeds_up_25_percent()
        => Assert.Equal(112, SpeedWith((sc, pc) => sc.Start(pc, StatusType.IncreaseAgi, 1, 0, 0, 0, 60_000)));

    [Fact]
    public void DecreaseAgi_slows_25_percent()
        => Assert.Equal(187, SpeedWith((sc, pc) => sc.Start(pc, StatusType.DecreaseAgi, 1, 0, 0, 0, 60_000)));

    [Fact]
    public void WindWalk_speedup_scales_with_val1()
        => Assert.Equal(135, SpeedWith((sc, pc) => sc.Start(pc, StatusType.Windwalk, val1: 5, 0, 0, 0, 60_000))); // 2*5=10% → 135

    [Fact]
    public void SlowPotion_uses_val1_slowdown()
        => Assert.Equal(225, SpeedWith((sc, pc) => sc.Start(pc, StatusType.Slowdown, val1: 50, 0, 0, 0, 60_000)));

    [Fact]
    public void SteelBody_overrides_to_200_even_with_a_slowdown()
        => Assert.Equal(200, SpeedWith((sc, pc) =>
        {
            sc.Start(pc, StatusType.DecreaseAgi, 1, 0, 0, 0, 60_000);
            sc.Start(pc, StatusType.Steelbody, 1, 0, 0, 0, 60_000);
        }));

    [Fact]
    public void Defender_floors_speed_at_200()
        => Assert.Equal(200, SpeedWith((sc, pc) => sc.Start(pc, StatusType.Defender, 1, 0, 0, 0, 60_000)));

    [Fact]
    public void Equip_speedup_still_folds_in_without_any_sc()
    {
        // Backward-compat: a -25 equip delta (faster) with no SC → 150 * 75/100 = 112.
        var pc = Pc(b => b.SpeedRate = -25);
        var calc = new StatusCalcService();
        calc.CalcPc(pc, SpeedInputs);
        Assert.Equal(112, pc.Stats.Speed);
    }

    // ---- helpers ----

    private static readonly PcBaseInputs SpeedInputs = new(
        BaseLevel: 99, JobLevel: 50,
        Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
        AttackRange: 1);

    private static int SpeedWith(Action<RecordingStatusChangeService, PlayerEntity> arrange)
    {
        var pc = new PlayerEntity(7, 7, "Mover", Guid.NewGuid(), 0, 0, 0);
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        arrange(sc, pc);
        var calc = new StatusCalcService(sc: new Lazy<IStatusChangeService>(() => sc));
        calc.CalcPc(pc, SpeedInputs);
        return pc.Stats.Speed;
    }

    private static PlayerEntity Pc(Action<EquipBonusBundle> configure)
    {
        var pc = new PlayerEntity(1, 1, "PC", Guid.NewGuid(), 0, 0, 0);
        configure(pc.EquipBonuses);
        return pc;
    }
}
