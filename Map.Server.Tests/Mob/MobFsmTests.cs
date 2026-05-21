using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.8a — FSM transition rules from rAthena <c>mob_setstate</c>
/// (mob.cpp:1820). Two transitions swap based on the mob's
/// <see cref="MobMode.Angry"/> bit; all others write through.
/// </summary>
public class MobFsmTests
{
    [Fact]
    public void TransitionTo_Berserk_NonAggressive_StaysBerserk()
    {
        var mob = MakeMob(angry: false);
        MobFsm.TransitionTo(mob, MobSkillState.Berserk);
        Assert.Equal(MobSkillState.Berserk, mob.SkillState);
    }

    [Fact]
    public void TransitionTo_Berserk_Aggressive_BecomesAngry()
    {
        var mob = MakeMob(angry: true);
        MobFsm.TransitionTo(mob, MobSkillState.Berserk);
        Assert.Equal(MobSkillState.Angry, mob.SkillState);
    }

    [Fact]
    public void TransitionTo_Rush_NonAggressive_StaysRush()
    {
        var mob = MakeMob(angry: false);
        MobFsm.TransitionTo(mob, MobSkillState.Rush);
        Assert.Equal(MobSkillState.Rush, mob.SkillState);
    }

    [Fact]
    public void TransitionTo_Rush_Aggressive_BecomesFollow()
    {
        var mob = MakeMob(angry: true);
        MobFsm.TransitionTo(mob, MobSkillState.Rush);
        Assert.Equal(MobSkillState.Follow, mob.SkillState);
    }

    [Theory]
    [InlineData(MobSkillState.Idle)]
    [InlineData(MobSkillState.Walk)]
    [InlineData(MobSkillState.Loot)]
    [InlineData(MobSkillState.Dead)]
    [InlineData(MobSkillState.AnyTarget)]
    public void TransitionTo_NonCombatState_WritesThrough(MobSkillState target)
    {
        var mob = MakeMob(angry: true);
        MobFsm.TransitionTo(mob, target);
        Assert.Equal(target, mob.SkillState);
    }

    private static MobEntity MakeMob(bool angry)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "M", Name = "M", Hp = 1000 };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(1), db, origin, mapId: 1, x: 0, y: 0);
        new StatusCalcService().CalcMob(mob);
        if (angry) mob.Stats.Mode |= MobMode.Angry;
        return mob;
    }
}
