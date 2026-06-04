using Core.Database.Entities;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Pet;
using Map.Server.Pet.PetOps;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Pet;

/// <summary>
/// GP-PET (FEATURE-27) — pet_id ↔ egg-card binding: the catch flow grants an egg whose card slots
/// carry the persistent pet_id (CARD0_PET), so a saved pet survives being re-hatched.
/// </summary>
public class PetEggBindingTests
{
    private const int PoringClass = 1002;
    private const uint EggItemId = 9001;

    [Fact]
    public void PetEggCard_round_trips_pet_id_through_card_slots()
    {
        var (c0, c1, c2) = PetEggCard.Bind(0x0012_3456);
        Assert.Equal(0x0100u, c0);                 // CARD0_PET marker
        Assert.Equal(0x3456u, c1);                 // low word
        Assert.Equal(0x0012u, c2);                 // high word

        var egg = new InventoryItem { Card0 = c0, Card1 = c1, Card2 = c2 };
        Assert.Equal(0x0012_3456, PetEggCard.ReadPetId(egg));
    }

    [Fact]
    public void ReadPetId_returns_null_for_a_plain_item()
    {
        Assert.Null(PetEggCard.ReadPetId(new InventoryItem { Card0 = 0 }));
    }

    [Fact]
    public void GetEgg_grants_egg_bound_to_the_pet_id()
    {
        var (svc, pc, inv) = Build();

        Assert.True(svc.GetEgg(pc, PoringClass, (int)EggItemId, petId: 4242));

        var grant = inv.Granted.Single();
        Assert.Equal(EggItemId, grant.NameId);
        Assert.Equal(PetEggCard.Card0Pet, grant.Card0);
        // pet_id 4242 = 0x1092 → low word 0x1092, high word 0.
        Assert.Equal(4242u, grant.Card1);
        Assert.Equal(0u, grant.Card2);
    }

    private static (PetOpsService svc, PlayerEntity pc, RecordingInventory inv) Build()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "Owner", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var session = new MapSessionData(TestSocketFactory.CreateSocketPair().ServerSide, 30000,
            new Core.Server.Packets.PacketSystem().Factory, new Core.Server.Packets.PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        var inv = new RecordingInventory();
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance, null, null, null, new Random(0),
            sessions: new FakeSessions(pc.Id, session), inventory: inv, pet: null, entities: registry);
        return (svc, pc, inv);
    }

    private sealed class RecordingInventory : IInventoryService
    {
        public readonly List<(uint NameId, uint Card0, uint Card1, uint Card2)> Granted = new();
        public bool GiveItem(MapSessionData session, uint nameId, int amount) { Granted.Add((nameId, 0, 0, 0)); return true; }
        public bool GiveItemWithCards(MapSessionData session, uint nameId, int amount, uint card0, uint card1, uint card2, uint card3)
        { Granted.Add((nameId, card0, card1, card2)); return true; }
        public System.Threading.Tasks.Task LoadAsync(MapSessionData session, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public void SendInventoryList(MapSessionData session) { }
    }

    private sealed class FakeSessions(EntityId id, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
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
