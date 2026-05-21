using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Mob.Conditions;
using Map.Server.Spawn;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.2 — unit tests for each <see cref="IMobSkillConditionEvaluator"/>.
/// Each test feeds a synthetic <see cref="MobEntity"/> + entry +
/// <see cref="MobConditionContext"/> and asserts the IsMet decision.
///
/// <para>Catches regressions where adding a new MSC_* value renumbers
/// the existing ones — exactly the bug we fixed when porting the full
/// enum table off the 5-value partial.</para>
/// </summary>
public class MobConditionEvaluatorsTests
{
    [Fact]
    public void Always_AlwaysTrue()
    {
        var mob = MakeMob(hp: 100, max: 1000);
        var entry = MakeEntry(MobSkillCondition.Always);
        Assert.True(new AlwaysCondition().IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Theory]
    [InlineData(50, 100, 50, true)]   // hp% = 50, ≤ 50 → true
    [InlineData(49, 100, 50, true)]   // hp% = 49, ≤ 50 → true
    [InlineData(51, 100, 50, false)]  // hp% = 51 → false
    public void MyHpLessThanRate(int hp, int max, int cond, bool expected)
    {
        var mob = MakeMob(hp, max);
        var entry = MakeEntry(MobSkillCondition.MyHpLessThanRate, cond2: cond);
        Assert.Equal(expected, new MyHpLessThanRateCondition().IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Theory]
    [InlineData(50, 100, 25, 75, true)]   // 50% in [25, 75]
    [InlineData(25, 100, 25, 75, true)]   // boundary low
    [InlineData(75, 100, 25, 75, true)]   // boundary high
    [InlineData(20, 100, 25, 75, false)]  // below
    [InlineData(80, 100, 25, 75, false)]  // above
    public void MyHpInRate(int hp, int max, int low, int high, bool expected)
    {
        var mob = MakeMob(hp, max);
        var entry = MakeEntry(MobSkillCondition.MyHpInRate, cond2: low, val1: high);
        Assert.Equal(expected, new MyHpInRateCondition().IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Fact]
    public void CloseAttacked_FiresOnlyOnMeleeHit()
    {
        var ev = new CloseAttackedCondition();
        var entry = MakeEntry(MobSkillCondition.CloseAttacked);
        var mob = MakeMob(100, 100);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { RecentMelee = true }));
        Assert.False(ev.IsMet(mob, entry, new MobConditionContext { RecentRanged = true }));
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Fact]
    public void LongRangeAttacked_FiresOnlyOnRangedHit()
    {
        var ev = new LongRangeAttackedCondition();
        var entry = MakeEntry(MobSkillCondition.LongRangeAttacked);
        var mob = MakeMob(100, 100);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { RecentRanged = true }));
        Assert.False(ev.IsMet(mob, entry, new MobConditionContext { RecentMelee = true }));
    }

    [Fact]
    public void GroundAttacked_FiresOnlyOnGroundUnit()
    {
        var ev = new GroundAttackedCondition();
        var entry = MakeEntry(MobSkillCondition.GroundAttacked);
        var mob = MakeMob(100, 100);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { RecentGroundHit = true }));
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Fact]
    public void SkillUsed_MatchesByCond2()
    {
        var ev = new SkillUsedCondition();
        var entry = MakeEntry(MobSkillCondition.SkillUsed, cond2: 28); // AL_HEAL
        var mob = MakeMob(100, 100);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { SkillUsedNearby = 28 }));
        Assert.False(ev.IsMet(mob, entry, new MobConditionContext { SkillUsedNearby = 14 }));  // MG_SOULSTRIKE
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Fact]
    public void CastTargeted_Mirrors_ContextFlag()
    {
        var ev = new CastTargetedCondition();
        var entry = MakeEntry(MobSkillCondition.CastTargeted);
        var mob = MakeMob(100, 100);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { CastTargeted = true }));
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Theory]
    [InlineData(1000, 500, true)]
    [InlineData(500, 500, false)]
    [InlineData(0, 500, false)]
    public void DamagedGreater(int dmg, int cond, bool expected)
    {
        var ev = new DamagedGreaterCondition();
        var entry = MakeEntry(MobSkillCondition.DamagedGreater, cond2: cond);
        var mob = MakeMob(100, 100);
        Assert.Equal(expected,
            ev.IsMet(mob, entry, new MobConditionContext { CumulativeDamageTaken = dmg }));
    }

    [Fact]
    public void RudeAttacked_FiresAtThreshold()
    {
        var ev = new RudeAttackedCondition();
        var entry = MakeEntry(MobSkillCondition.RudeAttacked);
        var mob = MakeMob(100, 100);
        mob.RudeAttackedCount = RudeAttackedCondition.DefaultThreshold - 1;
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
        mob.RudeAttackedCount = RudeAttackedCondition.DefaultThreshold;
        Assert.True(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    // --- helpers ---

    private static MobEntity MakeMob(int hp, int max)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = max };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(1), db, origin, mapId: 1, x: 0, y: 0);
        mob.MaxHp = max;
        mob.Hp = hp;
        return mob;
    }

    private static MobSkillEntry MakeEntry(
        MobSkillCondition cond,
        int cond2 = 0,
        int val1 = 0,
        ushort skill = 1)
        => new()
        {
            SkillId = skill,
            SkillLevel = 1,
            State = MobSkillState.Any,
            Condition = cond,
            Cond2 = cond2,
            Val1 = val1,
        };
}
