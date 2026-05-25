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
/// Wave 71 / Track D — Targeters counter mirrors rAthena
/// <c>unit_counttargeted</c>. Verifies that
/// <see cref="IAttackService.StartAttack"/> increments the target's
/// <see cref="Entity.Targeters"/>, <see cref="IAttackService.StopAttack"/>
/// decrements it, and re-latching the same target is a no-op.
/// </summary>
public class Wave71TargetersTests
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
    public void StartAttack_IncrementsTargetersOnce()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t);
        Assert.Equal(0, t.Targeters);
        ctx.Attack.StartAttack(a, t.Id, continuous: true);
        Assert.Equal(1, t.Targeters);
    }

    [Fact]
    public void StartAttack_SameTargetReLatch_DoesNotDoubleCount()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t);
        ctx.Attack.StartAttack(a, t.Id, continuous: true);
        ctx.Attack.StartAttack(a, t.Id, continuous: true); // re-latch same target
        Assert.Equal(1, t.Targeters);
    }

    [Fact]
    public void StartAttack_NewTarget_TransfersTargeters()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t1 = NewMob(2, 6, 5);
        var t2 = NewMob(3, 7, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t1);
        ctx.Entities.Add(t2);
        ctx.Attack.StartAttack(a, t1.Id, continuous: true);
        Assert.Equal(1, t1.Targeters);
        ctx.Attack.StartAttack(a, t2.Id, continuous: true);
        Assert.Equal(0, t1.Targeters);
        Assert.Equal(1, t2.Targeters);
    }

    [Fact]
    public void StopAttack_DecrementsTargeters()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        var t = NewMob(2, 6, 5);
        ctx.Entities.Add(a);
        ctx.Entities.Add(t);
        ctx.Attack.StartAttack(a, t.Id, continuous: true);
        ctx.Attack.StopAttack(a);
        Assert.Equal(0, t.Targeters);
    }

    [Fact]
    public void StopAttack_NoLatchedTarget_IsNoOp()
    {
        var ctx = Build();
        var a = NewMob(1, 5, 5);
        ctx.Entities.Add(a);
        ctx.Attack.StopAttack(a);
        // No assert needed — call must not throw.
    }

    [Fact]
    public void MultipleAttackers_AccumulateTargeters()
    {
        var ctx = Build();
        var a1 = NewMob(1, 5, 5);
        var a2 = NewMob(2, 5, 6);
        var a3 = NewMob(3, 5, 7);
        var t = NewMob(4, 6, 5);
        ctx.Entities.Add(a1);
        ctx.Entities.Add(a2);
        ctx.Entities.Add(a3);
        ctx.Entities.Add(t);
        ctx.Attack.StartAttack(a1, t.Id, continuous: true);
        ctx.Attack.StartAttack(a2, t.Id, continuous: true);
        ctx.Attack.StartAttack(a3, t.Id, continuous: true);
        Assert.Equal(3, t.Targeters);
    }

    // -- harness --

    private sealed record TestContext(
        IAttackService Attack,
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
        return new TestContext(attack, entities);
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
