using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-04 (axis 1) — renewal PC base-damage DEX floor
/// (battle.cpp:2453 battle_calc_base_damage): atkmin = DEX, then
/// ×(80 + weaponLv*20)/100 when a weapon is equipped, capped at atkmax.
/// </summary>
public class Combat04BaseDamageTests
{
    [Theory]
    [InlineData(50, 0, 1000, 50)]   // bare-handed: atkmin = DEX
    [InlineData(50, 1, 1000, 50)]   // 50 × 100/100
    [InlineData(50, 2, 1000, 60)]   // 50 × 120/100
    [InlineData(50, 3, 1000, 70)]   // 50 × 140/100
    [InlineData(50, 4, 1000, 80)]   // 50 × 160/100  (ticket's worked example)
    [InlineData(120, 4, 1000, 192)] // 120 × 160/100
    [InlineData(50, 4, 70, 70)]     // capped at atkMax (= rhw.atk)
    public void ComputePcAtkMin_matches_rAthena(int dex, int weaponLv, int atkMax, int expected)
        => Assert.Equal(expected, BattleCalculator.ComputePcAtkMin(dex, weaponLv, atkMax));

    [Fact]
    public void WeaponLevel_reaches_BattleStats_via_CalcPc()
    {
        var calc = new StatusCalcService();
        var pc = new PlayerEntity(1, 1, "Hero", System.Guid.NewGuid(), 0, 0, 0);
        calc.CalcPc(pc, new PcBaseInputs(
            BaseLevel: 1, JobLevel: 1,
            Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
            AttackRange: 1, WeaponLevel: 4));
        Assert.Equal(4, pc.Stats.WeaponLevel);
    }

    [Theory]
    [InlineData(50, 0, 50)]  // bare hand → DEX
    [InlineData(50, 4, 80)]  // lv4 weapon → 80
    [InlineData(50, 2, 60)]
    public void Pc_weapon_swing_floor_is_dex_derived(int dex, int weaponLv, int expected)
    {
        // Pin atkMax == expected atkMin so there is no roll → the swing equals
        // the DEX-derived floor deterministically. Neutral target, def 0, no
        // crit, Batk 0 → CalcWeaponAttack.Damage == atkMin.
        var calc = new BattleCalculator(new System.Random(0));
        var pc = new PlayerEntity(1, 1, "Hero", System.Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Dex = (short)dex;
        pc.Stats.WeaponLevel = (byte)weaponLv;
        pc.Stats.WatkMin = 0;
        pc.Stats.WatkMax = (ushort)expected; // == computed atkMin → no roll
        pc.Stats.Batk = 0;
        pc.Stats.Cri = 0;       // no critical
        pc.Stats.Hit = 10000;   // always hits

        var tgt = MakeMob(2002);
        tgt.Stats.Def = 0; tgt.Stats.Def2 = 0;
        tgt.Stats.DefenseElement = BattleElement.Neutral; tgt.Stats.ElementLevel = 1;
        tgt.Stats.Size = BattleSize.Medium;
        tgt.Stats.Flee = 0; // always hits

        var dmg = calc.CalcWeaponAttack(pc, tgt);
        Assert.Equal(expected, dmg.Damage);
    }

    private static MobEntity MakeMob(int id)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        return new MobEntity(new EntityId(id), db, origin, mapId: 0, x: 0, y: 0);
    }
}
