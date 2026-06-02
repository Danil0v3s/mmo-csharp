using System;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-32 — absolute base-stat addends from passive skills + Super Novice
/// all-stat +10 (rAthena status_calc_pc_, status.cpp:4221-4241). These layer
/// into base_status alongside the job bonus, before the allocated/card/equip
/// fold, so they raise the final stat and its derived hit/atk/matk.
/// </summary>
public class Combat32PassiveBaseStatTests
{
    private const int Hunter = 11;
    private const int SuperNovice = 23;
    private const int Sage = 16;
    private const int Ranger = 4056; // JOB_RANGER — but the addend is purely skill-gated
    private const int Blacksmith = 10;
    private const int Summoner = 4218;

    [Fact]
    public void AcOwl_adds_dex_per_level_and_raises_hit()
    {
        var baseStats = Calc(Hunter, jobLevel: 50, dieCounter: 0);
        var owlStats = Calc(Hunter, jobLevel: 50, dieCounter: 0,
            skills: (SkillIds.AC_OWL, 10));

        Assert.Equal(baseStats.Dex + 10, owlStats.Dex); // +lv DEX
        Assert.True(owlStats.Hit > baseStats.Hit);       // derived Hit rises with DEX
    }

    [Fact]
    public void SuperNovice_job70_neverdied_gets_all_stats_plus10()
    {
        var baseStats = Calc(SuperNovice, jobLevel: 69, dieCounter: 0); // gate not met
        var snStats = Calc(SuperNovice, jobLevel: 70, dieCounter: 0);   // gate met

        Assert.Equal(baseStats.Str + 10, snStats.Str);
        Assert.Equal(baseStats.Agi + 10, snStats.Agi);
        Assert.Equal(baseStats.Vit + 10, snStats.Vit);
        Assert.Equal(baseStats.IntStat + 10, snStats.IntStat);
        Assert.Equal(baseStats.Dex + 10, snStats.Dex);
        Assert.Equal(baseStats.Luk + 10, snStats.Luk);
    }

    [Fact]
    public void SuperNovice_that_has_died_gets_no_bonus()
    {
        var neverDied = Calc(SuperNovice, jobLevel: 99, dieCounter: 0);
        var died = Calc(SuperNovice, jobLevel: 99, dieCounter: 1);
        Assert.Equal(neverDied.Str - 10, died.Str); // died loses the +10
    }

    [Fact]
    public void Non_supernovice_at_job70_gets_no_allstat_bonus()
    {
        // A Hunter at job 70 is not a Super Novice → no +10.
        var hunter = Calc(Hunter, jobLevel: 70, dieCounter: 0);
        var noviceBaseline = Calc(0, jobLevel: 70, dieCounter: 0);
        Assert.Equal(noviceBaseline.Str, hunter.Str);
    }

    [Fact]
    public void Dragonology_adds_half_level_rounded_up_int()
    {
        var none = Calc(Sage, jobLevel: 50, dieCounter: 0);
        var lv5 = Calc(Sage, jobLevel: 50, dieCounter: 0, skills: (SkillIds.SA_DRAGONOLOGY, 5));
        Assert.Equal(none.IntStat + 3, lv5.IntStat); // (5+1)/2 = 3
    }

    [Fact]
    public void HiltBinding_adds_one_str()
    {
        var none = Calc(Blacksmith, jobLevel: 50, dieCounter: 0);
        var bound = Calc(Blacksmith, jobLevel: 50, dieCounter: 0, skills: (SkillIds.BS_HILTBINDING, 1));
        Assert.Equal(none.Str + 1, bound.Str);
    }

    [Fact]
    public void ResearchTrap_adds_level_int()
    {
        var none = Calc(Ranger, jobLevel: 50, dieCounter: 0);
        var rt = Calc(Ranger, jobLevel: 50, dieCounter: 0, skills: (SkillIds.RA_RESEARCHTRAP, 10));
        Assert.Equal(none.IntStat + 10, rt.IntStat);
    }

    [Fact]
    public void PowerOfLand_adds_twenty_int()
    {
        var none = Calc(Summoner, jobLevel: 50, dieCounter: 0);
        var pol = Calc(Summoner, jobLevel: 50, dieCounter: 0, skills: (SkillIds.SU_POWEROFLAND, 1));
        Assert.Equal(none.IntStat + 20, pol.IntStat);
    }

    [Fact]
    public void Addends_are_idempotent_across_repeated_recalc()
    {
        // Two recalcs on the same entity must not double-count the addends.
        var calc = new StatusCalcService();
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.LearnedSkills[SkillIds.AC_OWL] = 10;
        var inputs = Inputs(Hunter, jobLevel: 50);

        calc.CalcPc(pc, inputs);
        var firstDex = pc.Stats.Dex;
        calc.CalcPc(pc, inputs);
        var secondDex = pc.Stats.Dex;

        Assert.Equal(firstDex, secondDex);
    }

    private static BattleStats Calc(int jobId, int jobLevel, int dieCounter,
        params (ushort id, byte lv)[] skills)
    {
        var calc = new StatusCalcService(); // no job-stats cache → deterministic base
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0) { DieCounter = dieCounter };
        foreach (var (id, lv) in skills) pc.LearnedSkills[id] = lv;
        calc.CalcPc(pc, Inputs(jobId, jobLevel));
        return pc.Stats;
    }

    private static PcBaseInputs Inputs(int jobId, int jobLevel) => new(
        BaseLevel: 99, JobLevel: jobLevel,
        Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
        AttackRange: 1, JobId: jobId);
}
