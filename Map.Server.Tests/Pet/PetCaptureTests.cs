using System.Collections.Concurrent;
using Core.Database.Entities;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Pet;
using Map.Server.Mob;
using Map.Server.Pet;
using Map.Server.Pet.PetOps;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Fakes;
using Map.Server.Tests.Session;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Pet;

/// <summary>
/// GP-PET — capture flow: CZ_TRYCAPTURE_MONSTER → pet_catch_process_end. Validates the rAthena gates
/// + the non-legacy HP%-scaled rate + the ZC_START_CAPTURE / ZC_TRYCAPTURE_MONSTER emits.
/// </summary>
public class PetCaptureTests
{
    private const int PoringClass = 1002;
    private const uint EggItemId = 9001;

    [Fact]
    public void CatchProcessStart_arms_and_emits_start_capture()
    {
        var (svc, pc, session, _, _, _) = Build(roll: 0);
        svc.CatchProcessStart(pc, PoringClass);

        Assert.Equal(PoringClass, pc.PetCatchTargetClass);
        Assert.Contains(Outbound(session), x => Header(x) == (ushort)PacketHeader.ZC_START_CAPTURE);
    }

    [Fact]
    public void CatchProcessEnd_success_removes_mob_emits_roulette_and_creates_egg()
    {
        var (svc, pc, session, intif, mob, reg) = Build(roll: 0, capture: 10000); // rate >= roll → catch
        pc.PetCatchTargetClass = PoringClass;

        svc.CatchProcessEnd(pc, mob.Id);

        var b = Outbound(session).Single(x => Header(x) == (ushort)PacketHeader.ZC_TRYCAPTURE_MONSTER);
        Assert.Equal(1, b[2]);                       // success result
        Assert.Equal(1, intif.PetCreateCalls);       // egg created char-side
        Assert.Equal(PoringClass, intif.LastClass);
        Assert.Equal(-1, pc.PetCatchTargetClass);    // disarmed
        Assert.Null(reg.Get(mob.Id)); // mob removed from the map
    }

    [Fact]
    public void CatchProcessEnd_roll_fail_keeps_mob_and_emits_failure()
    {
        var (svc, pc, session, intif, mob, reg) = Build(roll: 9999, capture: 5000); // 9999 >= 5000 → fail
        pc.PetCatchTargetClass = PoringClass;

        svc.CatchProcessEnd(pc, mob.Id);

        var b = Outbound(session).Single(x => Header(x) == (ushort)PacketHeader.ZC_TRYCAPTURE_MONSTER);
        Assert.Equal(0, b[2]);                        // failure
        Assert.Equal(0, intif.PetCreateCalls);
        Assert.NotNull(reg.Get(mob.Id)); // mob stays
    }

    [Fact]
    public void Low_hp_raises_catch_rate_above_full_hp()
    {
        // roll 7000: full-HP rate 5000 fails (7000>=5000), 50%-HP rate 7500 succeeds (7000<7500).
        var (svcFull, pcFull, _, intifFull, mobFull, _) = Build(roll: 7000, capture: 5000, hpPct: 100);
        pcFull.PetCatchTargetClass = PoringClass;
        svcFull.CatchProcessEnd(pcFull, mobFull.Id);
        Assert.Equal(0, intifFull.PetCreateCalls); // full HP → not caught

        var (svcLow, pcLow, _, intifLow, mobLow, _) = Build(roll: 7000, capture: 5000, hpPct: 50);
        pcLow.PetCatchTargetClass = PoringClass;
        svcLow.CatchProcessEnd(pcLow, mobLow.Id);
        Assert.Equal(1, intifLow.PetCreateCalls);  // wounded → caught
    }

    [Fact]
    public void CatchProcessEnd_not_armed_fails()
    {
        var (svc, pc, session, intif, mob, _) = Build(roll: 0, capture: 10000);
        pc.PetCatchTargetClass = -1; // not armed

        svc.CatchProcessEnd(pc, mob.Id);

        Assert.Equal(0, intif.PetCreateCalls);
        Assert.Equal(0, Outbound(session).Single(x => Header(x) == (ushort)PacketHeader.ZC_TRYCAPTURE_MONSTER)[2]);
    }

    [Fact]
    public void CatchProcessEnd_wrong_class_fails()
    {
        var (svc, pc, session, intif, mob, reg) = Build(roll: 0, capture: 10000);
        pc.PetCatchTargetClass = 1063; // armed for a different mob

        svc.CatchProcessEnd(pc, mob.Id);

        Assert.Equal(0, intif.PetCreateCalls);
        Assert.NotNull(reg.Get(mob.Id));
    }

