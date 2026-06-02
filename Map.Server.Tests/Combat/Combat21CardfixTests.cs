using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-21 — advanced cardfix: per-category multiplicative grouping
/// (battle_calc_cardfix / APPLY_CARDFIX, battle.cpp:711/748), magic-add-race,
/// and critical-add-race.
/// </summary>
public class Combat21CardfixTests
{
    private static BattleCardService Cards() => new(NullLogger<BattleCardService>.Instance);

    // ---- per-category multiplicative grouping ----

    [Fact]
    public void Offensive_categories_stack_multiplicatively()
    {
        // +20% vs race AND +20% vs size → ×1.2×1.2 = ×1.44 (additive would be ×1.40).
        var src = NewPlayer();
        src.EquipBonuses.AddRace[(int)BattleRace.Demihuman] = 20;
        src.EquipBonuses.AddSize[(int)BattleSize.Medium] = 20;
        var target = MakeMob(BattleRace.Demihuman, BattleSize.Medium);

        Assert.Equal(1440, Cards().CalcCardFix(BattleAttackType.Weapon, src, target, 1000, leftHand: false));
    }

    [Fact]
    public void Defensive_categories_stack_multiplicatively()
    {
        // bSubRace 20 + bSubEle 20 → ×0.8×0.8 = ×0.64 (additive would be ×0.60).
        var attacker = MakeMob(BattleRace.Formless, BattleSize.Medium);
        attacker.Stats.WeaponElement = (byte)BattleElement.Neutral;
        var target = NewPlayer();
        target.EquipBonuses.SubRace[(int)BattleRace.Formless] = 20;
        target.EquipBonuses.SubEle[(int)BattleElement.Neutral] = 20;

        Assert.Equal(640, Cards().CalcCardFix(BattleAttackType.Weapon, attacker, target, 1000, leftHand: false));
    }

    [Fact]
    public void Cardfix_below_zero_zeroes_damage()
    {
        // A -100%+ sub-element resist drives cardfix ≤ 0 → 0 damage (floored to 1
        // by the final guard).
        var attacker = MakeMob(BattleRace.Formless, BattleSize.Medium);
        var target = NewPlayer();
        target.EquipBonuses.SubEle[(int)BattleElement.Neutral] = 100;

        Assert.Equal(1, Cards().CalcCardFix(BattleAttackType.Weapon, attacker, target, 1000, leftHand: false));
    }

    // ---- magic-add-race vs weapon add-race ----

    [Fact]
    public void Magic_uses_magic_add_race_not_weapon_add_race()
    {
        var src = NewPlayer();
        src.EquipBonuses.MagicAddRace[(int)BattleRace.Demihuman] = 50; // +50% magic vs demi
        src.EquipBonuses.AddRace[(int)BattleRace.Demihuman] = 99;      // weapon only
        var target = MakeMob(BattleRace.Demihuman, BattleSize.Medium);

        Assert.Equal(1500, Cards().CalcCardFix(BattleAttackType.Magic, src, target, 1000, leftHand: false));
        Assert.Equal(1990, Cards().CalcCardFix(BattleAttackType.Weapon, src, target, 1000, leftHand: false));
    }

    // ---- critical-add-race ----

    [Fact]
    public void Critical_add_race_lets_zero_crit_attacker_crit_a_race()
    {
        // base cri 0 + bCriticalAddRace vs Demihuman (stored ×10) → guaranteed crit
        // vs a demi target, but NOT vs another race.
        var pc = NewPlayer();
        pc.Stats.Cri = 0;
        pc.Stats.WatkMin = pc.Stats.WatkMax = 50;
        pc.Stats.Dex = 50; pc.Stats.Hit = 10000;
        pc.EquipBonuses.CritAddRace[(int)BattleRace.Demihuman] = 1000; // 100% (×10)

        var calc = new BattleCalculator(new Random(0));
        var demi = MakeMob(BattleRace.Demihuman, BattleSize.Medium);
        var brute = MakeMob(BattleRace.Brute, BattleSize.Medium);

        Assert.True(calc.CalcWeaponAttack(pc, demi).IsCritical);
        Assert.False(calc.CalcWeaponAttack(pc, brute).IsCritical);
    }

    // ---- extractor parse ----

    [Fact]
    public void Extractor_parses_magicaddrace_and_criticaladdrace()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.ApplyIndexedBonus(b, "MagicAddRace", "RC_DemiHuman", 30);
        BonusScriptExtractor.ApplyIndexedBonus(b, "CriticalAddRace", "RC_Brute", 5);

        Assert.Equal(30, b.MagicAddRace[(int)BattleRace.Demihuman]);
        Assert.Equal(50, b.CritAddRace[(int)BattleRace.Brute]); // 5 × 10
    }

    // ---- helpers ----

    private static PlayerEntity NewPlayer()
    {
        var p = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        p.Stats.AttackRange = 1;
        p.Stats.WeaponElement = (byte)BattleElement.Neutral;
        return p;
    }

    private static MobEntity MakeMob(BattleRace race, BattleSize size)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Race = race; m.Stats.Size = size;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }
}
