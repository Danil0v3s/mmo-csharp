using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// Wave 69 / Track B — end-to-end timer-advancement tests covering the
/// canonical entry points
/// <see cref="IAttackService.SetAttackDelay"/> and
/// <see cref="IMovementService.SetWalkDelay"/>.
///
/// Scenarios:
/// * SetAttackDelay pushes <see cref="AttackState.AttackableTick"/>
///   forward (mirrors rAthena's post-swing attacktimer slip).
/// * SetWalkDelay stamps <see cref="Entity.WalkableAfterTick"/> and
///   cancels any active walk (rAthena unit_set_walkdelay).
/// * TryStartWalk respects the WalkableAfterTick freeze.
/// * Longest-delay-wins: a later smaller delay never shortens an
///   existing freeze.
/// </summary>
public class AttackTimerTests
{
    private static PlayerEntity NewPc(int charId = 1) =>
        new(characterId: charId, accountId: charId, name: $"PC{charId}",
            sessionId: Guid.NewGuid(), mapId: 0, x: 5, y: 5);

    private static MobEntity NewMob(int id = 2)
    {
        var m = new MobEntity(new EntityId(id), classId: 1001, name: "test_mob",
            mapId: 0, x: 5, y: 5);
        m.Hp = m.MaxHp = 1000;
        return m;
    }

    // ---- SetAttackDelay on AttackState ----

    [Fact]
    public void SetAttackDelay_PushesAttackableTickForward()
    {
        var ctx = BuildContext();
        var mob = NewMob();
        ctx.Entities.Add(mob);
        mob.Attack = new AttackState { TargetId = new EntityId(1), Continuous = true };
        // AttackableTick starts at 0 (no swing yet). After SetAttackDelay,
        // it should be ~now+delay.
        var now = Environment.TickCount64;

        ctx.Attack.SetAttackDelay(mob, dmotionMs: 500);

        var diff = mob.Attack.AttackableTick - now;
        Assert.True(diff >= 499 && diff <= 700,
            $"Expected ~500ms advance from now; got {diff}ms");
    }

    [Fact]
    public void SetAttackDelay_NoOpWhenNoAttackState()
    {
        var ctx = BuildContext();
        var mob = NewMob();
        // mob.Attack is null — should not throw.
        ctx.Attack.SetAttackDelay(mob, 500);
        Assert.Null(mob.Attack);
    }

    [Fact]
    public void SetAttackDelay_RejectsSmallerThanCurrentRemaining()
    {
        // Longest delay wins (rAthena: a fresh hit doesn't shorten an
        // existing hit-stun window).
        var ctx = BuildContext();
        var mob = NewMob();
        var farFuture = Environment.TickCount64 + 10_000;
        mob.Attack = new AttackState
        {
            TargetId = new EntityId(1),
            Continuous = true,
            AttackableTick = farFuture,
        };
        ctx.Attack.SetAttackDelay(mob, 100); // tiny delay
        Assert.Equal(farFuture, mob.Attack.AttackableTick);
    }

    [Fact]
    public void SetAttackDelay_ZeroOrNegativeIsNoOp()
    {
        var ctx = BuildContext();
        var mob = NewMob();
        mob.Attack = new AttackState { TargetId = new EntityId(1) };
        var before = mob.Attack.AttackableTick;
        ctx.Attack.SetAttackDelay(mob, 0);
        ctx.Attack.SetAttackDelay(mob, -500);
        Assert.Equal(before, mob.Attack.AttackableTick);
    }

    // ---- SetWalkDelay on Entity.WalkableAfterTick ----

    [Fact]
    public void SetWalkDelay_StampsWalkableAfterTick()
    {
        var ctx = BuildContext();
        var mob = NewMob();
        Assert.Equal(0, mob.WalkableAfterTick);
        ctx.Movement.SetWalkDelay(mob, 250);
        var diff = mob.WalkableAfterTick - Environment.TickCount64;
        Assert.True(diff >= 200 && diff <= 500,
            $"Expected ~250ms walkdelay; got {diff}ms");
    }

