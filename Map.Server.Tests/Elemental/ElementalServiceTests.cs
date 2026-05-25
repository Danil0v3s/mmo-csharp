using Map.Server.Elemental;
using Map.Server.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Elemental;

/// <summary>
/// Wave 89 — coverage for the per-master ElementalEntity store +
/// lifecycle hooks (data_received / save / change_mode /
/// clean_effect / set_target / unlock_target / heal / skillnotok /
/// summon_init / SerializeSnapshot).
/// </summary>
public class ElementalServiceTests
{
    [Fact]
    public void Create_SetsBindingAndLifetime()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        var classId = (int)ElementalClass.AgniS;

        var rc = svc.Create(pc, classId, lifetimeMs: 60_000);

        Assert.Equal(classId, rc);
        Assert.Equal(classId, pc.ActiveElementalClassId);
        Assert.True(pc.ActiveElementalExpiresAt > Environment.TickCount64);
    }

    [Fact]
    public void Create_ReplacesExisting_DropsLiveEntity()
    {
        var (svc, pc, registry, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        Assert.Single(registry.AllElementalEntities());

        svc.Create(pc, 2117 /* AQUA_S */, 30_000);

        Assert.Equal(2117, pc.ActiveElementalClassId);
        // delete-before-create dropped the prior live entity
        Assert.Empty(registry.AllElementalEntities());
    }

    [Fact]
    public void DataReceived_WithRegistry_RegistersLiveEntity()
    {
        var (svc, pc, registry, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);

        var rc = svc.DataReceived(pc);

        Assert.Equal(1, rc);
        Assert.Single(registry.AllElementalEntities());
        var ele = Assert.IsType<ElementalEntity>(registry.AllElementalEntities().First());
        Assert.Equal(2114, ele.ClassId);
        Assert.Equal(pc.Id, ele.MasterId);
        Assert.Equal(ElementalService.ModePassive, ele.Mode);
    }

    [Fact]
    public void DataReceived_NoBinding_Returns0()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        Assert.Equal(0, svc.DataReceived(pc));
    }

    [Fact]
    public void DataReceived_HeadlessNoRegistry_Returns0()
    {
        var pc = NewPc();
        var svc = new ElementalService(NullLogger<ElementalService>.Instance);
        svc.Create(pc, 2114, 60_000);

        Assert.Equal(0, svc.DataReceived(pc));
    }

    [Fact]
    public void Save_WithLiveEntity_Returns1()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);

        Assert.Equal(1, svc.Save(pc));
    }

    [Fact]
    public void Save_NoLiveEntity_Returns0()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        Assert.Equal(0, svc.Save(pc));
    }

    [Fact]
    public void ChangeMode_PassiveToAggressive_UpdatesEntityMode()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);

        var rc = svc.ChangeMode(pc, ElementalService.ModeAggressive);

        Assert.Equal(1, rc);
        Assert.NotNull(svc.GetForMaster(pc));
        Assert.Equal(ElementalService.ModeAggressive, svc.GetForMaster(pc)!.Mode);
    }

    [Fact]
    public void ChangeMode_NoLiveEntity_Returns0()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        Assert.Equal(0, svc.ChangeMode(pc, ElementalService.ModeAggressive));
    }

    [Fact]
    public void SetTarget_LatchesWhenUnset_Returns1()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        var ele = svc.GetForMaster(pc)!;
        var target = new EntityId(42);

        var rc = svc.SetTarget(pc, target);

        Assert.Equal(1, rc);
        Assert.Equal(target.Value, ele.TargetId);
    }

    [Fact]
    public void SetTarget_DoesNotClobberExistingLock()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        var ele = svc.GetForMaster(pc)!;
        ele.TargetId = 99;

        svc.SetTarget(pc, new EntityId(42));

        Assert.Equal(99, ele.TargetId); // unchanged — rAthena guards on ==0
    }

    [Fact]
    public void UnlockTarget_ClearsLock()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        var ele = svc.GetForMaster(pc)!;
        ele.TargetId = 77;

        svc.UnlockTarget(pc);

        Assert.Equal(0, ele.TargetId);
    }

    [Fact]
    public void Action_DropsPriorTarget_ThenLatchesNewOne()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        var ele = svc.GetForMaster(pc)!;
        ele.TargetId = 5; // prior

        var rc = svc.Action(pc, new EntityId(42), tick: 1234);

        Assert.Equal(1, rc);
        Assert.Equal(42, ele.TargetId);
        Assert.Equal(1234, ele.LastThinkTick);
    }

    [Fact]
    public void Heal_ClampsToMax()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        var ele = svc.GetForMaster(pc)!;
        ele.MaxHp = 100;
        ele.Hp = 80;
        ele.MaxSp = 50;
        ele.Sp = 40;

        svc.Heal(pc, hp: 999, sp: 999);

        Assert.Equal(100, ele.Hp);
        Assert.Equal(50, ele.Sp);
    }

    [Fact]
    public void Heal_NoLiveEntity_NoThrow()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        // No exception, no state change.
        svc.Heal(pc, 10, 10);
    }

    [Fact]
    public void SkillNotOk_WithLiveEntity_ReturnsFalse()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        Assert.False(svc.SkillNotOk(pc, skillId: 100));
    }

    [Fact]
    public void SkillNotOk_NoLiveEntity_ReturnsTrue()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        Assert.True(svc.SkillNotOk(pc, skillId: 100));
    }

    [Fact]
    public void SummonInit_StampsTicks_OnLiveEntity()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        var ele = svc.GetForMaster(pc)!;
        ele.SummonExpiresAtTick = 0;
        ele.LastThinkTick = 0;

        svc.SummonInit(pc);

        Assert.Equal(pc.ActiveElementalExpiresAt, ele.SummonExpiresAtTick);
        Assert.True(ele.LastThinkTick > 0);
    }

    [Fact]
    public void Delete_DropsLiveEntity_FromRegistry()
    {
        var (svc, pc, registry, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        Assert.Single(registry.AllElementalEntities());

        var rc = svc.Delete(pc);

        Assert.Equal(1, rc);
        Assert.Empty(registry.AllElementalEntities());
        Assert.Equal(0, pc.ActiveElementalClassId);
    }

    [Fact]
    public void SerializeSnapshot_FindsLiveById()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        var ele = svc.GetForMaster(pc)!;

        // EleId=0 unsaved (round-trips from char server). Test the
        // not-found and the matching path against the assigned id.
        Assert.Null(svc.SerializeSnapshot(elementalId: 9999));
        Assert.NotNull(svc.SerializeSnapshot(elementalId: ele.ElementalId));
    }

    [Fact]
    public void CleanEffect_NoLiveEntity_Returns0()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        Assert.Equal(0, svc.CleanEffect(pc));
    }

    [Fact]
    public void CleanEffect_WithLiveEntity_Returns1()
    {
        var (svc, pc, _, _) = NewServiceWithRegistry();
        svc.Create(pc, 2114, 60_000);
        svc.DataReceived(pc);
        Assert.Equal(1, svc.CleanEffect(pc));
    }

    // ---- helpers ----

    private static PlayerEntity NewPc()
        => new(characterId: 1234, accountId: 1, name: "Sorcerer", sessionId: Guid.NewGuid(),
            mapId: 0, x: 100, y: 100);

    private static (ElementalService Svc, PlayerEntity Pc, FakeRegistry Registry, EntityIdAllocator Ids)
        NewServiceWithRegistry()
    {
        var registry = new FakeRegistry();
        var ids = new EntityIdAllocator();
        var pc = NewPc();
        registry.Add(pc);
        var svc = new ElementalService(NullLogger<ElementalService>.Instance, registry, ids);
        return (svc, pc, registry, ids);
    }

    private sealed class FakeRegistry : IEntityRegistry
    {
        private readonly Dictionary<EntityId, Entity> _e = new();
        public int Count => _e.Count;
        public void Add(Entity entity) => _e[entity.Id] = entity;
        public Entity? Remove(EntityId id) => _e.Remove(id, out var e) ? e : null;
        public Entity? Get(EntityId id) => _e.TryGetValue(id, out var e) ? e : null;
        public bool Contains(EntityId id) => _e.ContainsKey(id);
        public void Move(EntityId id, short newX, short newY) { }
        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask)
            => _e.Values.ToArray();
        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask)
            => _e.Values.ToArray();
        public IEnumerable<Entity> All() => _e.Values;

        // Test-only — filter to ElementalEntity instances.
        public IReadOnlyList<Entity> AllElementalEntities()
            => _e.Values.Where(e => e is ElementalEntity).ToArray();
    }
}

/// <summary>
/// Sorcerer / EM elemental class ids (subset of rAthena
/// <c>elemental_elementalid</c>). Only the canonical Agni / Aqua
/// entry points are referenced from tests; the catalog row is the
/// source of truth elsewhere.
/// </summary>
internal enum ElementalClass
{
    AgniS = 2114,
    AquaS = 2117,
}
