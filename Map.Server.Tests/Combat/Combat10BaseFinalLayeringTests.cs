using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-10 — base→final stat layering. Verifies the equip param bonus +
/// job bonus fold onto the persisted base, that repeated recalc is
/// idempotent (no double-count), that an SC stat mod sitting on top of the
/// param base survives a recalc, and that the renewal BaseAtk / Hit derive
/// from the final (post-fold) stat. rAthena status.cpp:4205-4266.
/// </summary>
public class Combat10BaseFinalLayeringTests
{
    private static PlayerEntity NewPc()
        => new(1, 1, "Hero", System.Guid.NewGuid(), mapId: 0, x: 0, y: 0);

    // Lv1 with the base passed via inputs (as the recalc-input builders do
    // from PlayerEntity.BaseParams). Str/Dex carried so the derived-stat
    // assertions have something to move.
    private static PcBaseInputs BaseInputs(int str = 1, int dex = 1) => new(
        BaseLevel: 1, JobLevel: 1,
        Str: str, Agi: 1, Vit: 1, Int: 1, Dex: dex, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 17, WeaponAtkMax: 17,
        EquipDef: 10, EquipMdef: 0, AttackRange: 1);

    // ---- Criterion 1: bStr,10 raises STR + Batk; bDex,10 raises Hit + Batk ----

    [Fact]
    public void EquipParam_bStr_raisesStrAndBaseAtk()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        // Baseline (no gear).
        calc.CalcPc(pc, BaseInputs());
        var batk0 = pc.Stats.Batk;

        // +10 STR card.
        var pc2 = NewPc();
        pc2.EquipBonuses.Str = 10;
        calc.CalcPc(pc2, BaseInputs());

        Assert.Equal(11, pc2.Stats.Str);          // 1 + 10
        Assert.True(pc2.Stats.Batk > batk0);      // renewal BaseAtk includes STR
    }

    [Fact]
    public void EquipParam_bDex_raisesHitAndBaseAtk()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        calc.CalcPc(pc, BaseInputs());
        var hit0 = pc.Stats.Hit;       // 177
        var batk0 = pc.Stats.Batk;

        var pc2 = NewPc();
        pc2.EquipBonuses.Dex = 10;
        calc.CalcPc(pc2, BaseInputs());

        Assert.Equal(11, pc2.Stats.Dex);
        // Hit = level + dex + luk/3 + 175 → +10 dex → +10 hit.
        Assert.Equal(hit0 + 10, pc2.Stats.Hit);
        Assert.True(pc2.Stats.Batk > batk0); // DEX term in renewal BaseAtk
    }

    // ---- Criterion 2: idempotency — no double-count across recalcs ----

    [Fact]
    public void EquipParam_isIdempotent_acrossRepeatedRecalc()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        pc.EquipBonuses.Str = 10;

        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(11, pc.Stats.Str);

        // Three more recalcs with the SAME base + bundle must not stack.
        calc.CalcPc(pc, BaseInputs());
        calc.CalcPc(pc, BaseInputs());
        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(11, pc.Stats.Str);
    }

    [Fact]
    public void EquipParam_changingCard_appliesDeltaNotSum()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        pc.EquipBonuses.Str = 10;
        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(11, pc.Stats.Str);

        // Swap to a +20 card and recalc — final must be 21, not 11+20.
        pc.EquipBonuses.Str = 20;
        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(21, pc.Stats.Str);

        // Unequip — back to base.
        pc.EquipBonuses.Str = 0;
        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(1, pc.Stats.Str);
    }

    // ---- Criterion 3: SC stat mod survives a recalc ----

    [Fact]
    public void ScStatMod_survivesRecalc()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        calc.CalcPc(pc, BaseInputs());          // base STR 1
        Assert.Equal(1, pc.Stats.Str);

        // Simulate a Blessing-style SC: handlers mutate Stats.Str directly.
        pc.Stats.Str += 10;
        Assert.Equal(11, pc.Stats.Str);

        // A recalc (level-up / equip / stat-alloc) must NOT wipe the buff.
        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(11, pc.Stats.Str);         // 1 base + 10 SC preserved

        // And the buff coexists with an equip param fold on the same recalc.
        pc.EquipBonuses.Str = 5;
        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(16, pc.Stats.Str);         // 1 base + 5 equip + 10 SC

        // SC ends (handler reverts its own delta).
        pc.Stats.Str -= 10;
        calc.CalcPc(pc, BaseInputs());
        Assert.Equal(6, pc.Stats.Str);          // 1 base + 5 equip
    }

    // ---- Criterion 4: job-bonus stats apply per job + job level ----

    [Fact]
    public void JobBonus_appliesPerJobAndLevel()
    {
        // Fake catalog returns a known job-bonus sum; the only thing under
        // test is that CalcPc layers it onto the base + preserves idempotency.
        var jobStats = new FakeJobStats(new JobBonusStatsSum(
            Str: 5, Agi: 0, Vit: 3, Int: 0, Dex: 2, Luk: 0,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0));
        var calc = new StatusCalcService(jobStats: jobStats);
        var pc = NewPc();
        // JobId must be a class JobAegisMapper can resolve (Swordman = 1).
        var inputs = BaseInputs() with { JobId = 1, JobLevel = 50 };

        calc.CalcPc(pc, inputs);
        Assert.Equal(1 + 5, pc.Stats.Str);   // base 1 + job 5
        Assert.Equal(1 + 3, pc.Stats.Vit);
        Assert.Equal(1 + 2, pc.Stats.Dex);

        // Idempotent: re-calc doesn't stack the job bonus.
        calc.CalcPc(pc, inputs);
        Assert.Equal(6, pc.Stats.Str);

        // Job bonus + equip param + SC all coexist.
        pc.EquipBonuses.Str = 4;
        pc.Stats.Str += 7; // SC
        calc.CalcPc(pc, inputs);
        Assert.Equal(1 + 5 + 4 + 7, pc.Stats.Str);
    }

    private sealed class FakeJobStats : IJobStatsCacheService
    {
        private readonly JobBonusStatsSum _sum;
        public FakeJobStats(JobBonusStatsSum sum) => _sum = sum;
        public int GetBaseHp(string jobAegis, int level) => 0;       // → Novice fallback
        public int GetBaseSp(string jobAegis, int level) => 0;
        public Core.Database.Entities.JobInfoDbEntity? GetJobInfo(string jobAegis) => null;
        public int GetMaxBaseLevel(string jobAegis) => 99;
        public int GetMaxJobLevel(string jobAegis) => 50;
        public JobBonusStatsSum GetBonusSum(string jobAegis, int currentLevel) => _sum;
    }
}
