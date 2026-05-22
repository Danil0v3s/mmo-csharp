using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Mob.Conditions;
using Map.Server.Spawn;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.9e — unit tests for the slave-protect + alchemist-summon
/// evaluators. rAthena <c>mob.cpp:4373-4376</c>.
/// </summary>
public class MobSlaveConditionsTests
{
    // ---- MSC_MASTERATTACKED ----

    [Fact]
    public void MasterAttacked_NoMasterId_False()
    {
        var slave = MakeMob(id: 1);
        Assert.Null(slave.MasterId);

        var ev = new MasterAttackedCondition();
        Assert.False(ev.IsMet(slave, MakeEntry(MobSkillCondition.MasterAttacked),
            new MobConditionContext { Entities = new FakeRegistry() }));
    }

    [Fact]
    public void MasterAttacked_MasterMissingFromRegistry_False()
    {
        var slave = MakeMob(id: 1);
        slave.MasterId = new EntityId(999); // points at nothing
        var registry = new FakeRegistry();
        registry.Add(slave);

        var ev = new MasterAttackedCondition();
        Assert.False(ev.IsMet(slave, MakeEntry(MobSkillCondition.MasterAttacked),
            new MobConditionContext { Entities = registry }));
    }

    [Fact]
    public void MasterAttacked_MasterMob_WithAttackers_True()
    {
        // Master mob with one distinct attacker in its DmgList → fire.
        var master = MakeMob(id: 5);
        master.DmgList.Record(attackerId: new EntityId(100), damage: 50);

        var slave = MakeMob(id: 6);
        slave.MasterId = master.Id;

        var registry = new FakeRegistry();
        registry.Add(master);
        registry.Add(slave);

        var ev = new MasterAttackedCondition();
        Assert.True(ev.IsMet(slave, MakeEntry(MobSkillCondition.MasterAttacked),
            new MobConditionContext { Entities = registry }));
    }

    [Fact]
    public void MasterAttacked_MasterMob_NoAttackers_False()
    {
        var master = MakeMob(id: 5);
        var slave = MakeMob(id: 6);
        slave.MasterId = master.Id;

        var registry = new FakeRegistry();
        registry.Add(master);
        registry.Add(slave);

        var ev = new MasterAttackedCondition();
        Assert.False(ev.IsMet(slave, MakeEntry(MobSkillCondition.MasterAttacked),
            new MobConditionContext { Entities = registry }));
    }

    [Fact]
    public void MasterAttacked_PcMaster_WithAttackers_True()
    {
        // T5.1a — homunculus / mercenary master is a PC. The MobMaster
        // path reads through PlayerEntity.AttackerLog which mirrors the
        // rAthena PC side of unit_counttargeted.
        var pcMaster = new PlayerEntity(
            characterId: 7, accountId: 7, name: "Owner",
            sessionId: Guid.NewGuid(), mapId: 1, x: 0, y: 0);
        pcMaster.AttackerLog.Record(new EntityId(101), damage: 25);

        var slave = MakeMob(id: 9);
        slave.MasterId = pcMaster.Id;

        var registry = new FakeRegistry();
        registry.Add(pcMaster);
        registry.Add(slave);

        var ev = new MasterAttackedCondition();
        Assert.True(ev.IsMet(slave, MakeEntry(MobSkillCondition.MasterAttacked),
            new MobConditionContext { Entities = registry }));
    }

    [Fact]
    public void MasterAttacked_PcMaster_NoAttackers_False()
    {
        var pcMaster = new PlayerEntity(
            characterId: 7, accountId: 7, name: "Owner",
            sessionId: Guid.NewGuid(), mapId: 1, x: 0, y: 0);
        // AttackerLog empty.

        var slave = MakeMob(id: 9);
        slave.MasterId = pcMaster.Id;

        var registry = new FakeRegistry();
        registry.Add(pcMaster);
        registry.Add(slave);

        var ev = new MasterAttackedCondition();
        Assert.False(ev.IsMet(slave, MakeEntry(MobSkillCondition.MasterAttacked),
            new MobConditionContext { Entities = registry }));
    }

    // ---- MSC_ALCHEMIST ----

    [Fact]
    public void Alchemist_NonSummoned_NeverFires()
    {
        var mob = MakeMob(id: 1);
        mob.SpecialAi = MobSpecialAi.None;
        mob.Hp = 50;

        Assert.False(new AlchemistCondition().IsMet(mob,
            MakeEntry(MobSkillCondition.Alchemist), MobConditionContext.Empty));
    }

    [Fact]
    public void Alchemist_Summoned_FullHp_DoesNotFire()
    {
        var mob = MakeMob(id: 1);
        mob.SpecialAi = MobSpecialAi.Sphere;
        Assert.Equal(mob.MaxHp, mob.Hp);

        Assert.False(new AlchemistCondition().IsMet(mob,
            MakeEntry(MobSkillCondition.Alchemist), MobConditionContext.Empty));
    }

    [Fact]
    public void Alchemist_Summoned_Damaged_NotTrickCasting_Fires()
    {
        var mob = MakeMob(id: 1);
        mob.SpecialAi = MobSpecialAi.Sphere;
        mob.Hp = mob.MaxHp - 1;

        Assert.True(new AlchemistCondition().IsMet(mob,
            MakeEntry(MobSkillCondition.Alchemist), MobConditionContext.Empty));
    }

    [Fact]
    public void Alchemist_Summoned_Damaged_TrickCasting_DoesNotFire()
    {
        var mob = MakeMob(id: 1);
        mob.SpecialAi = MobSpecialAi.Sphere;
        mob.Hp = mob.MaxHp - 1;
        mob.TrickCasting = 1;

        Assert.False(new AlchemistCondition().IsMet(mob,
            MakeEntry(MobSkillCondition.Alchemist), MobConditionContext.Empty));
    }

    // ---- helpers ----

    private static MobEntity MakeMob(int id)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 100 };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(id), db, origin, mapId: 1, x: 0, y: 0);
        mob.MaxHp = 100;
        mob.Hp = 100;
        return mob;
    }

    private static MobSkillEntry MakeEntry(MobSkillCondition cond, int cond2 = 0)
        => new()
        {
            SkillId = 1,
            SkillLevel = 1,
            State = MobSkillState.Any,
            Condition = cond,
            Cond2 = cond2,
        };

    private sealed class FakeRegistry : IEntityRegistry
    {
        private readonly Dictionary<EntityId, Entity> _byId = new();
        public int Count => _byId.Count;
        public void Add(Entity e) => _byId[e.Id] = e;
        public Entity? Remove(EntityId id) { _byId.Remove(id, out var e); return e; }
        public Entity? Get(EntityId id) => _byId.GetValueOrDefault(id);
        public bool Contains(EntityId id) => _byId.ContainsKey(id);
        public IEnumerable<Entity> All() => _byId.Values;
        public void Move(EntityId id, short newX, short newY) { }
        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask)
            => Array.Empty<Entity>();
        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask)
            => Array.Empty<Entity>();
    }
}
