using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Movement.UnitOps;
using Map.Server.Pathing;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Movement;

/// <summary>
/// Wave 72 — UnitOpsService façade delegation. Verifies each rAthena
/// <c>unit_*</c> entry point on <see cref="IUnitOpsService"/> hits the
/// underlying canonical owner (MovementService, AttackService,
/// IPathService, etc.) instead of returning the previous stub default.
/// </summary>
public class UnitOpsServiceTests
{
    private static MobEntity NewMob(int id, short x = 5, short y = 5)
    {
        var mapId = (uint)"stub".GetHashCode();
        var m = new MobEntity(new EntityId(id), classId: 1001,
            name: $"mob{id}", mapId: mapId, x: x, y: y);
        m.Hp = m.MaxHp = 1000;
        return m;
    }

    [Fact]
    public void WalkToXy_DelegatesToMovementTryStartWalk()
    {
        var ctx = Build();
        var m = NewMob(1);
        ctx.Entities.Add(m);
        var ok = ctx.UnitOps.WalkToXy(m, (short)(m.X + 1), m.Y, flag: 0);
        Assert.True(ok);
        Assert.NotNull(m.Walk); // MovementService set up a walk state.
    }

    [Fact]
    public void StopWalking_CancelsActiveWalk()
    {
        var ctx = Build();
        var m = NewMob(1);
        ctx.Entities.Add(m);
        ctx.Movement.TryStartWalk(m, (short)(m.X + 1), m.Y);
        Assert.NotNull(m.Walk);
        var wasWalking = ctx.UnitOps.StopWalking(m, type: 0);
        Assert.True(wasWalking);
        Assert.Null(m.Walk);
    }

    [Fact]
    public void StopWalking_NotWalking_ReturnsFalse()
    {
        var ctx = Build();
        var m = NewMob(1);
        ctx.Entities.Add(m);
        var wasWalking = ctx.UnitOps.StopWalking(m, type: 0);
        Assert.False(wasWalking);
    }