    [Fact]
    public void SetWalkDelay_LongestDelayWins()
    {
        var ctx = BuildContext();
        var mob = NewMob();
        ctx.Movement.SetWalkDelay(mob, 1000);
        var afterFirst = mob.WalkableAfterTick;
        // Try a shorter delay; should be ignored.
        ctx.Movement.SetWalkDelay(mob, 100);
        Assert.Equal(afterFirst, mob.WalkableAfterTick);
    }

    [Fact]
    public void SetWalkDelay_ZeroOrNegativeIsNoOp()
    {
        var ctx = BuildContext();
        var mob = NewMob();
        ctx.Movement.SetWalkDelay(mob, 0);
        ctx.Movement.SetWalkDelay(mob, -100);
        Assert.Equal(0, mob.WalkableAfterTick);
    }

    [Fact]
    public void TryStartWalk_BlockedDuringWalkdelayWindow()
    {
        var ctx = BuildContext();
        var mob = MobOnContextMap(ctx);
        ctx.Entities.Add(mob);
        ctx.Movement.SetWalkDelay(mob, 5_000);
        // Try to walk one cell over — should be refused while the
        // freeze is active.
        var started = ctx.Movement.TryStartWalk(mob, (short)(mob.X + 1), mob.Y);
        Assert.False(started);
    }

    [Fact]
    public void TryStartWalk_AllowedAfterWalkdelayElapses()
    {
        var ctx = BuildContext();
        var mob = MobOnContextMap(ctx);
        ctx.Entities.Add(mob);
        // Stamp a delay already in the past.
        mob.WalkableAfterTick = Environment.TickCount64 - 1000;
        var started = ctx.Movement.TryStartWalk(mob, (short)(mob.X + 1), mob.Y);
        Assert.True(started);
    }

    /// <summary>Build a mob whose MapId matches the test-context map hash.</summary>
    private static MobEntity MobOnContextMap(TestContext ctx)
    {
        var mapId = (uint)"stub".GetHashCode();
        var m = new MobEntity(new EntityId(3), classId: 1001, name: "test_mob",
            mapId: mapId, x: 5, y: 5);
        m.Hp = m.MaxHp = 1000;
        return m;
    }

    // ---- end-to-end approximation: DMotion → SetAttackDelay path ----

    [Fact]
    public void DMotion_PostHit_AdvancesTargetSwingTick()
    {
        // Simulate what DamageService.PerformMeleeAttack does after
        // CalcWeaponAttack: it pushes target.Attack.AttackableTick by
        // the BattleDamage.DMotion. With target.Amotion = 500, dmotion
        // = clamp(500-50, 0, 2000) = 450.
        var ctx = BuildContext();
        var mob = NewMob();
        ctx.Entities.Add(mob);
        var pc = NewPc();
        mob.Attack = new AttackState { TargetId = pc.Id, Continuous = true };
        const int dmotion = 450;
        ctx.Attack.SetAttackDelay(mob, dmotion);
        var diff = mob.Attack.AttackableTick - Environment.TickCount64;
        Assert.True(diff >= dmotion - 50 && diff <= dmotion + 200,
            $"Expected ~{dmotion}ms advance; got {diff}ms");
    }

    // ---- harness ----

    private sealed record TestContext(
        IAttackService Attack,
        IMovementService Movement,
        EntityRegistry Entities);

    private static TestContext BuildContext()
    {
        const string mapName = "stub";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(
            entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(),
            NullLogger<MovementService>.Instance);
        var damage = new NullDamage();
        var attack = new AttackService(
            entities, damage, movement, NullLogger<AttackService>.Instance);
        return new TestContext(attack, movement, entities);
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }

    private sealed class NullDamage : IDamageService
    {
        public int ApplyDamage(Entity target, int damage, Entity? source = null) => damage;
        public BattleDamage PerformMeleeAttack(Entity source, Entity target) => new();
    }
}
