using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.9f — tests for the mob_chat_db lookup helper and the
/// mob_randomwalk wander roll. Chat broadcast pipe is exercised
/// via <see cref="IClifWireService.MobChat"/> at the seam where the
/// mob_skill picker fires; the YAML loader for the db itself
/// remains a separate wave.
/// </summary>
public class MobChatRandomWalkTests
{
    // ---- mob_chat_db ----

    [Fact]
    public void MobChatDb_AddAndFind_RoundTrips()
    {
        var db = new MobChatDb();
        Assert.Equal(0, db.Count);
        Assert.Null(db.Find(42));

        var row = new MobChatRow(42, 0xFF0000, "RAH!");
        db.Add(row);
        Assert.Equal(1, db.Count);

        var found = db.Find(42);
        Assert.NotNull(found);
        Assert.Equal("RAH!", found!.Message);
        Assert.Equal(0xFF0000u, found.ColorRgb);
    }

    [Fact]
    public void MobChatDb_AddOverwrites()
    {
        var db = new MobChatDb();
        db.Add(new MobChatRow(7, 0x00FF00, "first"));
        db.Add(new MobChatRow(7, 0x0000FF, "second"));
        Assert.Equal(1, db.Count);
        Assert.Equal("second", db.Find(7)!.Message);
    }

    // ---- mob_randomwalk ----

    [Fact]
    public void Wander_FirstCall_InitsNextWanderTickAndReturnsFalse()
    {
        // rAthena mob.cpp:1681 — first invocation populates
        // next_walktime and returns 1 WITHOUT walking. Our port
        // returns false to match the "no walk happened" semantics.
        var mob = MakeMob();
        Assert.Equal(0, mob.NextWanderTick);

        var svc = new MobRandomWalkService(
            NullLogger<MobRandomWalkService>.Instance, movement: null, rng: new Random(1));
        var fired = svc.TryWander(mob, nowTick: 1000);

        Assert.False(fired);
        Assert.True(mob.NextWanderTick > 1000); // initialised
    }

    [Fact]
    public void Wander_TooSoon_DoesNothing()
    {
        var mob = MakeMob();
        mob.NextWanderTick = 999_999; // far in the future

        var svc = new MobRandomWalkService(
            NullLogger<MobRandomWalkService>.Instance, movement: null, rng: new Random(2));
        Assert.False(svc.TryWander(mob, nowTick: 1000));
    }

    [Fact]
    public void Wander_NoRandomWalkMode_DoesNothing()
    {
        var mob = MakeMob();
        mob.Stats.Mode |= MobMode.NoRandomWalk;
        mob.NextWanderTick = 100; // due

        var svc = new MobRandomWalkService(
            NullLogger<MobRandomWalkService>.Instance, movement: null, rng: new Random(3));
        Assert.False(svc.TryWander(mob, nowTick: 1000));
    }

    [Fact]
    public void Wander_CannotMove_DoesNothing()
    {
        var mob = MakeMob();
        // Drop the CanMove bit.
        mob.Stats.Mode = MobMode.Aggressive; // no CanMove
        mob.NextWanderTick = 100; // due

        var svc = new MobRandomWalkService(
            NullLogger<MobRandomWalkService>.Instance, movement: null, rng: new Random(4));
        Assert.False(svc.TryWander(mob, nowTick: 1000));
    }

    [Fact]
    public void Wander_Eligible_QueuesNextWanderTick()
    {
        // Movement service is null, so TryStartWalk can't actually
        // run. We assert that NextWanderTick advances regardless —
        // matching rAthena mob.cpp:1696-1706 where next_walktime is
        // pushed forward before the walk attempt.
        var mob = MakeMob();
        mob.Stats.Mode = MobMode.CanMove; // eligible
        mob.NextWanderTick = 100;

        var svc = new MobRandomWalkService(
            NullLogger<MobRandomWalkService>.Instance, movement: null, rng: new Random(5));
        svc.TryWander(mob, nowTick: 1000);

        Assert.True(mob.NextWanderTick > 1000);
    }

    // ---- helpers ----

    private static MobEntity MakeMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 100 };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(1), db, origin, mapId: 1, x: 10, y: 10);
        mob.MaxHp = 100;
        mob.Hp = 100;
        mob.Stats.Mode = MobMode.CanMove;
        return mob;
    }
}
