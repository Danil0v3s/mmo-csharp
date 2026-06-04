using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Achievement;
using Map.Server.Entities;
using Map.Server.Handlers.Achievement;
using Map.Server.Services.Intif;
using Map.Server.Session;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Achievement;

/// <summary>
/// GP-ACHIEVE — CZ handler → service routing: CZ_REQ_ACH_REWARD → CheckReward (after a save flush),
/// CZ_REQ_CHANGE_TITLE → SetTitle. Unspawned sessions are ignored.
/// </summary>
public class AchievementHandlerTests
{
    [Fact]
    public async Task RewardHandler_flushes_save_then_calls_check_reward()
    {
        var (registry, pc, session) = Spawned();
        var ach = new FakeAchievements();
        var intif = new FakeIntif();
        var handler = new AchievementCheckRewardHandler(registry, ach, intif, NullLogger<AchievementCheckRewardHandler>.Instance);

        await handler.HandleAsync(session, Reward(70001));

        Assert.Equal(1, intif.SaveCalls);             // intif_achievement_save flush
        Assert.Equal(70001, ach.RewardClaim);         // achievement_check_reward
    }

    [Fact]
    public async Task RewardHandler_ignores_unspawned()
    {
        var (registry, pc, session) = Spawned();
        session.AuthState = MapAuthState.Authenticated;
        var ach = new FakeAchievements();
        var handler = new AchievementCheckRewardHandler(registry, ach, new FakeIntif(), NullLogger<AchievementCheckRewardHandler>.Instance);

        await handler.HandleAsync(session, Reward(70001));

        Assert.Null(ach.RewardClaim);
    }

    [Fact]
    public async Task TitleHandler_routes_title_id_to_set_title()
    {
        var (registry, pc, session) = Spawned();
        var ach = new FakeAchievements();
        var handler = new ChangeTitleHandler(registry, ach, NullLogger<ChangeTitleHandler>.Instance);

        await handler.HandleAsync(session, Title(1000));

        Assert.Equal(1000, ach.TitleSet);
    }

    [Fact]
    public async Task TitleHandler_ignores_unspawned()
    {
        var (registry, pc, session) = Spawned();
        session.AuthState = MapAuthState.Authenticated;
        var ach = new FakeAchievements();
        var handler = new ChangeTitleHandler(registry, ach, NullLogger<ChangeTitleHandler>.Instance);

        await handler.HandleAsync(session, Title(1000));

        Assert.Null(ach.TitleSet);
    }

    private static CZ_REQ_ACH_REWARD Reward(int id)
    {
        var p = new CZ_REQ_ACH_REWARD();
        typeof(CZ_REQ_ACH_REWARD).GetProperty("AchievementId")!.SetValue(p, id);
        return p;
    }

    private static CZ_REQ_CHANGE_TITLE Title(int id)
    {
        var p = new CZ_REQ_CHANGE_TITLE();
        typeof(CZ_REQ_CHANGE_TITLE).GetProperty("TitleId")!.SetValue(p, id);
        return p;
    }

    private static (EntityRegistry registry, PlayerEntity pc, MapSessionData session) Spawned()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var registry = new EntityRegistry(new StubWorld(map));
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), (uint)"test_map".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        registry.Add(pc);
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, AuthState = MapAuthState.Spawned, EntityId = pc.Id };
        return (registry, pc, session);
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

    private sealed class FakeAchievements : IAchievementService
    {
        public int? RewardClaim;
        public int? TitleSet;
        public void CheckReward(PlayerEntity pc, int achievementId) => RewardClaim = achievementId;
        public bool SetTitle(PlayerEntity pc, int titleId) { TitleSet = titleId; return true; }

        // unused surface
        public bool CheckCondition(PlayerEntity pc, int achievementId) => true;
        public bool CheckDependent(PlayerEntity pc, int achievementId) => true;
        public bool Remove(PlayerEntity pc, int achievementId) => false;
        public bool UpdateAchievement(PlayerEntity pc, int achievementId, bool completed) => false;
        public int CheckProgress(PlayerEntity pc, int achievementId) => 0;
        public int UpdateObjectiveSub(PlayerEntity pc, int achievementId, byte objective, int delta) => -1;
        public void UpdateObjective(PlayerEntity pc, byte type, byte index, int value) { }
        public void GetReward(PlayerEntity pc, int achievementId) { }
        public IReadOnlyList<int> GetTitles(PlayerEntity pc) => Array.Empty<int>();
        public void Free(PlayerEntity pc) { }
        public void ReloadDb() { }
        public int Level(PlayerEntity pc) => 0;
        public int TotalScore(PlayerEntity pc) => 0;
        public (int Level, int Exp, int ExpNext, int TotalScore) LevelInfo(PlayerEntity pc) => (0, 0, 0, 0);
        public bool MobExists(int mobId) => false;
        public void PcLogin(PlayerEntity pc) { }
        public void EmitUpdate(PlayerEntity pc, int achievementId) { }
        public IReadOnlyList<AchievementEntryData> SnapshotFor(PlayerEntity pc) => Array.Empty<AchievementEntryData>();
        public void Hydrate(PlayerEntity pc, IEnumerable<AchievementEntryData> entries) { }
    }

    private sealed class FakeIntif : Map.Server.Tests.Fakes.NoOpIntifService
    {
        public int SaveCalls;
        public override int AchievementSave(PlayerEntity pc) { SaveCalls++; return 1; }
    }
}
