using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-79 — the DEF subtraction (battle_calc_defense_reduction) is rAthena's LAST physical step
/// (battle.cpp:7862), AFTER patk / mastery / Res. Previously the C# applied it early; verify the
/// reorder so patk and Res operate on the pre-DEF value.
///
/// Fixture pins base per-hand damage to 100 (bare weapon roll, Batk 0, neutral, no crit).
/// DEF curve: <c>dmg * (4000+eDEF)/(4000+10*eDEF) - sDEF</c>.
/// </summary>
public class Combat79DefAtEndTests
{
    [Fact]
    public void Def_with_no_other_terms_matches_the_renewal_curve()
    {
        // base 100, eDEF 100, sDEF 20 → 100 * 4100/5000 - 20 = 82 - 20 = 62.
        var pc = MakeSwinger();
        var target = MakeTarget(); target.Stats.Def = 100; target.Stats.Def2 = 20;
        Assert.Equal(62, Swing(pc, target));
    }

    [Fact]
    public void Patk_is_applied_before_def()
    {
        // OLD (def early): 100 → def 62 → patk 93.   NEW (def last): 100 → patk 150 → def
        // 150*4100/5000 - 20 = 123 - 20 = 103.
        var pc = MakeSwinger(); pc.Stats.Patk = 50;
        var target = MakeTarget(); target.Stats.Def = 100; target.Stats.Def2 = 20;
        Assert.Equal(103, Swing(pc, target));
    }

    [Fact]
    public void Res_is_applied_before_def()
    {
        // base 100 → Res 100: 100*5100/6000 = 85 → def last: 85*4100/5000 - 20 = 69 - 20 = 49.
        var pc = MakeSwinger();
        var target = MakeTarget(); target.Stats.Def = 100; target.Stats.Def2 = 20; target.Stats.Res = 100;
        Assert.Equal(49, Swing(pc, target));
    }

    // ---- helpers (mirror COMBAT-61) ----

    private static long Swing(PlayerEntity pc, MobEntity target)
        => new BattleCalculator(rng: new ZeroRandom()).CalcWeaponAttack(pc, target).Damage;

    private static PlayerEntity MakeSwinger()
    {
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { WeaponType = 0 };
        pc.Stats.WeaponLevel = 0;
        pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100;
        pc.Stats.Dex = 100;
        pc.Stats.Batk = 0; pc.Stats.Cri = 0; pc.Stats.Hit = 10000;
        pc.Stats.Patk = 0;
        pc.Stats.WeaponElement = 0;
        return pc;
    }

    private static MobEntity MakeTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Res = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0; m.Stats.Flee2 = 0; m.Stats.Luk = 0;
        return m;
    }

    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
