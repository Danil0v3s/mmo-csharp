using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.9d — unit tests for <see cref="MobChangeTargetService"/>.
/// rAthena <c>mob_can_changetarget</c> (mob.cpp:1229) — the gate
/// matrix that decides whether a mob may re-aim from its current
/// target to a new attacker.
/// </summary>
public class MobChangeTargetTests
{
    [Fact]
    public void Berserk_WithoutChangeTargetMelee_Refuses()
    {
        // Engaged melee + no MD_CHANGETARGETMELEE — refuse.
        var mob = MakeMob(mode: 0, state: MobSkillState.Berserk, currentTargetId: 99);
        var newTarget = MakePc(id: 7);
        var svc = new MobChangeTargetService();

        Assert.False(svc.CanChangeTarget(mob, newTarget));
        Assert.False(svc.TrySetTarget(mob, newTarget));
        Assert.Equal(99, mob.TargetId); // unchanged
    }

    [Fact]
    public void Berserk_WithChangeTargetMelee_Allows()
    {
        var mob = MakeMob(mode: MobMode.ChangeTargetMelee, state: MobSkillState.Berserk, currentTargetId: 99);
        var newTarget = MakePc(id: 7);
        var svc = new MobChangeTargetService();

        Assert.True(svc.CanChangeTarget(mob, newTarget));
        Assert.True(svc.TrySetTarget(mob, newTarget));
        Assert.Equal(7, mob.TargetId);
    }

    [Fact]
    public void Rush_RequiresChangeTargetChaseBit()
    {
        // Chasing without the bit — refuse.
        var noBit = MakeMob(mode: 0, state: MobSkillState.Rush, currentTargetId: 99);
        // Same mob but with the bit — allow.
        var withBit = MakeMob(mode: MobMode.ChangeTargetChase, state: MobSkillState.Rush, currentTargetId: 99);
        var newTarget = MakePc(id: 7);
        var svc = new MobChangeTargetService();

        Assert.False(svc.CanChangeTarget(noBit, newTarget));
        Assert.True(svc.CanChangeTarget(withBit, newTarget));
    }

    [Theory]
    [InlineData(MobSkillState.Idle)]
    [InlineData(MobSkillState.Walk)]
    [InlineData(MobSkillState.Follow)]
    [InlineData(MobSkillState.Angry)]
    [InlineData(MobSkillState.Loot)]
    public void PassiveStates_AlwaysAllow(MobSkillState state)
    {
        var mob = MakeMob(mode: 0, state: state, currentTargetId: 99);
        var newTarget = MakePc(id: 7);
        var svc = new MobChangeTargetService();
        Assert.True(svc.CanChangeTarget(mob, newTarget));
    }

    [Fact]
    public void DeadState_Refuses()
    {
        var mob = MakeMob(mode: 0, state: MobSkillState.Dead, currentTargetId: 99);
        var newTarget = MakePc(id: 7);
        Assert.False(new MobChangeTargetService().CanChangeTarget(mob, newTarget));
    }

    [Fact]
    public void TrySetTarget_NoCurrentTarget_AlwaysAccepts()
    {
        // First acquisition skips the gate — rAthena mob.cpp:1296
        // only checks mob_can_changetarget when md->target_id is set.
        var mob = MakeMob(mode: 0, state: MobSkillState.Berserk, currentTargetId: 0);
        var newTarget = MakePc(id: 7);
        var svc = new MobChangeTargetService();

        Assert.True(svc.TrySetTarget(mob, newTarget));
        Assert.Equal(7, mob.TargetId);
    }

    // --- helpers ---

    private static MobEntity MakeMob(MobMode mode, MobSkillState state, int currentTargetId)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 100 };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(1), db, origin, mapId: 1, x: 0, y: 0);
        mob.MaxHp = 100;
        mob.Hp = 100;
        mob.Stats.Mode = mode;
        mob.SkillState = state;
        mob.TargetId = currentTargetId;
        return mob;
    }

    private static PlayerEntity MakePc(int id)
        => new(characterId: id, accountId: 1, name: "PC", sessionId: Guid.NewGuid(),
            mapId: 1, x: 0, y: 0);
}
