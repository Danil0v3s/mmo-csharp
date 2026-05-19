using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// Pin behavior of the renewal weapon-attack pipeline against rAthena
/// formula references. Per-call randomness is deterministic via a seeded
/// RNG; expected values come from manual eval of the same formulas.
/// </summary>
public class BattleCalculatorTests
{
    [Fact]
    public void ZeroStatTarget_AlwaysHits()
    {
        var (attacker, target) = NewPair();
        // Attacker hit 200, target flee 0 → hitrate clamped to 100 anyway.
        attacker.Stats.Hit = 200;
        target.Stats.Flee = 0;
        target.Stats.Flee2 = 0;
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 10;
        attacker.Stats.Batk = 5;
        var calc = new BattleCalculator(new Random(0));

        for (var i = 0; i < 50; i++)
        {
            var d = calc.CalcWeaponAttack(attacker, target);
            Assert.True(d.DidHit);
            Assert.True(d.Total >= 1);
        }
    }

    [Fact]
    public void TargetWithHighFlee_MissesAtLeastOnce()
    {
        var (attacker, target) = NewPair();
        attacker.Stats.Hit = 10;
        target.Stats.Flee = 200; // hitrate clamps to 5%
        target.Stats.Flee2 = 0;
        var calc = new BattleCalculator(new Random(0));

        var missCount = 0;
        for (var i = 0; i < 100; i++)
        {
            var d = calc.CalcWeaponAttack(attacker, target);
            if (!d.DidHit) missCount++;
        }
        Assert.True(missCount > 0, "expected at least one miss with 5% hitrate over 100 swings");
    }

    [Fact]
    public void FireWeapon_Vs_WaterTarget_Lv1_GetsHalvedTo90Percent()
    {
        // attr_fix Lv1 Fire vs Water = 90% (db/re/attr_fix.yml).
        var (attacker, target) = NewPair();
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 1000;
        attacker.Stats.Batk = 0;
        attacker.Stats.WeaponElement = (byte)BattleElement.Fire;
        attacker.Stats.Hit = 200;
        target.Stats.DefenseElement = BattleElement.Water;
        target.Stats.ElementLevel = 1;
        target.Stats.Def = 0; target.Stats.Def2 = 0;
        target.Stats.Flee = 0; target.Stats.Flee2 = 0;
        target.Stats.Luk = 0;
        // Disable crits for the attacker
        attacker.Stats.Cri = 0;

        var calc = new BattleCalculator(new Random(0));
        var d = calc.CalcWeaponAttack(attacker, target);
        // 1000 base × 90% element = 900 (no def reduction with def=0)
        Assert.Equal(900, d.Total);
    }

    [Fact]
    public void WaterWeapon_Vs_FireTarget_Lv1_Boosts150Percent()
    {
        var (attacker, target) = NewPair();
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 1000;
        attacker.Stats.Batk = 0;
        attacker.Stats.WeaponElement = (byte)BattleElement.Water;
        attacker.Stats.Hit = 200;
        attacker.Stats.Cri = 0;
        target.Stats.DefenseElement = BattleElement.Fire;
        target.Stats.ElementLevel = 1;
        target.Stats.Def = 0; target.Stats.Def2 = 0;
        target.Stats.Flee = 0; target.Stats.Flee2 = 0;
        target.Stats.Luk = 0;

        var calc = new BattleCalculator(new Random(0));
        Assert.Equal(1500, calc.CalcWeaponAttack(attacker, target).Total);
    }

    [Fact]
    public void DefenseReduction_Renewal_Formula()
    {
        // RE: damage * (4000 + eDEF) / (4000 + 10*eDEF) - sDEF
        var (attacker, target) = NewPair();
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 1000;
        attacker.Stats.Batk = 0;
        attacker.Stats.WeaponElement = (byte)BattleElement.Neutral;
        attacker.Stats.Hit = 200;
        attacker.Stats.Cri = 0;
        target.Stats.DefenseElement = BattleElement.Neutral;
        target.Stats.ElementLevel = 1;
        target.Stats.Def = 100;  // eDEF
        target.Stats.Def2 = 20;  // sDEF
        target.Stats.Flee = 0; target.Stats.Flee2 = 0;
        target.Stats.Luk = 0;

        var calc = new BattleCalculator(new Random(0));
        var d = calc.CalcWeaponAttack(attacker, target);
        // 1000 * (4000+100) / (4000+1000) - 20 = 1000*4100/5000 - 20 = 820-20 = 800
        Assert.Equal(800, d.Total);
    }

    private static (MobEntity attacker, MobEntity target) NewPair()
    {
        var dbA = new MobDbEntry { Id = 1, AegisName = "A", Name = "A" };
        var dbB = new MobDbEntry { Id = 2, AegisName = "B", Name = "B" };
        var sp = new MobSpawnEntry { MapId = 0, MobClassId = 0 };
        var attacker = new MobEntity(new EntityId(100), dbA, sp, 0, 0, 0);
        var target = new MobEntity(new EntityId(101), dbB, sp, 0, 1, 0);
        // MaxHp/Hp so the damage path doesn't choke before resolution.
        attacker.MaxHp = 10000; attacker.Hp = 10000;
        target.MaxHp = 10000; target.Hp = 10000;
        return (attacker, target);
    }
}
