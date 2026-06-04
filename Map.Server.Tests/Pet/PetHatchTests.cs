using Core.Database.Entities;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Handlers.Pet;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Pet;
using Map.Server.Pet.PetOps;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using PetEntity = Map.Server.Entities.PetEntity;

namespace Map.Server.Tests.Pet;

/// <summary>
/// GP-PET — hatch flow: use egg → ZC_PETEGG_LIST (clif_sendegg) → CZ_SELECT_PETEGG → pet_select_egg →
/// BirthProcess. Plus the IT_PETEGG item-use short-circuit (opens the list, doesn't consume the egg).
/// </summary>
public class PetHatchTests
{
    private const int PoringClass = 1002;
    private const uint EggItemId = 9001;

    [Fact]
    public void OpenEggList_lists_pet_eggs_by_client_index()
    {
        var (svc, pc, client, _) = Build(EggAt(3));
        svc.OpenEggList(pc);

        Assert.Single(client.EggLists);
        Assert.Equal(new short[] { 5 }, client.EggLists[0]); // client index = server index(3) + 2
    }

    [Fact]
    public void OpenEggList_skips_non_egg_items()
    {
        var apple = new InventoryItem { Id = 1, ServerIndex = 0, NameId = 501, Amount = 5 };
        var (svc, pc, client, _) = Build(apple, EggAt(1));
        svc.OpenEggList(pc);

        Assert.Equal(new short[] { 3 }, client.EggLists[0]); // only the egg (server index 1 + 2)
    }

    [Fact]
    public void SelectEgg_hatches_and_consumes_egg()
    {
        var (svc, pc, _, pet) = Build(EggAt(2));
        Assert.Equal(0, svc.SelectEgg(pc, 2));
        Assert.Equal(1, pet.SummonCalls);
        Assert.Equal(PoringClass, pet.LastClass);
    }

    [Fact]
    public async Task SelectPetEggHandler_converts_client_index_and_hatches()
    {
        var (svc, pc, _, pet) = Build(EggAt(2));
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        registry.Add(pc);
        var handler = new SelectPetEggHandler(registry, svc, NullLogger<SelectPetEggHandler>.Instance);

        var p = new Core.Server.Packets.In.CZ.CZ_SELECT_PETEGG();
        typeof(Core.Server.Packets.In.CZ.CZ_SELECT_PETEGG).GetProperty("Index")!.SetValue(p, (short)4); // client index → server 2
        await handler.HandleAsync(Session(pc), p);

        Assert.Equal(1, pet.SummonCalls);
    }

    [Fact]
    public void Using_an_egg_item_opens_the_list_and_does_not_consume_it()
    {
        var egg = EggAt(0);
        var inv = new List<InventoryItem> { egg };
        var session = SessionWith(inv);
        var pc = PcFor(session);
        var petOps = new RecordingPetOps();
        // The egg short-circuits before any effect lookup, so the registry's status service is unused.
        var use = new ItemUseService(new EggCatalog(),
            new Map.Server.Inventory.ItemEffects.ItemEffectRegistry(null!),
            new FakeSessions(pc.Id, session), NullLogger<ItemUseService>.Instance, hookDispatcher: null, petOps: petOps);

        Assert.True(use.UseItem(pc, 0));
        Assert.Equal(1, petOps.OpenEggListCalls); // bpet flow
        Assert.Equal(1u, egg.Amount);             // egg NOT consumed
        Assert.Single(session.Inventory!);
    }

    // --- helpers ---

    private static InventoryItem EggAt(int slot) =>
        new() { Id = slot + 1, ServerIndex = slot, NameId = EggItemId, Amount = 1 };

