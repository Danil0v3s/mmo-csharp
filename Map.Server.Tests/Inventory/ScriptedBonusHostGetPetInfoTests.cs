using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Mob;
using Map.Server.World;

namespace Map.Server.Tests.Inventory;

/// <summary>
/// GAP-1 acceptance: <see cref="ScriptedBonusHost.getpetinfo"/> resolves
/// the active pet via <see cref="IEntityRegistry"/> and dispatches on the
/// rAthena <c>PETINFO_*</c> enum (both string and integer forms). The
/// pet-egg item ports in <c>scripts/items/manual/skipped.ts</c> rely on
/// this — without it, getpetinfo() would silently return 0 (via the
/// Proxy fallback) and every per-egg switch case would skip.
/// </summary>
public class ScriptedBonusHostGetPetInfoTests
{
    /// <summary>Minimal IEntityRegistry that holds entities in a flat list. Spatial / map-id queries are unused by getpetinfo.</summary>
    private sealed class FakeEntityRegistry : IEntityRegistry
    {
        private readonly List<Entity> _list = new();
        public int Count => _list.Count;
        public void Add(Entity e) => _list.Add(e);
        public Entity? Remove(EntityId id) { var e = _list.FirstOrDefault(x => x.Id == id); if (e != null) _list.Remove(e); return e; }
        public Entity? Get(EntityId id) => _list.FirstOrDefault(x => x.Id == id);
        public bool Contains(EntityId id) => _list.Any(x => x.Id == id);
        public void Move(EntityId id, short newX, short newY) { }
        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask) => Array.Empty<Entity>();
        public IEnumerable<Entity> All() => _list;
    }

    private static MobDbEntry FakeMobDb(int classId, string aegis = "PORING")
        => new() { Id = classId, AegisName = aegis, Name = aegis };

    private static (PlayerEntity pc, FakeEntityRegistry reg) MakeSetup(int eggId, int classId = 1002)
    {
        var pc = new PlayerEntity(
            characterId: 1, accountId: 1, name: "PetOwner",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0)
        {
            Level = 99, JobLevel = 50,
        };
        var reg = new FakeEntityRegistry();
        var pet = new PetEntity(
            new EntityId(9001), FakeMobDb(classId), 0, 0, 0)
        {
            PetName = "Pip",
            EggId = eggId,
            Intimacy = 750,
            Hunger = 60,
        };
        pet.MasterId = pc.Id;
        reg.Add(pet);
        return (pc, reg);
    }

    [Fact]
    public void getpetinfo_egg_id_string_form_returns_PetEntity_EggId()
    {
        var (pc, reg) = MakeSetup(eggId: 9088 /* Angeling */);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: reg);
        Assert.Equal(9088, host.getpetinfo("PETINFO_EGGID"));
    }

    [Fact]
    public void getpetinfo_egg_id_integer_form_returns_PetEntity_EggId()
    {
        var (pc, reg) = MakeSetup(eggId: 9119);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: reg);
        // PETINFO_EGGID = 8 in rAthena script_constants.hpp.
        Assert.Equal(9119, host.getpetinfo(8));
    }

    [Fact]
    public void getpetinfo_class_and_other_fields_resolve()
    {
        var (pc, reg) = MakeSetup(eggId: 9088, classId: 1188 /* Angeling mob class */);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: reg);
        Assert.Equal(1188, host.getpetinfo("PETINFO_CLASS"));
        Assert.Equal("Pip", host.getpetinfo("PETINFO_NAME"));
        Assert.Equal(750,  host.getpetinfo("PETINFO_INTIMATE"));
        Assert.Equal(60,   host.getpetinfo("PETINFO_HUNGRY"));
    }

    [Fact]
    public void getpetinfo_returns_zero_when_no_pet_present()
    {
        var pc = new PlayerEntity(
            characterId: 2, accountId: 1, name: "NoPet",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0);
        var reg = new FakeEntityRegistry();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: reg);
        Assert.Equal(0, host.getpetinfo("PETINFO_EGGID"));
        // rAthena returns "null" string for PETINFO_NAME with no pet.
        Assert.Equal("null", host.getpetinfo("PETINFO_NAME"));
    }

    [Fact]
    public void getpetinfo_returns_zero_when_entity_registry_not_injected()
    {
        // Back-compat: pre-GAP-1 callers (the test smoke harnesses) didn't
        // pass an IEntityRegistry. The host must still answer 0 instead of
        // throwing — otherwise CONV-4's onEquip smoke test would break.
        var pc = new PlayerEntity(
            characterId: 3, accountId: 1, name: "NoReg",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        Assert.Equal(0, host.getpetinfo("PETINFO_EGGID"));
    }

    [Fact]
    public void getpetinfo_ignores_pets_belonging_to_other_players()
    {
        var (pc, reg) = MakeSetup(eggId: 9088);
        // Re-bind the pet to a different master.
        var other = new PlayerEntity(
            characterId: 999, accountId: 99, name: "Other",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0);
        var pet = (PetEntity)reg.All().Single();
        pet.MasterId = other.Id;
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: reg);
        Assert.Equal(0, host.getpetinfo("PETINFO_EGGID"));
    }
}
