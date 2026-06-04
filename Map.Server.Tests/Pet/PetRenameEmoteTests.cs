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
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using PetEntity = Map.Server.Entities.PetEntity;

namespace Map.Server.Tests.Pet;

/// <summary>
/// GP-PET — rename (CZ_RENAME_PET → pet_change_name) + emotion (CZ_PET_ACT → clif_pet_emotion).
/// </summary>
public class PetRenameEmoteTests
{
    private const int PoringClass = 1002;

    [Fact]
    public void ChangeName_applies_name_sets_flag_and_emits_status()
    {
        var (svc, pc, session, _, pet) = Build();

        Assert.Equal(0, svc.ChangeName(pc, "Fluffy"));
        Assert.Equal("Fluffy", pet.PetName);
        Assert.True(pet.RenameFlag);
        var b = Outbound(session).Single(x => Header(x) == (ushort)PacketHeader.ZC_PROPERTY_PET);
        Assert.Equal("Fluffy", ReadCString(b, 2, 24));
        Assert.Equal(1, b[26]); // renamed flag
    }

    [Fact]
    public void ChangeName_rejects_second_rename()
    {
        var (svc, pc, _, _, pet) = Build();
        Assert.Equal(0, svc.ChangeName(pc, "Fluffy"));
        Assert.Equal(1, svc.ChangeName(pc, "Again")); // rename_flag already set
        Assert.Equal("Fluffy", pet.PetName);
    }

    [Fact]
    public void ChangeName_rejects_control_chars_and_empty()
    {
        var (svc, pc, _, _, pet) = Build();
        Assert.Equal(1, svc.ChangeName(pc, "badname")); // control char
        Assert.Equal(1, svc.ChangeName(pc, ""));               // empty
        Assert.False(pet.RenameFlag);
    }

    [Fact]
    public void Emotion_broadcasts_pet_act_to_area()
    {
        var (svc, pc, _, vis, pet) = Build();

        svc.Emotion(pc, 42);

        var sent = vis.AreaPackets.OfType<ZC_PET_ACT>().Single();
        Assert.Equal(pet.Id.Value, sent.Gid);
        Assert.Equal(42, sent.Data);
    }

    [Fact]
    public async Task RenamePetHandler_routes_name_to_service()
    {
        var (svc, pc, _, _, pet) = Build();
        var registry = RegistryWith(pc, pet);
        var handler = new RenamePetHandler(registry, svc, NullLogger<RenamePetHandler>.Instance);

        var p = new CZ_RENAME_PET();
        typeof(CZ_RENAME_PET).GetProperty("Name")!.SetValue(p, "Rex");
        await handler.HandleAsync(SessionFor(pc), p);

        Assert.Equal("Rex", pet.PetName);
    }

    [Fact]
    public async Task PetActHandler_routes_data_to_emotion()
    {
        var (svc, pc, _, vis, pet) = Build();
        var registry = RegistryWith(pc, pet);
        var handler = new PetActHandler(registry, svc, NullLogger<PetActHandler>.Instance);

        var p = new CZ_PET_ACT();
        typeof(CZ_PET_ACT).GetProperty("Data")!.SetValue(p, 7);
        await handler.HandleAsync(SessionFor(pc), p);

        Assert.Equal(7, vis.AreaPackets.OfType<ZC_PET_ACT>().Single().Data);
    }

    // --- helpers ---

    private static (PetOpsService svc, PlayerEntity pc, MapSessionData session, RecordingVisibility vis, PetEntity pet) Build()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "Owner", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var db = new MobDbEntry { Id = PoringClass, AegisName = "PORING", Name = "Poring", Level = 1 };
        var pet = new PetEntity(new EntityId(5000), db, pc.MapId, 50, 50) { PetName = "Poring", Intimacy = 500, Hunger = 80, MasterId = pc.Id };
        registry.Add(pet);
        var session = SessionFor(pc);
        var sessions = new FakeSessions(pc.Id, session);
        var client = new PetClientService(sessions, NullLogger<PetClientService>.Instance);
        var vis = new RecordingVisibility();
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance, null, null, null, new Random(0),
            sessions: sessions, pet: new NoOpPet(), client: client, entities: registry, visibility: vis);
        return (svc, pc, session, vis, pet);
    }

    private static EntityRegistry RegistryWith(PlayerEntity pc, PetEntity pet)
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var reg = new EntityRegistry(new StubWorld(map));
        reg.Add(pc); reg.Add(pet);
        return reg;
    }

    private static MapSessionData SessionFor(PlayerEntity pc)
    {
        var sockets = TestSocketFactory.CreateSocketPair();
        return new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
    }

    private static ushort Header(byte[] b) => (ushort)(b[0] | (b[1] << 8));
    private static string ReadCString(byte[] b, int off, int width)
    { var end = off; while (end < off + width && b[end] != 0) end++; return System.Text.Encoding.ASCII.GetString(b, off, end - off); }

    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private sealed class FakeSessions(EntityId id, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
    }

    private sealed class NoOpPet : IPetService
    {
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0) => null;
        public void Recall(PlayerEntity owner) { }
        public void Tick(long nowTick) { }
        public Core.Server.IPC.PetData? SerializeSnapshot(int petId) => null;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId) { petId = 0; return false; }
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

    private sealed class RecordingVisibility : IVisibilityService
    {
        public readonly List<Core.Server.Packets.OutgoingPacket> AreaPackets = new();
        public void SendToArea(Entity src, Core.Server.Packets.OutgoingPacket packet, SendTarget target = SendTarget.Area) => AreaPackets.Add(packet);
        public void SendToSelf(PlayerEntity player, Core.Server.Packets.OutgoingPacket packet) { }
        public void NotifySpawnedToArea(Entity entered) { }
        public void NotifyVanishedToArea(Entity gone, VanishReason reason) { }
        public void NotifyMoveToArea(Entity walker, short fromX, short fromY, short toX, short toY, uint startTime) { }
        public void SendCurrentViewToSelf(PlayerEntity self) { }
        public void NotifyMoveDiff(Entity walker, short fromX, short fromY, short toX, short toY) { }
        public IReadOnlyList<Entity> NewlyVisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> NewlyInvisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
    }
}
