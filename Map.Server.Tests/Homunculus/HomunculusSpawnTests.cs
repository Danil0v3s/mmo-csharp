using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Homunculus;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Homunculus;

/// <summary>
/// FEATURE-08 — the homunculus now exists as a spawned, AOI-visible entity (lifecycle slice).
/// </summary>
public class HomunculusSpawnTests
{
    private const int LifClass = 6001;

    private static (HomunculusService svc, PlayerEntity pc, EntityRegistry entities, FakeVisibility vis) Build()
    {
        const string mapName = "homun_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var entities = new EntityRegistry(new StubWorld(map));
        var vis = new FakeVisibility();
        var svc = new HomunculusService(NullLogger<HomunculusService>.Instance, entities, vis, new EntityIdAllocator());
        var pc = new PlayerEntity(1, 7, "Master", Guid.NewGuid(), (uint)mapName.GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        entities.Add(pc);
        return (svc, pc, entities, vis);
    }

    private static int HomunCount(EntityRegistry e, PlayerEntity pc)
        => e.All().Count(x => x is HomunculusEntity h && h.MasterId == pc.Id);

    [Fact]
    public void RecvData_spawns_a_visible_homun_entity()
    {
        var (svc, pc, entities, vis) = Build();
        svc.CreateRequest(pc, LifClass);

        Assert.Equal(1, svc.RecvData(pc));
        Assert.Equal(1, HomunCount(entities, pc));   // in the registry
        Assert.Equal(1, vis.SpawnCalls);             // AOI notified
    }

    [Fact]
    public void Vaporize_removes_from_view_but_keeps_record_then_Call_respawns()
    {
        var (svc, pc, entities, vis) = Build();
        svc.CreateRequest(pc, LifClass);
        svc.RecvData(pc);

        Assert.Equal(1, svc.Vaporize(pc, 0));
        Assert.Equal(0, HomunCount(entities, pc));   // out of view
        Assert.Equal(1, vis.VanishCalls);

        Assert.True(svc.Call(pc));                   // re-summon
        Assert.Equal(1, HomunCount(entities, pc));
        Assert.Equal(2, vis.SpawnCalls);
    }

    [Fact]
    public void Dead_removes_entity_but_keeps_record()
    {
        var (svc, pc, entities, vis) = Build();
        svc.CreateRequest(pc, LifClass);
        svc.RecvData(pc);

        Assert.Equal(1, svc.Dead(pc));
        Assert.Equal(0, HomunCount(entities, pc));
        Assert.Equal(1, vis.VanishCalls);
        // Record kept → Resurrect re-spawns it.
        Assert.Equal(1, svc.Resurrect(pc, 50, pc.X, pc.Y));
        Assert.Equal(1, HomunCount(entities, pc));
    }

    [Fact]
    public void Delete_removes_entity_and_record()
    {
        var (svc, pc, entities, _) = Build();
        svc.CreateRequest(pc, LifClass);
        svc.RecvData(pc);

        Assert.Equal(1, svc.Delete(pc));
        Assert.Equal(0, HomunCount(entities, pc));
        Assert.Equal(0, svc.RecvData(pc)); // record gone
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