    [Fact]
    public void Attack_DelegatesToAttackStartAttack()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t);
        var ok = ctx.UnitOps.Attack(a, t.Id, continuous: 1);
        Assert.True(ok);
        Assert.NotNull(a.Attack);
        Assert.Equal(t.Id.Value, a.Attack!.TargetId.Value);
        // Wave 71 — Targeters counter on the target.
        Assert.Equal(1, t.Targeters);
    }

    [Fact]
    public void StopAttack_DropsLatchAndDecrementsTargeters()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t);
        ctx.UnitOps.Attack(a, t.Id, continuous: 1);
        Assert.Equal(1, t.Targeters);

        var wasAttacking = ctx.UnitOps.StopAttack(a);
        Assert.True(wasAttacking);
        Assert.Null(a.Attack);
        Assert.Equal(0, t.Targeters);
    }

    [Fact]
    public void CanMove_TrueWhenIdle_FalseDuringWalkdelay()
    {
        var ctx = Build();
        var m = NewMob(1);
        ctx.Entities.Add(m);
        Assert.True(ctx.UnitOps.CanMove(m));
        // Hit-stun freeze.
        ctx.Movement.SetWalkDelay(m, 5000);
        Assert.False(ctx.UnitOps.CanMove(m));
    }

    [Fact]
    public void SetDir_WritesEntityDir_AndIsIdempotent()
    {
        var ctx = Build();
        var m = NewMob(1);
        Assert.Equal(0, m.Dir);
        var ok = ctx.UnitOps.SetDir(m, 4);
        Assert.True(ok);
        Assert.Equal(4, m.Dir);
        // Same direction set → still true, no change.
        Assert.True(ctx.UnitOps.SetDir(m, 4));
        Assert.Equal(4, m.Dir);
        // Out-of-range rejected.
        Assert.False(ctx.UnitOps.SetDir(m, 9));
    }

    [Fact]
    public void GetDir_ReadsEntityDir()
    {
        var ctx = Build();
        var m = NewMob(1);
        m.Dir = 7;
        Assert.Equal(7, ctx.UnitOps.GetDir(m));
    }

    [Fact]
    public void RemoveMap_DropsFromRegistry_AndReturnsOne()
    {
        var ctx = Build();
        var m = NewMob(1);
        ctx.Entities.Add(m);
        Assert.True(ctx.Entities.Contains(m.Id));
        var n = ctx.UnitOps.RemoveMap(m, clearType: 0);
        Assert.Equal(1, n);
        Assert.False(ctx.Entities.Contains(m.Id));
    }

    [Fact]
    public void RemoveMap_AlreadyGone_ReturnsZero()
    {
        var ctx = Build();
        var m = NewMob(1);
        // Not added to registry.
        Assert.Equal(0, ctx.UnitOps.RemoveMap(m, clearType: 0));
    }

    [Fact]
    public void Free_DropsWalkAndAttackState()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t);
        ctx.Movement.TryStartWalk(a, (short)(a.X + 1), a.Y);
        ctx.UnitOps.Attack(a, t.Id, continuous: 1);
        Assert.NotNull(a.Walk);
        Assert.NotNull(a.Attack);

        ctx.UnitOps.Free(a, clearType: 0);
        Assert.Null(a.Walk);
        Assert.Null(a.Attack);
    }

    [Fact]
    public void CanReachPos_OnWalkableCell_ReturnsTrue()
    {
        var ctx = Build();
        var m = NewMob(1);
        ctx.Entities.Add(m);
        Assert.True(ctx.UnitOps.CanReachPos(m, (short)(m.X + 5), m.Y, easy: 1));
    }

    [Fact]
    public void CanReachBl_ChebyshevWithinRange_ShortCircuits()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t);
        // 1 cell Chebyshev distance with range 2 → trivially reachable.
        Assert.True(ctx.UnitOps.CanReachBl(a, t, range: 2));
    }

    // ---- Wave 73 — small canonical helpers ----

    [Fact]
    public void IsWalking_TrueWhileWalkActive()
    {
        var ctx = Build();
        var m = NewMob(1);
        ctx.Entities.Add(m);
        Assert.False(ctx.UnitOps.IsWalking(m));
        ctx.Movement.TryStartWalk(m, (short)(m.X + 1), m.Y);
        Assert.True(ctx.UnitOps.IsWalking(m));
        ctx.Movement.CancelWalk(m);
        Assert.False(ctx.UnitOps.IsWalking(m));
    }

    [Fact]
    public void CanAttack_FalseForCrossMap_OrSelf_OrDead()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a); ctx.Entities.Add(t);
        Assert.True(ctx.UnitOps.CanAttack(a, t));
        // Self
        Assert.False(ctx.UnitOps.CanAttack(a, a));
        // Dead target
        t.Hp = 0;
        Assert.False(ctx.UnitOps.CanAttack(a, t));
    }

    [Fact]
    public void SetTarget_NoPriorLatch_LatchesAndIncrementsTargeters()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a); ctx.Entities.Add(t);
        Assert.True(ctx.UnitOps.SetTarget(a, t.Id));
        Assert.Equal(1, t.Targeters);
    }

    [Fact]
    public void SetTarget_SameTarget_NoOp()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a); ctx.Entities.Add(t);
        ctx.UnitOps.SetTarget(a, t.Id);
        Assert.Equal(1, t.Targeters);
        Assert.True(ctx.UnitOps.SetTarget(a, t.Id));
        Assert.Equal(1, t.Targeters); // unchanged
    }

    [Fact]
    public void ChangeTarget_Transfers_TargetersCount()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t1 = NewMob(2, 6, 5);
        var t2 = NewMob(3, 7, 5);
        ctx.Entities.Add(a); ctx.Entities.Add(t1); ctx.Entities.Add(t2);
        ctx.UnitOps.SetTarget(a, t1.Id);
        Assert.Equal(1, t1.Targeters);
        ctx.UnitOps.ChangeTarget(a, t2.Id);
        Assert.Equal(0, t1.Targeters);
        Assert.Equal(1, t2.Targeters);
    }

    [Fact]
    public void Unattackable_DropsAttackLock()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a); ctx.Entities.Add(t);
        ctx.UnitOps.Attack(a, t.Id, continuous: 1);
        Assert.NotNull(a.Attack);
        ctx.UnitOps.Unattackable(a);
        Assert.Null(a.Attack);
        Assert.Equal(0, t.Targeters);
    }

    [Fact]
    public void Refresh_OnNonRegisteredEntity_IsNoOp()
    {
        var ctx = Build();
        var m = NewMob(1);
        // Not added to registry — must not throw.
        ctx.UnitOps.Refresh(m);
    }

    // -- harness --

    private sealed record TestContext(
        IUnitOpsService UnitOps,
        IAttackService Attack,
        IMovementService Movement,
        EntityRegistry Entities);

    private static TestContext Build()
    {
        var map = new MapData("stub", 200, 200, new byte[200 * 200]);
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
        var path = new PathService(NullLogger<PathService>.Instance, world);
        var unitOps = new UnitOpsService(
            path, entities, visibility, movement, attack,
            NullLogger<UnitOpsService>.Instance, world);
        return new TestContext(unitOps, attack, movement, entities);
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
