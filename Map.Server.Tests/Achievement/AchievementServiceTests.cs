using Core.Database.Entities;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Achievement;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Achievement;

/// <summary>
/// FEATURE-04 — achievement service (objective / dependent / progress / complete / reward / titles /
/// level). The AG_BATTLE objective + MobExists were landed by FEATURE-01.
/// </summary>
public class AchievementServiceTests
{
    private const int Battle = (int)AchievementGroup.Battle;

    private static AchievementService Svc(params AchievementDbEntity[] catalog)
    {
        var s = new AchievementService(NullLogger<AchievementService>.Instance);
        s.SeedCatalogForTest(catalog);
        return s;
    }

    private static PlayerEntity Pc() =>
        new(1, 1, "P1", Guid.NewGuid(), 1, 50, 50) { Hp = 1000, MaxHp = 1000 };

    private static AchievementDbEntity Battle1(uint id, int mob = 1031, int count = 1, int score = 10,
        string deps = "", string rewardItem = "", int rewardTitle = 0)
        => new()
        {
            AchievementId = id, GroupName = "AG_BATTLE", Targets = $"@id={mob}:{count}", Score = score,
            Dependents = deps, RewardItem = rewardItem, RewardAmount = 1, RewardTitleId = rewardTitle,
        };

    // --- objective + completion + dependents ---

    [Fact]
    public void Battle_objective_advances_and_completes_on_target()
    {
        var svc = Svc(Battle1(70001, mob: 1031, count: 2));
        var pc = Pc();

        svc.UpdateObjective(pc, (byte)Battle, 0, 1031);
        Assert.Equal(1, pc.AchievementLog.Single().Counts[0]);
        Assert.Equal(0, pc.AchievementLog.Single().CompletedUnix); // not done (1 of 2)

        svc.UpdateObjective(pc, (byte)Battle, 0, 1031);
        Assert.True(pc.AchievementLog.Single().CompletedUnix > 0); // completed
    }

    [Fact]
    public void Completion_is_gated_on_dependents()
    {
        // 70002 depends on 70001; killing the 70002 target should NOT complete it until 70001 is done.
        var svc = Svc(Battle1(70001, mob: 1031, count: 1), Battle1(70002, mob: 1100, count: 1, deps: "70001"));
        var pc = Pc();

        svc.UpdateObjective(pc, (byte)Battle, 0, 1100); // advances 70002 but dep 70001 not done
        var e2 = pc.AchievementLog.Single(a => a.AchievementId == 70002);
        Assert.Equal(1, e2.Counts[0]);
        Assert.Equal(0, e2.CompletedUnix); // blocked by dependent

        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // complete 70001
        Assert.True(svc.CheckDependent(pc, 70002)); // dep now satisfied
    }

    [Fact]
    public void CheckDependent_reflects_prereq_completion()
    {
        var svc = Svc(Battle1(70001, count: 1), Battle1(70002, deps: "70001"));
        var pc = Pc();
        Assert.False(svc.CheckDependent(pc, 70002)); // 70001 not completed
        svc.UpdateAchievement(pc, 70001, completed: true);
        Assert.True(svc.CheckDependent(pc, 70002));
    }

    // --- progress / remove / free / objective-sub ---

    [Fact]
    public void CheckProgress_sums_counts()
    {
        var svc = Svc(Battle1(70001, count: 5));
        var pc = Pc();
        svc.UpdateObjectiveSub(pc, 70001, 0, 3);
        Assert.Equal(3, svc.CheckProgress(pc, 70001));
        svc.UpdateObjectiveSub(pc, 70001, 0, 99); // clamps at target 5
        Assert.Equal(5, svc.CheckProgress(pc, 70001));
    }

    [Fact]
    public void Remove_and_Free_clear_entries()
    {
        var svc = Svc(Battle1(70001), Battle1(70002));
        var pc = Pc();
        svc.UpdateAchievement(pc, 70001, true);
        svc.UpdateAchievement(pc, 70002, true);
        Assert.True(svc.Remove(pc, 70001));
        Assert.False(svc.Remove(pc, 70001)); // already gone
        Assert.Single(pc.AchievementLog);
        svc.Free(pc);
        Assert.Empty(pc.AchievementLog);
    }

    // --- reward + titles ---

    [Fact]
    public void Completion_does_not_auto_grant_reward_until_claimed()
    {
        // rAthena parity: achievement_update_achievement flags completion + sends clif_achievement_update
        // but does NOT grant the reward — that is the manual claim (CZ_REQ_ACH_REWARD → check_reward).
        var items = new FakeItems();
        var inv = new FakeInventory();
        var sessions = new FakeSessions();
        var svc = new AchievementService(NullLogger<AchievementService>.Instance, mobDb: null,
            sessions: sessions, items: items, inventory: inv);
        svc.SeedCatalogForTest(Battle1(70001, count: 1, rewardItem: "WHITE_POTION"));
        var pc = Pc();
        sessions.Register(pc);

        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // completes, but no auto-reward

        Assert.True(pc.AchievementLog.Single().CompletedUnix > 0);
        Assert.Equal(0, pc.AchievementLog.Single().RewardedUnix);   // not yet rewarded
        Assert.Equal(0, inv.GiveCalls);                             // no item granted on completion
    }

