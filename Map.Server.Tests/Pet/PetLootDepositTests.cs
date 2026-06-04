using Core.Database.Entities;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Pet.PetOps;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using PetEntity = Map.Server.Entities.PetEntity;

namespace Map.Server.Tests.Pet;

/// <summary>
/// GP-PET (FEATURE-28) — loot deposit: pet_lootitem_drop hands the pet's loot bag to the owner's
/// inventory (and ReturnEgg deposits before recall).
/// </summary>
public class PetLootDepositTests
{
    private const int PoringClass = 1002;

    [Fact]
    public void LootItemDrop_delivers_bag_to_owner_and_clears_delivered()
    {
        var (svc, pc, inv, pet) = Build();
        pet.LootItems.Add(new MobLootSlot(909, 3, PoringClass));
        pet.LootItems.Add(new MobLootSlot(501, 1, PoringClass));

        svc.LootItemDrop(pc);

        Assert.Equal(2, inv.Gives.Count);
        Assert.Contains((909u, 3), inv.Gives);
        Assert.Contains((501u, 1), inv.Gives);
        Assert.Empty(pet.LootItems); // delivered → bag cleared
    }

    [Fact]
    public void LootItemDrop_keeps_undeliverable_items_in_bag()
    {
        var (svc, pc, inv, pet) = Build();
        inv.Accept = false; // bag full / overweight → GiveItem fails
        pet.LootItems.Add(new MobLootSlot(909, 1, PoringClass));

        svc.LootItemDrop(pc);

        Assert.Single(pet.LootItems); // kept, not lost
    }

    [Fact]
    public void ReturnEgg_deposits_loot_before_recall()
    {
        var (svc, pc, inv, pet) = Build();
        pet.LootItems.Add(new MobLootSlot(909, 2, PoringClass));

        Assert.True(svc.ReturnEgg(pc));
        Assert.Single(inv.Gives);
        Assert.Empty(pet.LootItems);
    }

    private static (PetOpsService svc, PlayerEntity pc, RecordingInventory inv, PetEntity pet) Build()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "Owner", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var db = new MobDbEntry { Id = PoringClass, AegisName = "PORING", Name = "Poring", Level = 1 };
        var pet = new PetEntity(new EntityId(5000), db, pc.MapId, 50, 50) { PetName = "Poring", MasterId = pc.Id };
        registry.Add(pet);
        var session = new MapSessionData(TestSocketFactory.CreateSocketPair().ServerSide, 30000,
            new Core.Server.Packets.PacketSystem().Factory, new Core.Server.Packets.PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        var inv = new RecordingInventory();
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance, null, null, null, new Random(0),
            sessions: new FakeSessions(pc.Id, session), inventory: inv, pet: new NoOpPet(), entities: registry);
        return (svc, pc, inv, pet);
    }

    private sealed class RecordingInventory : IInventoryService
    {
        public bool Accept = true;
        public readonly List<(uint NameId, int Amount)> Gives = new();
        public bool GiveItem(MapSessionData session, uint nameId, int amount)
        { if (!Accept) return false; Gives.Add((nameId, amount)); return true; }
        public bool GiveItemWithCards(MapSessionData session, uint nameId, int amount, uint card0, uint card1, uint card2, uint card3) => GiveItem(session, nameId, amount);
        public System.Threading.Tasks.Task LoadAsync(MapSessionData session, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public void SendInventoryList(MapSessionData session) { }
    }

    private sealed class NoOpPet : Map.Server.Pet.IPetService
    {
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0, long petId = 0, int intimacy = -1, int hunger = -1, bool renamed = false) => null;
        public void Recall(PlayerEntity owner) { }
        public void Tick(long nowTick) { }
        public Core.Server.IPC.PetData? SerializeSnapshot(int petId) => null;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId) { petId = 0; return false; }
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
