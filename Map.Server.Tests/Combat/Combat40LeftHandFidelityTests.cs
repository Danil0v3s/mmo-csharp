using System;
using Core.Database.Entities;
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
/// COMBAT-40 — left-hand renewal fidelity. The dual-wield off-hand resolves its
/// weapon mastery from the OFF-HAND weapon type (rAthena battle_addmastery's
/// weapontype2 branch) and its element fix from the LEFT weapon element.
/// </summary>
public class Combat40LeftHandFidelityTests
{
    static Combat40LeftHandFidelityTests()
    {
        // ElementTable.Initialize REPLACES the global matrix; seed the superset
        // the other element test classes use so parallel ordering can't wipe an
        // entry (COMBAT-23 race), plus Water→Fire 150 for the off-hand element test.
        ElementTable.Initialize(new[]
        {
            new AttrFixDbEntity { Level = 1, AttackerElement = "Fire", DefenderElement = "Water", Multiplier = 90 },
            new AttrFixDbEntity { Level = 1, AttackerElement = "Water", DefenderElement = "Fire", Multiplier = 150 },
        });
    }

    private const ushort PR_MACEMASTERY = 65;
    private const ushort AS_KATAR = 134;
    private const ushort SM_SWORD = 2;

    // ---- criterion 1: AddMastery is weapon-type (hand) aware ----

    [Fact]
    public void AddMastery_resolves_the_passed_weapon_type()
    {
        var cards = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        pc.LearnedSkills[AS_KATAR] = 5;     // Katar Mastery → +3/lv = +15, katar only

        Assert.Equal(15, cards.AddMastery(pc, NewTarget(), 1000, BattleAttackType.Weapon, WeaponTypeCodes.Katar));
        Assert.Equal(0, cards.AddMastery(pc, NewTarget(), 1000, BattleAttackType.Weapon, WeaponTypeCodes.Dagger));
    }

    [Fact]
    public void AddMastery_sword_mastery_applies_to_dagger_not_katar()
    {
        var cards = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        pc.LearnedSkills[SM_SWORD] = 10;    // Sword Mastery → +4/lv = +40 for 1HSword/Dagger

        Assert.Equal(40, cards.AddMastery(pc, NewTarget(), 1000, BattleAttackType.Weapon, WeaponTypeCodes.Dagger));
        Assert.Equal(0, cards.AddMastery(pc, NewTarget(), 1000, BattleAttackType.Weapon, WeaponTypeCodes.Katar));
    }

    [Fact]
    public void Off_hand_mastery_comes_from_the_off_hand_weapon()
    {
        // PC learns Mace Mastery. With a MACE off-hand the off-hand swing gets the
        // mace mastery; with a DAGGER off-hand it does not — proving the left-hand
        // mastery is resolved from the off-hand weapon type, not the main hand.
        var calc = new BattleCalculator(new Random(0), cards: new BattleCardService(NullLogger<BattleCardService>.Instance));

        var maceOff = MakeDualWielder(rightType: WeaponTypeCodes.OneHandSword, leftType: WeaponTypeCodes.Mace);
        maceOff.LearnedSkills[PR_MACEMASTERY] = 10; // +30 off-hand mastery
        var withMace = calc.CalcWeaponAttack(maceOff, NewTarget());

        var daggerOff = MakeDualWielder(rightType: WeaponTypeCodes.OneHandSword, leftType: WeaponTypeCodes.Dagger);
        daggerOff.LearnedSkills[PR_MACEMASTERY] = 10; // mace mastery doesn't apply to a dagger
        var withDagger = calc.CalcWeaponAttack(daggerOff, NewTarget());

        Assert.True(withMace.Damage2 > withDagger.Damage2,
            $"mace off-hand ({withMace.Damage2}) should out-damage dagger off-hand ({withDagger.Damage2})");
    }

    // ---- criterion 2: off-hand element fix uses the LEFT weapon element ----

    [Fact]
    public void Off_hand_uses_the_left_weapon_element()
    {
        var calc = new BattleCalculator(new Random(0), cards: new BattleCardService(NullLogger<BattleCardService>.Instance));
        var target = NewTarget();
        target.Stats.DefenseElement = BattleElement.Fire; // Water→Fire = 150%, Neutral→Fire = 100%

        var waterOff = MakeDualWielder(rightType: WeaponTypeCodes.Dagger, leftType: WeaponTypeCodes.Dagger);
        waterOff.Stats.LeftWeaponElement = (byte)BattleElement.Water;
        var withWater = calc.CalcWeaponAttack(waterOff, target);

        var neutralOff = MakeDualWielder(rightType: WeaponTypeCodes.Dagger, leftType: WeaponTypeCodes.Dagger);
        neutralOff.Stats.LeftWeaponElement = (byte)BattleElement.Neutral;
        var withNeutral = calc.CalcWeaponAttack(neutralOff, target);

        Assert.True(withWater.Damage2 > withNeutral.Damage2,
            $"Water off-hand vs Fire ({withWater.Damage2}) should beat Neutral ({withNeutral.Damage2})");
    }

    // ---- helpers ----

    private static PlayerEntity NewPc() => new(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);

    private static PlayerEntity MakeDualWielder(int rightType, int leftType)
    {
        var pc = new PlayerEntity(1, 1, "Sin", Guid.NewGuid(), 0, 0, 0)
        {
            ClassMask = MapidClass.Assassin, // thief base → AS_RIGHT/AS_LEFT split applies
            WeaponType = rightType,
        };
        pc.Stats.Dex = 100;
        pc.Stats.Batk = 0;
        pc.Stats.Cri = 0;
        pc.Stats.Hit = 10000;
        pc.Stats.WeaponLevel = 0;
        pc.Stats.WatkMin = pc.Stats.WatkMax = 100;
        pc.Stats.LeftWeaponLevel = 0;
        pc.Stats.LeftWeaponType = leftType;
        pc.Stats.LeftWatkMin = pc.Stats.LeftWatkMax = 100;
        return pc;
    }

    private static MobEntity NewTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium;
        m.Stats.Race = BattleRace.Formless; // no race-bane masteries fire
        m.Stats.Flee = 0; m.Stats.Flee2 = 0;
        m.MaxHp = m.Hp = 5000;
        return m;
    }
}
