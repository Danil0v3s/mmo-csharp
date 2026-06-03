using System.Collections.Concurrent;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Handlers.Pet;
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
/// GP-PET — pet-menu client bridge: CZ_COMMAND_PET → PetOpsService.Menu → ZC_PROPERTY_PET /
/// ZC_CHANGESTATE_PET emits (clif_send_petstatus / clif_send_petdata).
/// </summary>
public class PetMenuEmitTests
{
    private const int PoringClass = 1002;

    [Fact]
    public void Menu_info_emits_pet_status_panel()
    {
        var (svc, pc, session, pet, _) = Build(intimacy: 500, hunger: 60);

        Assert.Equal(0, svc.Menu(pc, 0)); // 0 = pet information

        var b = Outbound(session).Single(x => Header(x) == (ushort)PacketHeader.ZC_PROPERTY_PET);
        Assert.Equal(37, b.Length);
        Assert.Equal("Poring", ReadCString(b, 2, 24));
        Assert.Equal(0, b[26]);                             // not renamed
        Assert.Equal(60, BitConverter.ToInt16(b, 29));      // hunger (after name24 + renamed1 + level2)
        Assert.Equal(500, BitConverter.ToInt16(b, 31));     // intimacy
        Assert.Equal(PoringClass, BitConverter.ToInt16(b, 35)); // class
    }

    [Fact]
    public void Menu_feed_raises_hunger_intimacy_and_emits_changestate()
    {
        var (svc, pc, session, pet, _) = Build(intimacy: 500, hunger: 60);

        Assert.Equal(1, svc.Menu(pc, 1)); // 1 = feed
        Assert.Equal(85, pet.Hunger);     // +25 food step
        Assert.Equal(510, pet.Intimacy);  // +10

        var changes = Outbound(session).Where(x => Header(x) == (ushort)PacketHeader.ZC_CHANGESTATE_PET).ToList();
        var hunger = changes.Single(x => x[2] == (byte)PetDataType.Hunger);
        var intim = changes.Single(x => x[2] == (byte)PetDataType.Intimacy);
        Assert.Equal(pet.Id.Value, BitConverter.ToInt32(hunger, 3));
        Assert.Equal(85, BitConverter.ToInt32(hunger, 7));
        Assert.Equal(510, BitConverter.ToInt32(intim, 7));
    }

    [Fact]
    public void Menu_return_recalls_pet()
    {
        var (svc, pc, _, _, recall) = Build();
        Assert.Equal(0, svc.Menu(pc, 3)); // 3 = return to egg
        Assert.True(recall.Recalled);
    }

    [Fact]
    public void Menu_unequip_clears_accessory_and_emits()
    {
        var (svc, pc, session, pet, _) = Build();
        pet.EquipItemId = 2301;

        Assert.Equal(0, svc.Menu(pc, 4)); // 4 = unequip accessory
        Assert.Equal(0u, pet.EquipItemId);
        var acc = Outbound(session).Single(x => Header(x) == (ushort)PacketHeader.ZC_CHANGESTATE_PET && x[2] == (byte)PetDataType.Accessory);
        Assert.Equal(0, BitConverter.ToInt32(acc, 7));
    }

    [Fact]
    public void Menu_on_runaway_pet_is_rejected()
    {
        var (svc, pc, _, pet, _r) = Build(intimacy: 0);
        Assert.Equal(1, svc.Menu(pc, 1)); // rAthena returns 1 when the pet is lost (intimate 0)
    }

    [Fact]
    public async Task Handler_routes_menu_type_to_service()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        var ops = new RecordingPetOps();
        var handler = new PetMenuHandler(registry, ops, NullLogger<PetMenuHandler>.Instance);

