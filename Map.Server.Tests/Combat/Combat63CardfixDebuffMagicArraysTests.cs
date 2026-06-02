using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-63 — the two cardfix-remainder pieces shipped on top of COMBAT-43's ignore-def:
/// the element-debuff (<c>battle_calc_cardfix_debuff</c>, battle.cpp:667) folded into the
/// BF_MAGIC branch, and the distinct offensive magic <c>magic_addele/addsize/addclass</c>
/// arrays (kept separate from the weapon arrays in rAthena). Baseline cardfix is the
/// 1000-base identity, so a single +N% category resolves to exact integer damage.
/// </summary>
public class Combat63CardfixDebuffMagicArraysTests
{
    // ---- element-debuff (battle_calc_cardfix_debuff) ----

    [Fact]
    public void MagicPoison_adds_50_percent_regardless_of_element()
    {
        // SC_MAGIC_POISON on the target → +50% magic, element-agnostic.
        Assert.Equal(150, MagicDamageVsDebuff(StatusType.MagicPoison, BattleElement.Wind));
        Assert.Equal(150, MagicDamageVsDebuff(StatusType.MagicPoison, BattleElement.Fire));
    }

    [Fact]
    public void ClimaxBloom_adds_100_percent_only_for_fire()
    {
        Assert.Equal(200, MagicDamageVsDebuff(StatusType.ClimaxBloom, BattleElement.Fire));
        // Wrong element → no debuff.
        Assert.Equal(100, MagicDamageVsDebuff(StatusType.ClimaxBloom, BattleElement.Water));
    }

    [Fact]
    public void ClimaxEarth_adds_100_percent_only_for_earth()
    {
        Assert.Equal(200, MagicDamageVsDebuff(StatusType.ClimaxEarth, BattleElement.Earth));
        Assert.Equal(100, MagicDamageVsDebuff(StatusType.ClimaxEarth, BattleElement.Fire));
    }

    [Fact]
    public void MistyFrost_adds_15_percent_only_for_water()
    {
        Assert.Equal(115, MagicDamageVsDebuff(StatusType.Mistyfrost, BattleElement.Water));
        Assert.Equal(100, MagicDamageVsDebuff(StatusType.Mistyfrost, BattleElement.Fire));
    }

    [Fact]
    public void CloudPoison_adds_5x_val1_percent_only_for_poison()
    {
        // val1 = 4 → +20% on a poison attack.
        Assert.Equal(120, MagicDamageVsDebuff(StatusType.CloudPoison, BattleElement.Poison, val1: 4));
        Assert.Equal(100, MagicDamageVsDebuff(StatusType.CloudPoison, BattleElement.Wind, val1: 4));
    }

    [Fact]
    public void No_debuff_sc_leaves_magic_unchanged()
    {
        var (svc, _) = NewService();
        var pc = NewPlayer();
        Assert.Equal(100, svc.CalcCardFix(BattleAttackType.Magic, pc, NewMob(BattleElement.Neutral), 100,
            leftHand: false, attackElement: BattleElement.Fire));
    }

    // ---- distinct magic ele/size/class arrays ----

    [Fact]
    public void Magic_uses_magic_addele_not_the_weapon_addele()
    {
        var (svc, _) = NewService();
        var pc = NewPlayer();
        pc.EquipBonuses.MagicAddEle[(int)BattleElement.Fire] = 50; // magic-only +50%
        var target = NewMob(BattleElement.Fire);

        // Magic reads magic_addele → 150; weapon reads addele (0) → unchanged.
        Assert.Equal(150, svc.CalcCardFix(BattleAttackType.Magic, pc, target, 100, leftHand: false, attackElement: BattleElement.Fire));
        Assert.Equal(100, svc.CalcCardFix(BattleAttackType.Weapon, pc, target, 100, leftHand: false));
    }

    [Fact]
    public void Weapon_uses_weapon_addele_not_the_magic_addele()
    {
        var (svc, _) = NewService();
        var pc = NewPlayer();
        pc.EquipBonuses.AddEle[(int)BattleElement.Fire] = 50; // weapon-only +50%
        var target = NewMob(BattleElement.Fire);

        Assert.Equal(150, svc.CalcCardFix(BattleAttackType.Weapon, pc, target, 100, leftHand: false));
        Assert.Equal(100, svc.CalcCardFix(BattleAttackType.Magic, pc, target, 100, leftHand: false, attackElement: BattleElement.Fire));
    }

    [Fact]
    public void Magic_addsize_and_addclass_scale_only_magic()
    {
        var (svc, _) = NewService();
        var pc = NewPlayer();
        pc.EquipBonuses.MagicAddSize[(int)BattleSize.Medium] = 30;
        pc.EquipBonuses.MagicAddClass[(int)Map.Server.Inventory.BattleClassFlag.Normal] = 20;
        var target = NewMob(BattleElement.Neutral); // Medium, Normal class

        // 1000 * 1.30 (size) * 1.20 (class) = 1560 → 156.
        Assert.Equal(156, svc.CalcCardFix(BattleAttackType.Magic, pc, target, 100, leftHand: false, attackElement: BattleElement.Neutral));
        // Weapon arrays are 0 → unchanged.
        Assert.Equal(100, svc.CalcCardFix(BattleAttackType.Weapon, pc, target, 100, leftHand: false));
    }

    // ---- helpers ----

    private static long MagicDamageVsDebuff(StatusType sc, BattleElement atkEle, int val1 = 1)
    {
        var (svc, scSvc) = NewService();
        var pc = NewPlayer();
        var target = NewMob(BattleElement.Neutral);
        scSvc.Start(target, sc, val1, 0, 0, 0, 60_000);
        return svc.CalcCardFix(BattleAttackType.Magic, pc, target, 100, leftHand: false, attackElement: atkEle);
    }

    private static (BattleCardService svc, RecordingStatusChangeService sc) NewService()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance,
            new Lazy<IStatusChangeService>(() => sc));
        return (svc, sc);
    }

    private static PlayerEntity NewPlayer()
    {
        var p = new PlayerEntity(1, 1, "Mage", Guid.NewGuid(), 0, 0, 0);
        p.Stats.AttackRange = 1;
        return p;
    }

    private static MobEntity NewMob(BattleElement defEle)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.DefenseElement = defEle; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Race = BattleRace.Formless;
        return m;
    }
}