    [Fact]
    public void CatchProcessEnd_out_of_range_fails()
    {
        var (svc, pc, session, intif, mob, reg) = Build(roll: 0, capture: 10000, mobX: 80, mobY: 80); // > 5 cells
        pc.PetCatchTargetClass = PoringClass;

        svc.CatchProcessEnd(pc, mob.Id);

        Assert.Equal(0, intif.PetCreateCalls);
        Assert.NotNull(reg.Get(mob.Id));
    }

    [Fact]
    public async Task Handler_forwards_target_to_catch_end()
    {
        var (svc, pc, session, intif, mob, reg) = Build(roll: 0, capture: 10000);
        pc.PetCatchTargetClass = PoringClass;
        var handler = new PetCaptureHandler(reg, svc, NullLogger<PetCaptureHandler>.Instance);

        var p = new CZ_TRYCAPTURE_MONSTER();
        typeof(CZ_TRYCAPTURE_MONSTER).GetProperty("TargetId")!.SetValue(p, (uint)mob.Id.Value);
        await handler.HandleAsync(session, p);

        Assert.Equal(1, intif.PetCreateCalls);
    }

    // --- helpers ---

    private static (PetOpsService svc, PlayerEntity pc, MapSessionData session, RecordingIntif intif, MobEntity mob, EntityRegistry reg) Build(
        int roll, int capture = 10000, int hpPct = 100, short mobX = 52, short mobY = 50)
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "Owner", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);

        var mob = new MobEntity(new EntityId(6000), PoringClass, "Poring", pc.MapId, mobX, mobY)
        { MaxHp = 1000, Hp = Math.Max(1, 1000 * hpPct / 100) };
        registry.Add(mob);

        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        var sessions = new FakeSessions(pc.Id, session);
        var client = new PetClientService(sessions, NullLogger<PetClientService>.Instance);
        var intif = new RecordingIntif();
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance,
            new FakeMobDb(), new FakeItems(), intif, new FixedRandom(roll),
            sessions: sessions, pet: null, client: client, entities: registry, visibility: new NoOpVisibility());
        svc.SeedCatalogForTest(new PetDbEntity
        {
            MobAegis = "PORING", EggItem = "PORING_EGG", CaptureRate = capture, IntimacyStart = 250, Fullness = 80,
        });
        svc.InvalidateEggIndexForTest();
        return (svc, pc, session, intif, mob, registry);
    }

    private static ushort Header(byte[] b) => (ushort)(b[0] | (b[1] << 8));

    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private sealed class FixedRandom(int value) : Random { public override int Next(int maxValue) => value; }

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

    private sealed class FakeMobDb : IMobDb
    {
        private readonly MobDbEntry _poring = new() { Id = PoringClass, AegisName = "PORING", Name = "Poring", Level = 1 };
        public int Count => 1;
        public MobDbEntry? Get(int classId) => classId == PoringClass ? _poring : null;
        public MobDbEntry? GetByAegisName(string aegisName) => string.Equals(aegisName, "PORING", StringComparison.OrdinalIgnoreCase) ? _poring : null;
        public IEnumerable<MobDbEntry> All() => new[] { _poring };
        public void Reload() { }
    }

    private sealed class FakeItems : Map.Server.Items.IItemCatalog
    {
        public int Count => 1;
        public ItemEntity? Get(uint itemId) => itemId == EggItemId ? Egg() : null;
        public ItemEntity? GetByAegisName(string aegisName) => string.Equals(aegisName, "PORING_EGG", StringComparison.OrdinalIgnoreCase) ? Egg() : null;
        public IEnumerable<ItemEntity> All() => new[] { Egg() };
        public void Reload() { }
        private static ItemEntity Egg() => new() { Id = EggItemId };
    }

    private sealed class RecordingIntif : NoOpIntifService
    {
        public int PetCreateCalls;
        public int LastClass;
        public override System.Threading.Tasks.Task<int> PetCreateAsync(PlayerEntity master, int classId, int eggItemId, byte intimate, byte hungry, string petName, System.Threading.CancellationToken ct = default)
        { PetCreateCalls++; LastClass = classId; return System.Threading.Tasks.Task.FromResult(100); }
    }

    private sealed class NoOpVisibility : IVisibilityService
    {
        public void SendToSelf(PlayerEntity player, Core.Server.Packets.OutgoingPacket packet) { }
        public void SendToArea(Entity src, Core.Server.Packets.OutgoingPacket packet, SendTarget target = SendTarget.Area) { }
        public void NotifySpawnedToArea(Entity entered) { }
        public void NotifyVanishedToArea(Entity gone, Core.Server.Packets.Out.ZC.VanishReason reason) { }
        public void NotifyMoveToArea(Entity walker, short fromX, short fromY, short toX, short toY, uint startTime) { }
        public void SendCurrentViewToSelf(PlayerEntity self) { }
        public void NotifyMoveDiff(Entity walker, short fromX, short fromY, short toX, short toY) { }
        public IReadOnlyList<Entity> NewlyVisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> NewlyInvisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
    }
}