        await handler.HandleAsync(session, Packet(3));
        Assert.Equal(3, ops.LastMenu);
    }

    // --- helpers ---

    private static (PetOpsService svc, PlayerEntity pc, MapSessionData session, PetEntity pet, RecallTrackingPet recall) Build(
        ushort intimacy = 250, ushort hunger = 80)
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "Owner", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);

        var db = new MobDbEntry { Id = PoringClass, AegisName = "PORING", Name = "Poring", Level = 1 };
        var pet = new PetEntity(new EntityId(5000), db, pc.MapId, 50, 50)
        { PetName = "Poring", Intimacy = intimacy, Hunger = hunger, MasterId = pc.Id };
        registry.Add(pet);

        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        var sessions = new FakeSessions(pc.Id, session);
        var client = new PetClientService(sessions, NullLogger<PetClientService>.Instance);
        var recallPet = new RecallTrackingPet(pet);
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance, null, null, null, new Random(0),
            sessions: sessions, pet: recallPet, client: client, entities: registry);        return (svc, pc, session, pet, recallPet);
    }

    private static CZ_COMMAND_PET Packet(byte type)
    {
        var p = new CZ_COMMAND_PET();
        typeof(CZ_COMMAND_PET).GetProperty("Type")!.SetValue(p, type);
        return p;
    }

    private static ushort Header(byte[] b) => (ushort)(b[0] | (b[1] << 8));

    private static string ReadCString(byte[] b, int off, int width)
    {
        var end = off; while (end < off + width && b[end] != 0) end++;
        return System.Text.Encoding.ASCII.GetString(b, off, end - off);
    }

    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
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

    private sealed class FakeSessions(EntityId id, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
    }

    private sealed class RecallTrackingPet(PetEntity pet) : IPetService
    {
        public bool Recalled;
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0) => pet;
        public void Recall(PlayerEntity owner) => Recalled = true;
        public void Tick(long nowTick) { }
        public Core.Server.IPC.PetData? SerializeSnapshot(int petId) => null;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId) { petId = (int)pet.PetId; return true; }
    }

    private sealed class RecordingPetOps : IPetOpsService
    {
        public int LastMenu = -1;
        public int Menu(PlayerEntity master, int choice) { LastMenu = choice; return 0; }

        // unused surface
        public bool DataInit(PlayerEntity master, byte flag) => false;
        public bool CreateEgg(PlayerEntity master, int itemId) => false;
        public bool GetEgg(PlayerEntity master, int classId, int itemId, byte gender) => false;
        public bool ReturnEgg(PlayerEntity master) => false;
        public int EggSearch(PlayerEntity master, int eggId) => -1;
        public int SelectEgg(PlayerEntity master, short eggIndex) => 0;
        public int Food(PlayerEntity master) => 0;
        public int HungryVal(PlayerEntity master) => 0;
        public int HungryTimerDelete(PlayerEntity master) => 0;
        public int AttackSkill(PlayerEntity master, EntityId targetId) => 0;
        public int TargetCheck(PlayerEntity master, EntityId targetId, int isType) => 0;
        public void UnlockTarget(PlayerEntity master) { }
        public void Evolution(PlayerEntity master, int evoTo) { }
        public bool EvolutionRequirementsCheck(PlayerEntity master, int evoTo) => false;
        public int BirthProcess(PlayerEntity master) => 0;
        public int ChangeName(PlayerEntity master, string newName) => 0;
        public int ChangeNameAck(PlayerEntity master, byte flag) => 0;
        public int RecvPetData(PlayerEntity master) => 0;
        public int EquipItem(PlayerEntity master, int inventoryIndex) => 0;
        public int ScCheck(PlayerEntity master, int statusType) => 0;
        public void SetIntimate(PlayerEntity master, int delta) { }
        public void LootItemDrop(PlayerEntity master, int amount) { }
        public void ClearSupportBonuses(PlayerEntity master) { }
        public bool AddAutoBonus(PlayerEntity master, string bonus, int rate, int duration, ushort flag) => false;
        public void DelAutoBonus(PlayerEntity master) { }
        public void ExeAutoBonus(PlayerEntity master) { }
        public void CatchProcessStart(PlayerEntity master, int targetMobClass) { }
        public void CatchProcessEnd(PlayerEntity master, EntityId targetId) { }
        public void Reload() { }
    }
}
