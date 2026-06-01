using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-06 (first batch) — the damage/defense rate bonuses with clean
/// consumers: bAtkRate / bMatkRate (BattleCalculator) and bDef / bMdef (flat) +
/// bDefRate / bMdefRate (% in CalcPc). rAthena pc.cpp pc_bonus arms.
/// </summary>
public class Combat06BonusTests
{
    // ---- parser ----

    [Theory]
    [InlineData("bonus bAtkRate,10;", "atkrate", 10)]
    [InlineData("bonus bMatkRate,7;", "matkrate", 7)]
    [InlineData("bonus bDef,5;", "def", 5)]
    [InlineData("bonus bMdef,3;", "mdef", 3)]
    [InlineData("bonus bDefRate,-50;", "defrate", -50)]
    [InlineData("bonus bMdefRate,25;", "mdefrate", 25)]
    public void Extractor_captures_rate_bonuses(string script, string which, int expected)
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply(script, b);
        var got = which switch
        {
            "atkrate" => b.AtkRate, "matkrate" => b.MatkRate, "def" => b.FlatDef,
            "mdef" => b.FlatMdef, "defrate" => b.DefRate, "mdefrate" => b.MdefRate,
            _ => int.MinValue,
        };
        Assert.Equal(expected, got);
    }

    // ---- CalcPc: bDef/bMdef flat + bDefRate/bMdefRate percent ----

    [Fact]
    public void Def_flat_then_rate_folds_in_CalcPc()
    {
        var calc = new StatusCalcService();
        var pc = new PlayerEntity(1, 1, "H", System.Guid.NewGuid(), 0, 0, 0);
        pc.EquipBonuses.FlatDef = 10; pc.EquipBonuses.DefRate = 50;   // (base+10)*150%
        pc.EquipBonuses.FlatMdef = 5; pc.EquipBonuses.MdefRate = 0;

        calc.CalcPc(pc, Inputs(equipDef: 50, equipMdef: 20));
        Assert.Equal((50 + 10) * 150 / 100, pc.Stats.Def); // 90
        Assert.Equal(20 + 5, pc.Stats.Mdef);               // 25

        // idempotent across a second recalc
        calc.CalcPc(pc, Inputs(equipDef: 50, equipMdef: 20));
        Assert.Equal(90, pc.Stats.Def);
        Assert.Equal(25, pc.Stats.Mdef);
    }

    // ---- BattleCalculator: bAtkRate (weapon) / bMatkRate (magic) ----

    [Fact]
    public void AtkRate_scales_weapon_damage()
    {
        var calc = new BattleCalculator(new System.Random(0));
        var pc = WeaponPc(atkRate: 20);          // +20%
        var tgt = NeutralTarget();
        // base swing pinned to 100 (Dex high, capped at WatkMax=100, no roll).
        Assert.Equal(120, calc.CalcWeaponAttack(pc, tgt).Damage);

        var pc0 = WeaponPc(atkRate: 0);
        Assert.Equal(100, calc.CalcWeaponAttack(pc0, tgt).Damage);
    }

    [Fact]
    public void MatkRate_scales_magic_damage()
    {
        var calc = new BattleCalculator(new System.Random(0));
        var src = new PlayerEntity(1, 1, "H", System.Guid.NewGuid(), 0, 0, 0);
        src.Stats.MatkMin = src.Stats.MatkMax = 100; // base 100
        src.EquipBonuses.MatkRate = 20;
        var tgt = NeutralTarget();
        Assert.Equal(120, calc.CalcMagicAttack(src, tgt, 0, 1, ratePerLevel: 100).Damage);
    }

    // ---- helpers ----

    private static PcBaseInputs Inputs(int equipDef, int equipMdef) => new(
        BaseLevel: 1, JobLevel: 1, Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 0, EquipDef: equipDef, EquipMdef: equipMdef, AttackRange: 1);

    private static PlayerEntity WeaponPc(int atkRate)
    {
        var pc = new PlayerEntity(1, 1, "H", System.Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Dex = 1000; pc.Stats.WeaponLevel = 0; // atkmin capped at WatkMax
        pc.Stats.WatkMin = 0; pc.Stats.WatkMax = 100;  // base swing = 100
        pc.Stats.Batk = 0; pc.Stats.Cri = 0; pc.Stats.Hit = 10000;
        pc.EquipBonuses.AtkRate = atkRate;
        return pc;
    }

    private static MobEntity NeutralTarget()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "M", Name = "M", Hp = 5000 };
        var m = new MobEntity(new EntityId(900), db, new MobSpawnEntry { MapId = 0, MobClassId = 1002 }, 0, 0, 0);
        m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Mdef = 0; m.Stats.Mdef2 = 0;
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Size = BattleSize.Medium; m.Stats.Flee = 0;
        return m;
    }
}