    private static (PetOpsService svc, PlayerEntity pc, RecordingClient client, FakePet pet) Build(params InventoryItem[] inv)
    {
        var session = SessionWith(inv.ToList());
        var pc = PcFor(session);
        var client = new RecordingClient();
        var pet = new FakePet();
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance,
            new FakeMobDb(), new FakeItems(), null, new Random(0),
            sessions: new FakeSessions(pc.Id, session), pet: pet, client: client);
        svc.SeedCatalogForTest(new PetDbEntity { MobAegis = "PORING", EggItem = "PORING_EGG", CaptureRate = 10000, IntimacyStart = 250, Fullness = 80 });
        svc.InvalidateEggIndexForTest();
        return (svc, pc, client, pet);
    }

    private static MapSessionData SessionWith(List<InventoryItem> inv)
    {
        var sockets = TestSocketFactory.CreateSocketPair();
        return new MapSessionData(sockets.ServerSide, 30000, new Core.Server.Packets.PacketSystem().Factory,
            new Core.Server.Packets.PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = new EntityId(1), Inventory = inv };
    }

    private static PlayerEntity PcFor(MapSessionData session)
        => new(1, 1, "Owner", Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1 };

    private static MapSessionData Session(PlayerEntity pc) => SessionWith(new List<InventoryItem> { EggAt(2) });

    private sealed class RecordingClient : IPetClientService
    {
        public readonly List<short[]> EggLists = new();
        public void SendPetStatus(PlayerEntity master, PetEntity pet) { }
        public void SendPetData(PlayerEntity master, PetEntity pet, PetDataType type, int data) { }
        public void SendCatchProcess(PlayerEntity master) { }
        public void SendPetRoulette(PlayerEntity master, bool success) { }
        public void SendEggList(PlayerEntity master, IReadOnlyList<short> clientIndices) => EggLists.Add(clientIndices.ToArray());
    }

    private sealed class FakePet : IPetService
    {
        public int SummonCalls; public int LastClass;
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0, long petId = 0, int intimacy = -1, int hunger = -1, bool renamed = false)
        { SummonCalls++; LastClass = petClassId; return null; }
        public void Recall(PlayerEntity owner) { }
        public void Tick(long nowTick) { }
        public Core.Server.IPC.PetData? SerializeSnapshot(int petId) => null;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId) { petId = 0; return false; }
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
        public ItemEntity? Get(uint itemId) => itemId == EggItemId ? new ItemEntity { Id = EggItemId } : null;
        public ItemEntity? GetByAegisName(string aegisName) => string.Equals(aegisName, "PORING_EGG", StringComparison.OrdinalIgnoreCase) ? new ItemEntity { Id = EggItemId } : null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class EggCatalog : IItemCatalog
    {
        public int Count => 1;
        public ItemEntity? Get(uint itemId) => itemId == EggItemId ? new ItemEntity { Id = EggItemId, Type = "Petegg" } : null;
        public ItemEntity? GetByAegisName(string aegisName) => null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class FakeSessions(EntityId id, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => session;
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

    private sealed class RecordingPetOps : IPetOpsService
    {
        public int OpenEggListCalls;
        public void OpenEggList(PlayerEntity master) => OpenEggListCalls++;

        public bool DataInit(PlayerEntity master, byte flag) => false;
        public bool CreateEgg(PlayerEntity master, int itemId) => false;
        public bool GetEgg(PlayerEntity master, int classId, int eggItemId, int petId) => false;
        public bool ReturnEgg(PlayerEntity master) => false;
        public int EggSearch(PlayerEntity master, int eggId) => -1;
        public int SelectEgg(PlayerEntity master, short eggSlot) => 0;
        public int Food(PlayerEntity master) => 0;
        public int HungryVal(PlayerEntity master) => 0;
        public int HungryTimerDelete(PlayerEntity master) => 0;
        public int AttackSkill(PlayerEntity master, EntityId targetId) => 0;
        public int TargetCheck(PlayerEntity master, EntityId targetId, int isType) => 0;
        public void UnlockTarget(PlayerEntity master) { }
        public void Evolution(PlayerEntity master, int evoTo) { }
        public bool EvolutionRequirementsCheck(PlayerEntity master, int evoTo) => false;
        public int BirthProcess(PlayerEntity master, int eggSlot) => 0;
        public int ChangeName(PlayerEntity master, string newName) => 0;
        public int ChangeNameAck(PlayerEntity master, byte flag) => 0;
        public void Emotion(PlayerEntity master, int data) { }
        public int Menu(PlayerEntity master, int choice) => 0;
        public int RecvPetData(PlayerEntity master) => 0;
        public int EquipItem(PlayerEntity master, int inventoryIndex) => 0;
        public int ScCheck(PlayerEntity master, int statusType) => 0;
        public void SetIntimate(PlayerEntity master, int delta) { }
        public void LootItemDrop(PlayerEntity master) { }
        public void ClearSupportBonuses(PlayerEntity master) { }
        public bool AddAutoBonus(PlayerEntity master, string bonus, int rate, int duration, ushort flag) => false;
        public void DelAutoBonus(PlayerEntity master) { }
        public void ExeAutoBonus(PlayerEntity master) { }
        public void CatchProcessStart(PlayerEntity master, int targetMobClass) { }
        public void CatchProcessEnd(PlayerEntity master, EntityId targetId) { }
        public void Reload() { }
    }
}
