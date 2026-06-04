using Core.Database.Entities;
using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Pet;
using Map.Server.Pet.PetOps;
using Map.Server.Services.Intif;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Fakes;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using PetEntity = Map.Server.Entities.PetEntity;

namespace Map.Server.Tests.Pet;

/// <summary>
/// GP-PET (FEATURE-27) — hatch READ side: a bound egg (CARD0_PET) loads the saved pet row
/// (intif_request_petdata) and hatches with the persisted intimacy/hunger/name/pet_id; an unbound egg
/// hatches fresh; a missing char row falls back to a fresh hatch.
/// </summary>
public class PetHatchLoadTests
{
    private const int PoringClass = 1002;
    private const uint EggItemId = 9001;

    [Fact]
    public void Hatching_a_bound_egg_loads_the_saved_pet_state()
    {
        var saved = new PetData { PetId = 4242, ClassId = PoringClass, Name = "Fluffy", Intimacy = 999, Hungry = 42, RenameFlag = 1, EggItemId = (int)EggItemId };
        var (svc, pc, pet) = Build(load: saved, egg: BoundEgg(2, 4242));

        Assert.Equal(0, svc.SelectEgg(pc, 2));

        Assert.Equal(1, pet.Calls);
        Assert.Equal(PoringClass, pet.LastClass);
        Assert.Equal("Fluffy", pet.LastName);
        Assert.Equal(4242L, pet.LastPetId);
        Assert.Equal(999, pet.LastIntimacy);
        Assert.Equal(42, pet.LastHunger);
        Assert.True(pet.LastRenamed);
    }

    [Fact]
    public void Hatching_an_unbound_egg_hatches_fresh()
    {
        var (svc, pc, pet) = Build(load: null, egg: FreshEgg(2));

        Assert.Equal(0, svc.SelectEgg(pc, 2));

        Assert.Equal(1, pet.Calls);
        Assert.Equal(PoringClass, pet.LastClass);
        Assert.Equal(-1, pet.LastIntimacy); // fresh-hatch default sentinel (no loaded value)
        Assert.Equal(0L, pet.LastPetId);
    }

    [Fact]
    public void Bound_egg_with_no_saved_row_falls_back_to_fresh_hatch()
    {
        var (svc, pc, pet) = Build(load: null, egg: BoundEgg(2, 7777)); // PetLoad returns null

        Assert.Equal(0, svc.SelectEgg(pc, 2));

        Assert.Equal(1, pet.Calls);
        Assert.Equal(PoringClass, pet.LastClass); // fell back to the egg's class
        Assert.Equal(0L, pet.LastPetId);
    }

    // --- helpers ---

    private static InventoryItem FreshEgg(int slot) =>
        new() { Id = slot + 1, ServerIndex = slot, NameId = EggItemId, Amount = 1 };

    private static InventoryItem BoundEgg(int slot, int petId)
    {
        var (c0, c1, c2) = PetEggCard.Bind(petId);
        return new InventoryItem { Id = slot + 1, ServerIndex = slot, NameId = EggItemId, Amount = 1, Card0 = c0, Card1 = c1, Card2 = c2 };
    }

    private static (PetOpsService svc, PlayerEntity pc, CapturingPet pet) Build(PetData? load, InventoryItem egg)
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "Owner", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var session = new MapSessionData(TestSocketFactory.CreateSocketPair().ServerSide, 30000,
            new Core.Server.Packets.PacketSystem().Factory, new Core.Server.Packets.PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id, Inventory = new List<InventoryItem> { egg } };
        var pet = new CapturingPet();
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance,
            new FakeMobDb(), new FakeItems(), new LoadIntif(load), new Random(0),
            sessions: new FakeSessions(pc.Id, session), pet: pet, entities: registry);
        svc.SeedCatalogForTest(new PetDbEntity { MobAegis = "PORING", EggItem = "PORING_EGG", CaptureRate = 10000, IntimacyStart = 250, Fullness = 80 });
        svc.InvalidateEggIndexForTest();
        return (svc, pc, pet);
    }

    private sealed class CapturingPet : IPetService
    {
        public int Calls; public int LastClass; public string? LastName; public long LastPetId;
        public int LastIntimacy = -2; public int LastHunger = -2; public bool LastRenamed;
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0, long petId = 0, int intimacy = -1, int hunger = -1, bool renamed = false)
        {
            Calls++; LastClass = petClassId; LastName = petName; LastPetId = petId;
            LastIntimacy = intimacy; LastHunger = hunger; LastRenamed = renamed;
            return null;
        }
        public void Recall(PlayerEntity owner) { }
        public void Tick(long nowTick) { }
        public PetData? SerializeSnapshot(int petId) => null;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId) { petId = 0; return false; }
    }

    private sealed class LoadIntif : NoOpIntifService
    {
        private readonly PetData? _load;
        public LoadIntif(PetData? load) => _load = load;
        public override System.Threading.Tasks.Task<PetData?> PetLoadAsync(int petId, int accountId, int charId, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.FromResult(_load);
    }

    private sealed class FakeSessions(EntityId id, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
    }

    private sealed class FakeMobDb : IMobDb
    {
        private readonly MobDbEntry _poring = new() { Id = PoringClass, AegisName = "PORING", Name = "Poring", Level = 1 };
        public int Count => 1;
        public MobDbEntry? Get(int classId) => classId == PoringClass ? _poring : null;
        public MobDbEntry? GetByAegisName(string aegisName) => string.Equals(aegisName, "PORING", StringComparison.OrdinalIgnoreCase) ? _poring : null;
        public IEnumerable<MobDbEntry> All() => new[] { _poring };
        public void Reload() { }
    }

    private sealed class FakeItems : IItemCatalog
    {
        public int Count => 1;
        public ItemEntity? Get(uint itemId) => new() { Id = itemId };
        public ItemEntity? GetByAegisName(string aegisName) => string.Equals(aegisName, "PORING_EGG", StringComparison.OrdinalIgnoreCase) ? new ItemEntity { Id = EggItemId } : null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) => _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
