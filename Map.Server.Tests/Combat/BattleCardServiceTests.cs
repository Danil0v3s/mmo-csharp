using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// T2.2 acceptance tests — the card-modifier port. Damage flows
/// through <see cref="BattleCardService.CalcCardFix"/> using the
/// equipped items' bonus scripts to pick up race / element / size /
/// class multipliers.
/// </summary>
public class BattleCardServiceTests
{
    private static PlayerEntity NewPc() =>
        new(characterId: 1, accountId: 1, name: "test",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0);

    private static MobEntity NewMob(BattleRace race, BattleSize size = BattleSize.Medium,
        BattleElement defElement = BattleElement.Neutral, MobMode mode = MobMode.None)
    {
        var m = new MobEntity(new EntityId(2), classId: 1001, name: "test_mob", mapId: 0, x: 0, y: 0);
        m.Stats.Race = race;
        m.Stats.Size = size;
        m.Stats.DefenseElement = defElement;
        m.Stats.Mode = mode;
        return m;
    }

    [Fact]
    public void CalcCardFix_NoBonuses_ReturnsInputDamage()
    {
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        var mob = NewMob(BattleRace.Demihuman);
        Assert.Equal(1000, svc.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void HydraCard_OnDemiHuman_Adds20Percent()
    {
        // bonus2 bAddRace,RC_DemiHuman,20;  (the Hydra card script)
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        BonusScriptExtractor.Apply("bonus2 bAddRace,RC_DemiHuman,20;", pc.EquipBonuses);
        var mob = NewMob(BattleRace.Demihuman);
        Assert.Equal(1200, svc.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void HydraCard_OnNonDemiHuman_NoChange()
    {
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        BonusScriptExtractor.Apply("bonus2 bAddRace,RC_DemiHuman,20;", pc.EquipBonuses);
        var insect = NewMob(BattleRace.Insect);
        Assert.Equal(1000, svc.CalcCardFix(BattleAttackType.Weapon, pc, insect, 1000, false));
    }

    [Fact]
    public void SkelWorkerCard_AddSizeLarge25_OnLargeMob()
    {
        // bonus2 bAddSize,Size_Large,25; — Skel Worker card.
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        BonusScriptExtractor.Apply("bonus2 bAddSize,Size_Large,25;", pc.EquipBonuses);
        var mob = NewMob(BattleRace.Brute, BattleSize.Large);
        Assert.Equal(1250, svc.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void GoblinCard_AddSizeMedium15_StacksWithRace()
    {
        // bonus2 bAddSize,Size_Medium,15; + Hydra style → +35% total.
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        BonusScriptExtractor.Apply(
            "bonus2 bAddRace,RC_DemiHuman,20; bonus2 bAddSize,Size_Medium,15;",
            pc.EquipBonuses);
        var mob = NewMob(BattleRace.Demihuman, BattleSize.Medium);
        Assert.Equal(1350, svc.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void AddRace_All_AppliesRegardlessOfTarget()
    {
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        BonusScriptExtractor.Apply("bonus2 bAddRace,RC_All,10;", pc.EquipBonuses);
        var mob = NewMob(BattleRace.Dragon);
        Assert.Equal(1100, svc.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void AddClass_Boss_AppliesToMvp()
    {
        // bonus2 bAddClass,Class_Boss,50; — anti-MVP weapons.
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        BonusScriptExtractor.Apply("bonus2 bAddClass,Class_Boss,50;", pc.EquipBonuses);
        var mvp = NewMob(BattleRace.Dragon, mode: MobMode.Mvp);
        Assert.Equal(1500, svc.CalcCardFix(BattleAttackType.Weapon, pc, mvp, 1000, false));
    }

    [Fact]
    public void AddClass_Boss_DoesNotApplyToNormalMob()
    {
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        BonusScriptExtractor.Apply("bonus2 bAddClass,Class_Boss,50;", pc.EquipBonuses);
        var normal = NewMob(BattleRace.Demihuman);
        Assert.Equal(1000, svc.CalcCardFix(BattleAttackType.Weapon, pc, normal, 1000, false));
    }

    [Fact]
    public void LongAtkRate_AppliesWhenWeaponRangeIsRanged()
    {
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        pc.Stats.AttackRange = 9; // bow range
        BonusScriptExtractor.Apply("bonus bLongAtkRate,30;", pc.EquipBonuses);
        var mob = NewMob(BattleRace.Insect);
        Assert.Equal(1300, svc.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void ShortAtkRate_AppliesWhenWeaponRangeIsMelee()
    {
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var pc = NewPc();
        pc.Stats.AttackRange = 1; // dagger range
        BonusScriptExtractor.Apply("bonus bShortAtkRate,15;", pc.EquipBonuses);
        var mob = NewMob(BattleRace.Insect);
        Assert.Equal(1150, svc.CalcCardFix(BattleAttackType.Weapon, pc, mob, 1000, false));
    }

    [Fact]
    public void NonPlayer_Source_NoCardFix()
    {
        // Mobs don't wear cards — pass-through preserved.
        var svc = new BattleCardService(NullLogger<BattleCardService>.Instance);
        var attacker = NewMob(BattleRace.Insect);
        var target = NewMob(BattleRace.Demihuman);
        Assert.Equal(1000, svc.CalcCardFix(BattleAttackType.Weapon, attacker, target, 1000, false));
    }
}
