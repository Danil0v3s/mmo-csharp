using Core.Database.Entities;
using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mercenary;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MercenaryEntity = Map.Server.Entities.MercenaryEntity;

namespace Map.Server.Tests.Mercenary;

/// <summary>
/// FEATURE-09 — the mercenary now exists as a spawned, AOI-visible entity (lifecycle slice) +
/// SerializeSnapshot projects a real payload.
/// </summary>
public class MercenarySpawnTests
{
    private const int MercClass = 6017;

    private static (MercenaryService svc, PlayerEntity pc, EntityRegistry entities, FakeVisibility vis) Build()
    {
        const string mapName = "merc_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var entities = new EntityRegistry(new StubWorld(map));
        var vis = new FakeVisibility();
        var svc = new MercenaryService(NullLogger<MercenaryService>.Instance, entities, vis, new EntityIdAllocator());
        svc.SeedCatalogForTest(new MercenaryDbEntity { MercId = (uint)MercClass, Hp = 5000, Sp = 200 });
        var pc = new PlayerEntity(1, 7, "Master", Guid.NewGuid(), (uint)mapName.GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        entities.Add(pc);
        return (svc, pc, entities, vis);
    }

    private static int MercCount(EntityRegistry e, PlayerEntity pc)
        => e.All().Count(x => x is MercenaryEntity m && m.MasterId == pc.Id);

    [Fact]
    public void Create_spawns_a_visible_merc_entity()
    {
        var (svc, pc, entities, vis) = Build();

        Assert.True(svc.Create(pc, MercClass, lifetimeMs: 600_000));
        Assert.Equal(1, MercCount(entities, pc));
        Assert.Equal(1, vis.SpawnCalls);
        var merc = entities.All().OfType<MercenaryEntity>().Single();
        Assert.Equal(5000, merc.MaxHp); // from the catalog
    }

    [Fact]
    public void Create_unknown_class_returns_false_no_spawn()
    {
        var (svc, pc, entities, _) = Build();
        Assert.False(svc.Create(pc, classId: 9999, lifetimeMs: 600_000));
        Assert.Equal(0, MercCount(entities, pc));
    }

    [Fact]
    public void Delete_removes_entity_and_record()
    {
        var (svc, pc, entities, vis) = Build();
        svc.Create(pc, MercClass, 600_000);

        Assert.Equal(MercClass, svc.Delete(pc, reason: 0));
        Assert.Equal(0, MercCount(entities, pc));
        Assert.Equal(1, vis.VanishCalls);
        Assert.Equal(0, svc.Delete(pc, 0)); // record gone
    }

    [Fact]
    public void ContractStop_despawns_the_merc()
    {
        var (svc, pc, entities, vis) = Build();
        svc.Create(pc, MercClass, 600_000);

        svc.ContractStop(pc);
        Assert.Equal(0, MercCount(entities, pc));
        Assert.True(vis.VanishCalls >= 1);
    }

    [Fact]
    public void SerializeSnapshot_projects_the_live_merc()
    {
        var (svc, pc, _, _) = Build();
        svc.Create(pc, MercClass, 600_000);
        svc.SetMercIdForTest(pc, 99);

        var snap = svc.SerializeSnapshot(99);
        Assert.NotNull(snap);
        Assert.Equal(99, snap!.MercenaryId);
        Assert.Equal(MercClass, snap.ClassId);
        Assert.Equal(pc.CharacterId, snap.CharacterId);
        Assert.True(snap.LifeTime > 0);

        Assert.Null(svc.SerializeSnapshot(12345)); // no such live merc
    }

    // --- fakes ---

    private sealed class FakeVisibility : IVisibilityService
    {
        public int SpawnCalls, VanishCalls;
        public void NotifySpawnedToArea(Entity entered) => SpawnCalls++;
        public void NotifyVanishedToArea(Entity gone, VanishReason reason) => VanishCalls++;
        public void SendToSelf(PlayerEntity player, OutgoingPacket packet) { }
        public void SendToArea(Entity src, OutgoingPacket packet, SendTarget target = SendTarget.Area) { }
        public void NotifyMoveToArea(Entity walker, short fromX, short fromY, short toX, short toY, uint startTime) { }
        public void SendCurrentViewToSelf(PlayerEntity self) { }
        public void NotifyMoveDiff(Entity walker, short fromX, short fromY, short toX, short toY) { }
        public IReadOnlyList<Entity> NewlyVisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> NewlyInvisible(uint mapId, short fromX, short fromY, short toX, short toY, EntityType mask) => Array.Empty<Entity>();
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
