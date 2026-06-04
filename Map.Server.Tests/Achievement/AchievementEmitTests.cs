using System.Collections.Concurrent;
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
/// GP-ACHIEVE — achievement client emits + title change: ZC_ALL_ACH_LIST (login snapshot),
/// ZC_ACH_UPDATE (objective tick + reward), ZC_REQ_ACH_REWARD_ACK (claim result), ZC_ACK_CHANGE_TITLE
/// (title set/clear/reject). Byte-shape parity with rAthena clif_achievement_* functions.
/// </summary>
public class AchievementEmitTests
{
    private const int Battle = (int)AchievementGroup.Battle;

    private static AchievementDbEntity Battle1(uint id, int mob = 1031, int count = 1, int score = 10,
        string rewardItem = "", int rewardTitle = 0)
        => new()
        {
            AchievementId = id, GroupName = "AG_BATTLE", Targets = $"@id={mob}:{count}", Score = score,
            RewardItem = rewardItem, RewardAmount = 1, RewardTitleId = rewardTitle,
        };

    [Fact]
    public void PcLogin_emits_update_header_then_full_list()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 2, score: 10));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // 1 of 2 — appears in the log

        // clear the objective-tick emits so we only see the login pair
        Drain(session);
        svc.PcLogin(pc);

        var update = One(session, PacketHeader.ZC_ACH_UPDATE);
        Assert.Equal(66, update.Length);

        var list = One(session, PacketHeader.ZC_ALL_ACH_LIST);
        Assert.Equal(list.Length, BitConverter.ToUInt16(list, 2)); // len field == actual
        Assert.Equal(1, BitConverter.ToInt32(list, 4));            // one achievement in the log
        // first entry at offset 22: id.L completed.B count[10].L completedTime.L rewarded.B
        Assert.Equal(70001u, BitConverter.ToUInt32(list, 22));    // achievement id
        Assert.Equal(0, list[26]);                                // not completed (1 of 2)
        Assert.Equal(1, BitConverter.ToInt32(list, 27));          // count[0] = 1
    }

    [Fact]
    public void PcLogin_empty_log_sends_header_only_no_list()
    {
        var (svc, pc, session) = Build(Battle1(70001));
        svc.PcLogin(pc); // no achievements started

        Assert.Single(Outbound(session), x => Head(x) == (ushort)PacketHeader.ZC_ACH_UPDATE);
        Assert.DoesNotContain(Outbound(session), x => Head(x) == (ushort)PacketHeader.ZC_ALL_ACH_LIST);
    }

    [Fact]
    public void Objective_tick_emits_ach_update_with_live_count()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 3, score: 10));

        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // 0 → 1

        var b = One(session, PacketHeader.ZC_ACH_UPDATE);
        Assert.Equal(66, b.Length);
        // wire layout: opcode.W score.L level.W exp.L expNext.L (16B) then id.L completed.B count[10].L ...
        Assert.Equal(70001u, BitConverter.ToUInt32(b, 16)); // achievement id at offset 16
        Assert.Equal(0, b[20]);                             // not completed
        Assert.Equal(1, BitConverter.ToInt32(b, 21));       // count[0] = 1
    }

    [Fact]
    public void Reward_claim_without_title_emits_success_ack_and_update()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 1, rewardItem: "WHITE_POTION"));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // completes
        Drain(session);

        svc.CheckReward(pc, 70001);

        var ack = One(session, PacketHeader.ZC_REQ_ACH_REWARD_ACK);
        Assert.Equal(7, ack.Length);
        Assert.Equal(1, ack[2]);                       // result = success
        Assert.Equal(70001, BitConverter.ToInt32(ack, 3));
        Assert.Contains(Outbound(session), x => Head(x) == (ushort)PacketHeader.ZC_ACH_UPDATE);
    }

    [Fact]
    public void Reward_claim_with_title_re_sends_full_list()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 1, rewardTitle: 1000));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // completes
        Drain(session);

        svc.CheckReward(pc, 70001);

        // title reward → ZC_ALL_ACH_LIST re-sent (client learns the new owned title), not a reward ack
        Assert.Contains(Outbound(session), x => Head(x) == (ushort)PacketHeader.ZC_ALL_ACH_LIST);
        Assert.DoesNotContain(Outbound(session), x => Head(x) == (ushort)PacketHeader.ZC_REQ_ACH_REWARD_ACK);
    }

    [Fact]
    public void Reward_claim_when_not_completed_emits_failure_ack()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 2, rewardItem: "WHITE_POTION"));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // 1 of 2 — not complete
        Drain(session);

        svc.CheckReward(pc, 70001);

        var ack = One(session, PacketHeader.ZC_REQ_ACH_REWARD_ACK);
        Assert.Equal(0, ack[2]); // result = failure
    }

    // --- title change ---

    [Fact]
    public void SetTitle_owned_applies_and_acks_success()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 1, rewardTitle: 1000));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031); // completes
        svc.CheckReward(pc, 70001);                     // claim → title earned
        Drain(session);

        Assert.True(svc.SetTitle(pc, 1000));
        Assert.Equal(1000, pc.TitleId);

        var ack = One(session, PacketHeader.ZC_ACK_CHANGE_TITLE);
        Assert.Equal(7, ack.Length);
        Assert.Equal(0, ack[2]);                       // result = applied
        Assert.Equal(1000, BitConverter.ToInt32(ack, 3));
    }

    [Fact]
    public void SetTitle_not_owned_rejects_with_result_1()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 1, rewardTitle: 1000));
        // never claimed → title 1000 not owned

        Assert.False(svc.SetTitle(pc, 1000));
        Assert.Equal(0, pc.TitleId); // unchanged

        var ack = One(session, PacketHeader.ZC_ACK_CHANGE_TITLE);
        Assert.Equal(1, ack[2]); // result = not owned
    }

    [Fact]
    public void SetTitle_zero_clears_and_acks_success()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 1, rewardTitle: 1000));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031);
        svc.CheckReward(pc, 70001);
        svc.SetTitle(pc, 1000); // equip
        Drain(session);

        Assert.True(svc.SetTitle(pc, 0)); // clear
        Assert.Equal(0, pc.TitleId);

        var ack = One(session, PacketHeader.ZC_ACK_CHANGE_TITLE);
        Assert.Equal(0, ack[2]); // applied
    }

    [Fact]
    public void SetTitle_same_id_is_silent_noop()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 1, rewardTitle: 1000));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031);
        svc.CheckReward(pc, 70001);
        svc.SetTitle(pc, 1000);
        Drain(session);

        Assert.False(svc.SetTitle(pc, 1000)); // same id — rAthena returns early
        Assert.Empty(Outbound(session));      // no ack at all
    }

    [Fact]
    public void SetTitle_success_broadcasts_name_refresh()
    {
        var (svc, pc, session) = Build(Battle1(70001, count: 1, rewardTitle: 1000));
        svc.UpdateObjective(pc, (byte)Battle, 0, 1031);
        svc.CheckReward(pc, 70001);

        PlayerEntity? refreshed = null;
        svc.NameRefreshHook = p => refreshed = p;
        Assert.True(svc.SetTitle(pc, 1000));
        Assert.Same(pc, refreshed); // clif_name_area re-broadcast fired
    }

    // --- helpers ---

    private static ushort Head(byte[] x) => (ushort)(x[0] | (x[1] << 8));

    private static byte[] One(MapSessionData s, PacketHeader header)
        => Outbound(s).Single(x => Head(x) == (ushort)header);

    private static void Drain(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f?.GetValue(s) is ConcurrentQueue<byte[]> q) while (q.TryDequeue(out _)) { }
    }

    private static IReadOnlyList<byte[]> Outbound(MapSessionData s)
    {
        var f = typeof(Core.Server.Network.ClientSession).GetField("_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(s) is ConcurrentQueue<byte[]> q ? q.ToArray() : Array.Empty<byte[]>();
    }

    private static (AchievementService svc, PlayerEntity pc, MapSessionData session) Build(params AchievementDbEntity[] catalog)
    {
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), 1, 50, 50) { Hp = 1000, MaxHp = 1000 };
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000, new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 1, CharacterId = 1, EntityId = pc.Id };
        var sessions = new FakeSessions(pc.Id, session);
        var svc = new AchievementService(NullLogger<AchievementService>.Instance, mobDb: null,
            sessions: sessions, items: new FakeItems(), inventory: new FakeInventory());
        svc.SeedCatalogForTest(catalog);
        svc.SeedLevelCurveForTest(
            new AchievementLevelDbEntity { Level = 0, RequiredPoints = 0 },
            new AchievementLevelDbEntity { Level = 1, RequiredPoints = 100 });
        return (svc, pc, session);
    }

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
        public Task LoadAsync(MapSessionData session, CancellationToken ct = default) => Task.CompletedTask;
        public void SendInventoryList(MapSessionData session) { }
        public bool GiveItem(MapSessionData session, uint nameId, int amount) => true;
        public bool GiveItemWithCards(MapSessionData session, uint nameId, int amount, uint card0, uint card1, uint card2, uint card3) => true;
    }

    private sealed class FakeSessions(EntityId id, MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => entityId == id ? session : null;
    }
}
