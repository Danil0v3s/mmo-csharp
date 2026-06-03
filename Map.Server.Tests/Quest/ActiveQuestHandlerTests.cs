using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Handlers.Quest;
using Map.Server.Quest;
using Map.Server.Session;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Quest;

/// <summary>GP-QUEST — CZ_ACTIVE_QUEST toggle handler → QuestService.UpdateStatus.</summary>
public class ActiveQuestHandlerTests
{
    [Fact]
    public async Task Active_flag_maps_to_Q_ACTIVE_and_calls_update_status()
    {
        var (handler, pc, session, quests) = Build();
        await handler.HandleAsync(session, Packet(questId: 4242, active: 1));

        Assert.Equal((4242, (byte)1), quests.LastCall); // Q_ACTIVE = 1
    }

    [Fact]
    public async Task Inactive_flag_maps_to_Q_INACTIVE()
    {
        var (handler, pc, session, quests) = Build();
        await handler.HandleAsync(session, Packet(questId: 7, active: 0));

        Assert.Equal((7, (byte)0), quests.LastCall); // Q_INACTIVE = 0
    }

    [Fact]
    public async Task Unspawned_session_is_ignored()
    {
        var (handler, pc, session, quests) = Build();
        session.AuthState = MapAuthState.Authenticated; // not Spawned
        await handler.HandleAsync(session, Packet(questId: 7, active: 1));

        Assert.Null(quests.LastCall);
    }

    private static CZ_ACTIVE_QUEST Packet(int questId, byte active)
    {
        var p = new CZ_ACTIVE_QUEST();
        typeof(CZ_ACTIVE_QUEST).GetProperty("QuestId")!.SetValue(p, questId);
        typeof(CZ_ACTIVE_QUEST).GetProperty("Active")!.SetValue(p, active);
        return p;
    }

    private static (ActiveQuestHandler handler, PlayerEntity pc, MapSessionData session, FakeQuests quests) Build()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        var quests = new FakeQuests();
        var handler = new ActiveQuestHandler(registry, quests, NullLogger<ActiveQuestHandler>.Instance);
        return (handler, pc, session, quests);
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

    private sealed class FakeQuests : IQuestService
    {
        public (int questId, byte status)? LastCall;
        public int UpdateStatus(PlayerEntity pc, int questId, byte status) { LastCall = (questId, status); return 0; }

        // unused surface
        public int Add(PlayerEntity pc, int questId) => 0;
        public int Change(PlayerEntity pc, int oldQuestId, int newQuestId) => 0;
        public int Check(PlayerEntity pc, int questId, byte status) => -1;
        public int Delete(PlayerEntity pc, int questId) => 0;
        public int PcLogin(PlayerEntity pc) => 0;
        public int UpdateObjectiveSub(PlayerEntity pc, int questId, byte index, int delta) => 0;
        public void UpdateObjective(PlayerEntity pc, int questId, byte index, int delta) { }
        public void UpdateMobObjective(PlayerEntity pc, string mobAegis) { }
        public Core.Database.Entities.QuestDbEntity? GetCatalogEntry(uint questId) => null;
        public void Reload() { }
        public IReadOnlyList<Core.Server.IPC.QuestEntryData> SnapshotFor(PlayerEntity pc) => Array.Empty<Core.Server.IPC.QuestEntryData>();
        public void Hydrate(PlayerEntity pc, IEnumerable<Core.Server.IPC.QuestEntryData> entries) { }
    }
}
