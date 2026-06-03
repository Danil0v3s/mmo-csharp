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
/// COMBAT-82 — cardfix remainder: bMagicSubDefEle / bMagicSubSize (magic-only defense),
/// flag-matched bonus3 bSubEle (BF_LONG), and arrow_addele (ranged-only offense).
/// </summary>
public class Combat82CardfixRemainderTests
{
    private static BattleCardService Cards() => new(NullLogger<BattleCardService>.Instance);

    // ---- bMagicSubDefEle: magic-only resist keyed on the attacker's def element ----

    [Fact]
    public void MagicSubDefEle_reduces_magic_from_an_attacker_of_that_element()
    {
        var attacker = MakeMob(defEle: BattleElement.Fire, size: BattleSize.Medium, range: 1);
        var target = NewPlayer();
        target.EquipBonuses.MagicSubDefEle[(int)BattleElement.Fire] = 20;

        Assert.Equal(800, Cards().CalcCardFix(BattleAttackType.Magic, attacker, target, 1000, false,
            attackElement: BattleElement.Neutral));
        // weapon lane is unaffected (magic-only).
        Assert.Equal(1000, Cards().CalcCardFix(BattleAttackType.Weapon, attacker, target, 1000, false,
            attackElement: BattleElement.Neutral));
    }

    // ---- bMagicSubSize: magic-only per-size resist ----

    [Fact]
    public void MagicSubSize_reduces_magic_by_attacker_size()
    {
        var attacker = MakeMob(defEle: BattleElement.Neutral, size: BattleSize.Large, range: 1);
        var target = NewPlayer();
        target.EquipBonuses.MagicSubSize[(int)BattleSize.Large] = 20;

        Assert.Equal(800, Cards().CalcCardFix(BattleAttackType.Magic, attacker, target, 1000, false,
            attackElement: BattleElement.Neutral));
        Assert.Equal(1000, Cards().CalcCardFix(BattleAttackType.Weapon, attacker, target, 1000, false,
            attackElement: BattleElement.Neutral));
    }

    // ---- flag-matched bonus3 bSubEle, Ele, n, BF_LONG ----

    [Fact]
    public void Flag_matched_subele_reduces_only_long_attacks()
    {
        var target = NewPlayer();
        // bonus3 bSubEle, Ele_Neutral, 20, BF_LONG → defaulted flag.
        target.EquipBonuses.SubEle2.Add(((int)BattleElement.Neutral,
            BattleFlags.Default(BattleFlags.Long), 20));

        var longAtk = MakeMob(BattleElement.Neutral, BattleSize.Medium, range: 9);   // ranged → BF_LONG
        var shortAtk = MakeMob(BattleElement.Neutral, BattleSize.Medium, range: 1);  // melee → BF_SHORT
        longAtk.Stats.WeaponElement = (byte)BattleElement.Neutral;
        shortAtk.Stats.WeaponElement = (byte)BattleElement.Neutral;

        Assert.Equal(800, Cards().CalcCardFix(BattleAttackType.Weapon, longAtk, target, 1000, false));
        Assert.Equal(1000, Cards().CalcCardFix(BattleAttackType.Weapon, shortAtk, target, 1000, false));
    }

    // ---- arrow_addele: ranged-only offensive element bonus ----

    [Fact]
    public void ArrowAddEle_applies_only_on_a_ranged_swing()
    {
        var src = NewPlayer();
        src.EquipBonuses.ArrowAddEle[(int)BattleElement.Neutral] = 20;
        var target = MakeMob(BattleElement.Neutral, BattleSize.Medium, range: 1);

        src.Stats.AttackRange = 9; // bow // bow → ranged
        Assert.Equal(1200, Cards().CalcCardFix(BattleAttackType.Weapon, src, target, 1000, false));
        src.Stats.AttackRange = 1; // melee → no arrow bonus
        Assert.Equal(1000, Cards().CalcCardFix(BattleAttackType.Weapon, src, target, 1000, false));
    }

    // ---- helpers ----

    private static PlayerEntity NewPlayer()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Race = BattleRace.Demihuman; pc.Stats.Size = BattleSize.Medium;
        pc.Stats.DefenseElement = BattleElement.Neutral; pc.Stats.ElementLevel = 1;
        pc.Stats.AttackRange = 1;
        return pc;
    }

    private static MobEntity MakeMob(BattleElement defEle, BattleSize size, int range)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Race = BattleRace.Formless; m.Stats.Size = size;
        m.Stats.DefenseElement = defEle; m.Stats.ElementLevel = 1;
        m.Stats.WeaponElement = (byte)BattleElement.Neutral;
        m.Stats.AttackRange = (short)range;
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }
}
