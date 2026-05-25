using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// Wave 66 / Track B — dmotion + walkdelay on <see cref="BattleDamage"/>.
/// Verifies that <see cref="BattleCalculator.CalcWeaponAttack"/> populates
/// the new motion fields per the rAthena Damage struct (battle.hpp).
///
/// rAthena reference: <c>battle_damage</c> (battle.cpp:8243) writes
/// dmotion = target's animation motion - 50 (clamped 0..2000), and
/// walkdelay = max(80, dmotion/2) on hits, 0 on misses.
/// </summary>
public class Wave66MotionFieldsTests
{
    private static PlayerEntity NewPc(int hit = 200) =>
        new(characterId: 1, accountId: 1, name: "test",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0)
        {
            Stats =
            {
                Hit = (short)hit,
                Cri = 0,
                WatkMin = 100,
                WatkMax = 100,
                Amotion = 500,
            },
        };

    private static MobEntity NewMob(int flee = 50, int amotion = 600)
    {
        var m = new MobEntity(new EntityId(2), classId: 1001, name: "test_mob",
            mapId: 0, x: 0, y: 0);
        m.Stats.Flee = (short)flee;
        m.Stats.Amotion = (ushort)amotion;
        m.Stats.Def = 10;
        m.Stats.Def2 = 10;
        m.Stats.Race = BattleRace.Demihuman;
        m.Stats.Size = BattleSize.Medium;
        m.Stats.DefenseElement = BattleElement.Neutral;
        m.Stats.ElementLevel = 1;
        m.Hp = m.MaxHp = 1000;
        return m;
    }

    [Fact]
    public void Hit_PopulatesDMotionFromTargetAmotion()
    {
        // amotion 600 → dmotion = 550 (capped 0..2000)
        var calc = new BattleCalculator(rng: new Random(42));
        var pc = NewPc(hit: 9999);  // guarantee hit
        var mob = NewMob(flee: 0, amotion: 600);
        var dmg = calc.CalcWeaponAttack(pc, mob);
        Assert.True(dmg.Total > 0);
        Assert.Equal(550, dmg.DMotion);
    }

    [Fact]
    public void Hit_WalkDelayIsHalfDMotion_OrFloorAt80()
    {
        // amotion 600 → dmotion 550 → walkdelay = max(80, 275) = 275
        var calc = new BattleCalculator(rng: new Random(42));
        var pc = NewPc(hit: 9999);
        var mob = NewMob(flee: 0, amotion: 600);
        var dmg = calc.CalcWeaponAttack(pc, mob);
        Assert.Equal(275, dmg.WalkDelay);
    }

    [Fact]
    public void Hit_WithLowAmotion_WalkDelayFloorsAt80()
    {
        // amotion 100 → dmotion 50 → walkdelay = max(80, 25) = 80
        var calc = new BattleCalculator(rng: new Random(42));
        var pc = NewPc(hit: 9999);
        var mob = NewMob(flee: 0, amotion: 100);
        var dmg = calc.CalcWeaponAttack(pc, mob);
        Assert.Equal(50, dmg.DMotion);
        Assert.Equal(80, dmg.WalkDelay);
    }

    [Fact]
    public void Hit_AmotionClampedAtMax2000()
    {
        // amotion 5000 → dmotion = clamp(4950, 0, 2000) = 2000
        var calc = new BattleCalculator(rng: new Random(42));
        var pc = NewPc(hit: 9999);
        var mob = NewMob(flee: 0, amotion: 5000);
        var dmg = calc.CalcWeaponAttack(pc, mob);
        Assert.Equal(2000, dmg.DMotion);
        Assert.Equal(1000, dmg.WalkDelay);
    }

    [Fact]
    public void Miss_DMotionAndWalkDelayBothZero()
    {
        // Lucky-dodge guarantees a miss (Flee2 is stored ×10 and rolled
        // vs 1000, no clamp). Setting Flee2=1000 = always lucky-dodge.
        var calc = new BattleCalculator(rng: new Random(42));
        var pc = NewPc(hit: 9999);
        var mob = NewMob(flee: 0, amotion: 600);
        mob.Stats.Flee2 = 1000;
        var dmg = calc.CalcWeaponAttack(pc, mob);
        Assert.Equal(0, dmg.Total);
        Assert.Equal(0, dmg.DMotion);
        Assert.Equal(0, dmg.WalkDelay);
    }

    [Fact]
    public void IsSpDamage_DefaultsToFalse()
    {
        // Track C field — default false (only SP-drain skills flip it).
        var calc = new BattleCalculator(rng: new Random(42));
        var pc = NewPc(hit: 9999);
        var mob = NewMob(flee: 0);
        var dmg = calc.CalcWeaponAttack(pc, mob);
        Assert.False(dmg.IsSpDamage);
    }
}
