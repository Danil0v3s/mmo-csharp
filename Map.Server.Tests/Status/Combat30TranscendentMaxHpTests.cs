using System;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-30 — transcendent (JOBL_UPPER ×1.25) + Taekwon-ranker (×3) MaxHP/SP.
/// rAthena status_calc_maxhp_pc / maxsp_pc (status.cpp:3479), applied after the
/// VIT/INT scale and before the flat/rate equip fold.
/// </summary>
public class Combat30TranscendentMaxHpTests
{
    private const int LordKnight = 4008; // transcendent 2nd class
    private const int Knight = 7;        // regular 2nd class (not transcendent)
    private const int Taekwon = 4046;

    [Fact]
    public void Transcendent_gets_125_percent_maxhp_and_maxsp()
    {
        var (baseHp, baseSp) = Pools(Knight, level: 99, ranker: false);
        var (transHp, transSp) = Pools(LordKnight, level: 99, ranker: false);

        Assert.Equal(baseHp * 125 / 100, transHp);
        Assert.Equal(baseSp * 125 / 100, transSp);
    }

    [Fact]
    public void Taekwon_ranker_gets_triple_maxhp()
    {
        var (baseHp, _) = Pools(Taekwon, level: 99, ranker: false);
        var (rankerHp, _) = Pools(Taekwon, level: 99, ranker: true);
        Assert.Equal(baseHp * 3, rankerHp);
    }

    [Fact]
    public void Taekwon_ranker_below_level_90_gets_no_bonus()
    {
        var (baseHp, _) = Pools(Taekwon, level: 89, ranker: false);
        var (rankerHp, _) = Pools(Taekwon, level: 89, ranker: true);
        Assert.Equal(baseHp, rankerHp); // gated on base level >= 90
    }

    [Fact]
    public void Non_transcendent_is_unchanged()
    {
        // A regular Knight (2nd class but not transcendent) gets no multiplier.
        var novice = Pools(0, level: 50, ranker: false);
        var knight = Pools(Knight, level: 50, ranker: false);
        Assert.Equal(novice.hp, knight.hp); // both fall back to the Novice base, no ×1.25
    }

    [Fact]
    public void Setting_classid_populates_classmask()
    {
        // COMBAT-30 latent fix: ClassMask is derived from ClassId on set.
        var pc = new PlayerEntity(1, 1, "Sin", Guid.NewGuid(), 0, 0, 0) { ClassId = 12 }; // JOB_ASSASSIN
        Assert.Equal(MapidClass.Assassin, pc.ClassMask);
    }

    private static (int hp, int sp) Pools(int jobId, int level, bool ranker)
    {
        var calc = new StatusCalcService(); // no job-stats cache → deterministic Novice base
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { IsTaekwonRanker = ranker };
        calc.CalcPc(pc, new PcBaseInputs(
            BaseLevel: level, JobLevel: 50,
            Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
            AttackRange: 1, JobId: jobId));
        return (pc.Stats.MaxHp, pc.Stats.MaxSp);
    }
}
