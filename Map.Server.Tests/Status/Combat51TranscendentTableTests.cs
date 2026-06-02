using System;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-51 — the full transcendent (JOBL_UPPER) job-id set drives the ×1.25
/// MaxHP/SP bonus, including trans-3rd (_T / _T2) and 4th classes. rAthena
/// status_calc_maxhp_pc: <c>if (class_ &amp; JOBL_UPPER) dmax *= 1.25</c>.
/// </summary>
public class Combat51TranscendentTableTests
{
    [Theory]
    // trans 1st/2nd
    [InlineData(4001, true)]   // Novice High
    [InlineData(4008, true)]   // Lord Knight
    [InlineData(4022, true)]   // last trans-2nd
    // trans-3rd _T
    [InlineData(4060, true)]   // Rune Knight_T
    [InlineData(4065, true)]   // Guillotine Cross_T
    [InlineData(4073, true)]   // Royal Guard_T
    [InlineData(4079, true)]   // Shadow Chaser_T
    // trans-3rd _T2 (mounted)
    [InlineData(4081, true)]   // Rune Knight_T2
    [InlineData(4087, true)]   // Mechanic_T2
    // 4th classes (all carry JOBL_UPPER)
    [InlineData(4252, true)]   // Dragon Knight
    [InlineData(4264, true)]   // Trouvere
    [InlineData(4280, true)]   // Dragon Knight2
    [InlineData(4302, true)]   // Sky Emperor
    [InlineData(4316, true)]   // Sky Emperor 2
    // NOT transcendent
    [InlineData(7, false)]     // Knight (regular 2nd)
    [InlineData(4023, false)]  // baby class
    [InlineData(4046, false)]  // Taekwon (expanded, not trans)
    [InlineData(4054, false)]  // Rune Knight (non-trans 3rd)
    [InlineData(4072, false)]  // Shadow Chaser (non-trans 3rd)
    [InlineData(4080, false)]  // Rune Knight2 (non-trans mounted)
    [InlineData(4096, false)]  // Baby Rune Knight (JOBL_BABY, not UPPER)
    public void IsTranscendent_matches_the_JOBL_UPPER_set(int jobId, bool expected)
        => Assert.Equal(expected, JobAegisMapper.IsTranscendent(jobId));

    [Theory]
    [InlineData(4060)] // trans-3rd Rune Knight_T
    [InlineData(4252)] // 4th Dragon Knight
    public void Transcendent_3rd_and_4th_get_125_percent(int transJobId)
    {
        var (baseHp, baseSp) = Pools(jobId: 7);       // regular Knight → no multiplier
        var (transHp, transSp) = Pools(jobId: transJobId);
        Assert.Equal(baseHp * 125 / 100, transHp);
        Assert.Equal(baseSp * 125 / 100, transSp);
    }

    [Fact]
    public void Non_trans_third_class_gets_no_multiplier()
    {
        // A non-trans 3rd class (Rune Knight 4054) must NOT get ×1.25.
        var (knightHp, _) = Pools(jobId: 7);
        var (runeHp, _) = Pools(jobId: 4054);
        Assert.Equal(knightHp, runeHp);
    }

    private static (int hp, int sp) Pools(int jobId)
    {
        var calc = new StatusCalcService(); // no job-stats cache → deterministic Novice base
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        calc.CalcPc(pc, new PcBaseInputs(
            BaseLevel: 99, JobLevel: 50,
            Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
            AttackRange: 1, JobId: jobId));
        return (pc.Stats.MaxHp, pc.Stats.MaxSp);
    }
}
