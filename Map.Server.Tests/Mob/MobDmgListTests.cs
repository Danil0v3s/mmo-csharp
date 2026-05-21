using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.7 — unit tests for <see cref="MobDmgList"/> ring buffer.
/// Mirrors rAthena <c>md-&gt;dmglog</c> semantics from
/// <c>battle.cpp:battle_damage</c> and <c>mob.cpp:mob_damage</c>.
/// </summary>
public class MobDmgListTests
{
    [Fact]
    public void Record_NewAttacker_AddsSlot()
    {
        var log = new MobDmgList();
        log.Record(new EntityId(42), 100);
        Assert.Equal(1, log.DistinctAttackerCount);
        Assert.Equal(100, log.DamageFrom(new EntityId(42)));
    }

    [Fact]
    public void Record_SameAttackerTwice_Accumulates()
    {
        var log = new MobDmgList();
        log.Record(new EntityId(42), 100);
        log.Record(new EntityId(42), 50);
        Assert.Equal(1, log.DistinctAttackerCount);
        Assert.Equal(150, log.DamageFrom(new EntityId(42)));
    }

    [Fact]
    public void Record_ZeroOrNegativeDamage_Ignored()
    {
        var log = new MobDmgList();
        log.Record(new EntityId(1), 0);
        log.Record(new EntityId(1), -5);
        Assert.Equal(0, log.DistinctAttackerCount);
    }

    [Fact]
    public void Record_OverCapacity_EvictsOldest()
    {
        var log = new MobDmgList();
        for (int i = 1; i <= MobDmgList.Capacity; i++)
            log.Record(new EntityId(i), 10);
        Assert.Equal(MobDmgList.Capacity, log.DistinctAttackerCount);

        // 31st attacker evicts attacker 1.
        log.Record(new EntityId(99), 5);
        Assert.Equal(MobDmgList.Capacity, log.DistinctAttackerCount);
        Assert.Equal(0, log.DamageFrom(new EntityId(1)));
        Assert.Equal(5, log.DamageFrom(new EntityId(99)));
    }

    [Fact]
    public void DamageFrom_UnknownAttacker_Zero()
    {
        var log = new MobDmgList();
        log.Record(new EntityId(1), 100);
        Assert.Equal(0, log.DamageFrom(new EntityId(999)));
    }

    [Fact]
    public void Clear_DropsAllEntries()
    {
        var log = new MobDmgList();
        log.Record(new EntityId(1), 100);
        log.Record(new EntityId(2), 50);
        log.Clear();
        Assert.Equal(0, log.DistinctAttackerCount);
    }
}
