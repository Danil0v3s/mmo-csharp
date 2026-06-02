using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-05 (axis 1) — defender-side cardfix. rAthena battle_calc_cardfix
/// (battle.cpp:711) folds the DEFENDER's bSubRace/bSubEle/bSubSize/bSubClass
/// resist cards, indexed by the ATTACKER's attributes — including against mob
/// attackers, which the old `src is not PC → return` early-out skipped.
/// </summary>
public class Combat05DefenderCardfixTests
{
    private static readonly BattleCardService Cards = new(NullLogger<BattleCardService>.Instance);

    private static MobEntity BruteFireMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "M", Name = "M", Hp = 5000 };
        var m = new MobEntity(new EntityId(900), db, new MobSpawnEntry { MapId = 0, MobClassId = 1002 }, 0, 0, 0);
        m.Stats.Race = BattleRace.Brute;
        m.Stats.WeaponElement = (byte)BattleElement.Fire;
        m.Stats.Size = BattleSize.Medium;
        return m;
    }

    private static PlayerEntity Pc() => new(1, 1, "Hero", System.Guid.NewGuid(), 0, 0, 0);

    [Fact]
    public void Defender_bSubRace_reduces_mob_damage()
    {
        var mob = BruteFireMob();
        var pc = Pc();
        pc.EquipBonuses.SubRace[(int)BattleRace.Brute] = 30;
        // 1000 × (100 - 30)/100 = 700
        Assert.Equal(700, Cards.CalcCardFix(BattleAttackType.Weapon, mob, pc, 1000, false));
    }

    [Fact]
    public void Defender_bSubEle_reduces_by_attacker_element()
    {
        var mob = BruteFireMob();
        var pc = Pc();
        pc.EquipBonuses.SubEle[(int)BattleElement.Fire] = 50;
        Assert.Equal(500, Cards.CalcCardFix(BattleAttackType.Weapon, mob, pc, 1000, false));
    }

    [Fact]
    public void Defender_bSubSize_and_bSubClass_apply()
    {
        var mob = BruteFireMob();
        mob.Stats.Mode |= MobMode.Mvp; // attacker is a boss/MVP
        var pc = Pc();
        pc.EquipBonuses.SubSize[(int)BattleSize.Medium] = 20;
        pc.EquipBonuses.SubClass[(int)Map.Server.Inventory.BattleClassFlag.Boss] = 25;
        // COMBAT-21 — per-category multiplicative: 1000 × 0.80 × 0.75 = 600
        // (the old additive form gave 550).
        Assert.Equal(600, Cards.CalcCardFix(BattleAttackType.Weapon, mob, pc, 1000, false));
    }

    [Fact]
    public void Defender_subRace_All_slot_stacks()
    {
        var mob = BruteFireMob();
        var pc = Pc();
        pc.EquipBonuses.SubRace[(int)BattleRace.Brute] = 10;
        pc.EquipBonuses.SubRace[(int)BattleRace.All] = 15;
        Assert.Equal(750, Cards.CalcCardFix(BattleAttackType.Weapon, mob, pc, 1000, false)); // -25%
    }

    [Fact]
    public void MobVsMob_is_unchanged()
    {
        var a = BruteFireMob();
        var b = BruteFireMob();
        Assert.Equal(1000, Cards.CalcCardFix(BattleAttackType.Weapon, a, b, 1000, false));
    }

    [Fact]
    public void Attacker_offensive_AddRace_still_works_regression()
    {
        // PC attacker vs a brute mob — the existing offensive path is intact.
        var pc = Pc();
        pc.EquipBonuses.AddRace[(int)BattleRace.Brute] = 50;
        pc.Stats.AttackRange = 1;
        var mob = BruteFireMob();
        Assert.Equal(1500, Cards.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void PvP_applies_both_attacker_and_defender_cards()
    {
        var atk = Pc(); atk.Stats.Race = BattleRace.PlayerHuman; atk.Stats.Size = BattleSize.Medium;
        atk.Stats.WeaponElement = (byte)BattleElement.Neutral; atk.Stats.AttackRange = 1;
        atk.EquipBonuses.AddRace[(int)BattleRace.PlayerHuman] = 20; // +20% vs humans
        var def = Pc();
        def.EquipBonuses.SubRace[(int)BattleRace.PlayerHuman] = 10; // -10% from humans
        // COMBAT-21 — attacker (×1.20) then defender (×0.90) apply as separate
        // APPLY_CARDFIX passes: 1000 × 1.20 × 0.90 = 1080 (old additive: 1100).
        Assert.Equal(1080, Cards.CalcCardFix(BattleAttackType.Weapon, atk, def, 1000, false));
    }
}