    [Fact]
    public void CheckReward_grants_item_once_and_is_idempotent()
    {
        var items = new FakeItems();
        var inv = new FakeInventory();
        var sessions = new FakeSessions();
        var svc = new AchievementService(NullLogger<AchievementService>.Instance, mobDb: null,
            sessions: sessions, items: items, inventory: inv);
        svc.SeedCatalogForTest(Battle1(70001, count: 1, rewardItem: "WHITE_POTION", rewardTitle: 1000));
        var pc = Pc();
        sessions.Register(pc);

        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // completes
        svc.CheckReward(pc, 70001);                     // manual claim → grant

        Assert.Equal(1, inv.GiveCalls);
        Assert.True(pc.AchievementLog.Single().RewardedUnix > 0);

        svc.CheckReward(pc, 70001); // second claim — idempotent
        Assert.Equal(1, inv.GiveCalls);
    }

    [Fact]
    public void CheckReward_on_incomplete_achievement_does_not_grant()
    {
        var items = new FakeItems();
        var inv = new FakeInventory();
        var sessions = new FakeSessions();
        var svc = new AchievementService(NullLogger<AchievementService>.Instance, mobDb: null,
            sessions: sessions, items: items, inventory: inv);
        svc.SeedCatalogForTest(Battle1(70001, count: 2, rewardItem: "WHITE_POTION"));
        var pc = Pc();
        sessions.Register(pc);

        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // 1 of 2 — not complete
        svc.CheckReward(pc, 70001);                     // claim rejected (not completed)

        Assert.Equal(0, inv.GiveCalls);
        Assert.Equal(0, pc.AchievementLog.Single().RewardedUnix);
    }

    [Fact]
    public void GetTitles_returns_earned_titles_after_claim()
    {
        var svc = Svc(Battle1(70001, count: 1, rewardTitle: 1000), Battle1(70002, count: 1, rewardTitle: 0));
        var pc = Pc();
        // Complete + claim both (no item reward wired → grant only stamps RewardedUnix).
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // 70001 (mob 1031) completes
        svc.CheckReward(pc, 70001);                     // manual claim grants the title
        Assert.Contains(1000, svc.GetTitles(pc));
        Assert.DoesNotContain(0, svc.GetTitles(pc)); // title 0 not a real title
    }

    // --- level ---

    [Fact]
    public void Level_walks_the_score_curve()
    {
        var svc = Svc(Battle1(70001, count: 1, score: 30));
        var pc = Pc();
        svc.SeedLevelCurveForTest(
            new AchievementLevelDbEntity { Level = 0, RequiredPoints = 0 },
            new AchievementLevelDbEntity { Level = 1, RequiredPoints = 20 },
            new AchievementLevelDbEntity { Level = 2, RequiredPoints = 50 });

        Assert.Equal(0, svc.Level(pc)); // no completed achievements → score 0

        svc.UpdateAchievement(pc, 70001, true); // +30 score
        // total 30 > points[0]=0 and > points[1]=20 but not > points[2]=50 → level 2.
        Assert.Equal(2, svc.Level(pc));
    }

    // --- fakes ---

    private sealed class FakeItems : IItemCatalog
    {
        public int Count => 0;
        public ItemEntity? Get(uint itemId) => new() { Id = itemId };
        public ItemEntity? GetByAegisName(string aegisName) => new() { Id = 501, NameAegis = aegisName };
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class FakeInventory : IInventoryService
    {
        public int GiveCalls;
        public Task LoadAsync(MapSessionData session, CancellationToken ct = default) => Task.CompletedTask;
        public void SendInventoryList(MapSessionData session) { }
        public bool GiveItem(MapSessionData session, uint nameId, int amount) { GiveCalls++; return true; }
        public bool GiveItemWithCards(MapSessionData session, uint nameId, int amount, uint card0, uint card1, uint card2, uint card3) => GiveItem(session, nameId, amount);
    }

    private sealed class FakeSessions : ISessionManagerAccessor
    {
        private readonly Dictionary<int, MapSessionData> _byId = new();
        public void Register(PlayerEntity pc)
        {
            var sockets = TestSocketFactory.CreateSocketPair();
            _byId[pc.Id.Value] = new MapSessionData(sockets.ServerSide, 30000,
                new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
            { EntityId = pc.Id };
        }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byId.GetValueOrDefault(entityId.Value);
    }
}
