using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

public class ExpServiceTests
{
    [Fact]
    public void NextBaseExp_Lv1_Is9()
    {
        // db/re/exp.txt Novice group 0 row 1.
        Assert.Equal(9, ExpTable.NextBaseExp(1));
    }

    [Fact]
    public void GainExp_BelowThreshold_NoLevelUp()
    {
        var (svc, pc) = NewService();
        pc.Level = 1;
        pc.BaseExp = 0;

        var leveled = svc.GainExp(pc, baseExp: 5, jobExp: 0);

        Assert.False(leveled);
        Assert.Equal(1, pc.Level);
        Assert.Equal(5, pc.BaseExp);
    }

    [Fact]
    public void GainExp_OverThreshold_TriggersLevelUp_AwardsStatusPoints_FullHeals()
    {
        var (svc, pc) = NewService();
        pc.Level = 1;
        pc.BaseExp = 0;
        pc.StatusPoints = 48; // rAthena Novice start
        pc.Hp = 1;
        pc.Sp = 1;

        var leveled = svc.GainExp(pc, baseExp: 20, jobExp: 0);

        Assert.True(leveled);
        Assert.Equal(2, pc.Level);
        // 20 - 9 (next at lv1) = 11 carry-over; rAthena's over-carry cap
        // is `next-1` where `next` is the OLD level's threshold (9), so
        // 11 clamps to 8. Matches pc.cpp:8118.
        Assert.Equal(8, pc.BaseExp);
        // Status points: +3 + level/5 = 3 + 0 = 3 → 51.
        Assert.Equal(51, pc.StatusPoints);
        // Full heal on level up.
        Assert.Equal(pc.MaxHp, pc.Hp);
        Assert.Equal(pc.MaxSp, pc.Sp);
    }

    [Fact]
    public void GainExp_MultiLevelUp_OneCall_DoesAtMostOneLevel_Default()
    {
        // rAthena default battle_config.multi_level_up = false → one
        // level-up per call, even with excess EXP.
        var (svc, pc) = NewService();
        pc.Level = 1;
        pc.BaseExp = 0;

        svc.GainExp(pc, baseExp: 1_000_000, jobExp: 0);

        Assert.Equal(2, pc.Level);
        // Over-carry cap → 8 (next-1 for lv1's threshold).
        Assert.Equal(8, pc.BaseExp);
    }

    [Fact]
    public void GainExp_JobLevelUp()
    {
        var (svc, pc) = NewService();
        pc.JobLevel = 1;
        pc.JobExp = 0;
        pc.SkillPoints = 0;

        var leveled = svc.GainExp(pc, baseExp: 0, jobExp: 15);

        Assert.True(leveled);
        Assert.Equal(2, pc.JobLevel);
        Assert.Equal(1, pc.SkillPoints);
    }

    [Fact]
    public void GainExp_AtMaxBaseLevel_NoOp()
    {
        var (svc, pc) = NewService();
        pc.Level = ExpTable.MaxBaseLevel;
        pc.BaseExp = 0;

        svc.GainExp(pc, baseExp: 1_000_000, jobExp: 0);

        Assert.Equal(ExpTable.MaxBaseLevel, pc.Level);
    }

    private static (ExpService Service, PlayerEntity Pc) NewService()
    {
        var calc = new StatusCalcService();
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        calc.CalcPc(pc, new PcBaseInputs(
            BaseLevel: 1, JobLevel: 1,
            Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 17, WeaponAtkMax: 17,
            EquipDef: 10, EquipMdef: 0,
            AttackRange: 1));
        var svc = new ExpService(calc, new NoSessions(), NullLogger<ExpService>.Instance);
        return (svc, pc);
    }

    private sealed class NoSessions : ISessionManagerAccessor
    {
        public Map.Server.MapSessionData? GetByEntityId(EntityId entityId) => null;
    }
}
