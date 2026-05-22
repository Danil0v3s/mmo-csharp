using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-H2 — exercises the four member-tracking entry points that
/// rAthena guild.cpp uses to keep the in-world roster consistent
/// with char-server state: <c>member_joined</c> (cpp:1073),
/// <c>member_added</c> (cpp:1105), <c>member_withdraw</c> (cpp:1249),
/// <c>send_memberinfoshort</c> (cpp:1363), <c>recv_memberinfoshort</c>
/// (cpp:1397).
/// </summary>
public class GuildMemberTrackingTests
{
    [Fact]
    public void MemberJoined_BindsCachedMember_AndMarksOnline()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        // Member just connected (Level 50 PC, was Offline in cache)
        var pc = MakePc(charId: 200, accountId: 1001, level: 50);
        pc.GuildId = 1;

        Assert.True(svc.MemberJoined(pc));

        var g = svc.Find(1)!;
        var idx = g.GetIndex(1001, 200);
        Assert.True(g.Members[idx].Online);
        Assert.Equal(50, g.Members[idx].Level);
    }

    [Fact]
    public void MemberJoined_GuildNotCached_ReturnsFalseAndPreservesGuildId()
    {
        // No cache miss — rAthena requests info; we return false so the
        // caller knows to defer.
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(charId: 200, accountId: 1001, level: 50);
        pc.GuildId = 99;

        Assert.False(svc.MemberJoined(pc));
        Assert.Equal(99, pc.GuildId); // not cleared — RecvInfo will rehydrate
    }

    [Fact]
    public void MemberJoined_NotOnRoster_ClearsGuildId()
    {
        // Roster drift — PC claims guild but isn't a member. Match
        // rAthena guild.cpp:1090: zero the PC's guild_id.
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        var pc = MakePc(charId: 999, accountId: 9999, level: 50);
        pc.GuildId = 1;

        Assert.False(svc.MemberJoined(pc));
        Assert.Equal(0, pc.GuildId);
    }

    [Fact]
    public void MemberAdded_FlagZero_MarksOnline()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        Assert.Equal(1, svc.MemberAdded(guildId: 1, accountId: 1001, charId: 200, flag: 0));
        var g = svc.Find(1)!;
        Assert.True(g.Members[g.GetIndex(1001, 200)].Online);
    }

    [Fact]
    public void MemberAdded_FlagOne_NoChange()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        // Snapshot online state — rosters were initialized offline.
        var before = svc.Find(1)!.Members[1].Online;

        Assert.Equal(0, svc.MemberAdded(guildId: 1, accountId: 1001, charId: 200, flag: 1));

        var after = svc.Find(1)!.Members[1].Online;
        Assert.Equal(before, after);
    }

    [Fact]
    public void MemberAdded_UnknownGuild_ReturnsZero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.MemberAdded(guildId: 99, accountId: 1, charId: 1, flag: 0));
    }

    [Fact]
    public void MemberWithdraw_RemovesSlot_AndRecomputesAverages()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001), (300, 1002) });
        var g = svc.Find(1)!;
        // Force the two non-master members online so the recount has
        // something to drop.
        g.Members[1].Online = true; g.Members[1].Level = 80;
        g.Members[2].Online = true; g.Members[2].Level = 40;
        svc.MemberAdded(1, 1001, 200, 0); // recompute side-effect
        Assert.Equal(2, g.ConnectMember);

        Assert.Equal(1, svc.MemberWithdraw(1, 1001, 200, flag: 0, name: "L", mes: "bye"));

        Assert.Equal(2, g.Members.Count);            // master + Recruit
        Assert.Equal(1, g.ConnectMember);            // only Recruit online (master offline)
        Assert.Equal(-1, g.GetIndex(1001, 200));
    }

    [Fact]
    public void MemberWithdraw_FlagOne_ExpelLogged()
    {
        // Smoke: differentiated logging path. We don't assert log
        // contents (NullLogger), only that the slot removal still
        // happens regardless of flag.
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        Assert.Equal(1, svc.MemberWithdraw(1, 1001, 200, flag: 1, name: "L", mes: "expelled"));
        Assert.Equal(-1, svc.Find(1)!.GetIndex(1001, 200));
    }

    [Fact]
    public void MemberWithdraw_UnknownMember_ReturnsZero()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        Assert.Equal(0, svc.MemberWithdraw(1, 9999, 9999, flag: 0, name: "X", mes: ""));
    }

    [Fact]
    public void SendMemberInfoShort_OfflineFlag_DropsMemberOnline()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        var g = svc.Find(1)!;
        g.Members[1].Online = true;
        var pc = MakePc(charId: 200, accountId: 1001, level: 80);
        pc.GuildId = 1;

        Assert.Equal(1, svc.SendMemberInfoShort(pc, online: false));

        Assert.False(g.Members[1].Online);
    }

    [Fact]
    public void SendMemberInfoShort_OnlineFlag_RefreshesLevelAndOnline()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        var g = svc.Find(1)!;
        g.Members[1].Online = false;
        g.Members[1].Level = 40; // stale
        var pc = MakePc(charId: 200, accountId: 1001, level: 99);
        pc.GuildId = 1;

        Assert.Equal(1, svc.SendMemberInfoShort(pc, online: true));

        Assert.True(g.Members[1].Online);
        Assert.Equal(99, g.Members[1].Level);
    }

    [Fact]
    public void RecvMemberInfoShort_MutatesMemberAndRecomputes()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001), (300, 1002) });
        var g = svc.Find(1)!;
        Assert.Equal(0, g.ConnectMember);

        Assert.Equal(1, svc.RecvMemberInfoShort(1, 1001, 200, online: true, lv: 80, classId: 4060));
        Assert.Equal(1, svc.RecvMemberInfoShort(1, 1002, 300, online: true, lv: 40, classId: 4001));

        Assert.Equal(2, g.ConnectMember);
        // Avg over master (lv 99) + two recv'd updates (80, 40) = 219 / 3 = 73.
        Assert.Equal(73, g.AverageLevel);
        Assert.True(g.Members[1].Online);
        Assert.Equal(4060, g.Members[1].ClassId);
    }

    [Fact]
    public void RecvMemberInfoShort_UnknownMember_ReturnsZero()
    {
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        Assert.Equal(0, svc.RecvMemberInfoShort(1, 9999, 9999, online: true, lv: 10, classId: 1));
        // Cache untouched
        Assert.Equal(0, svc.Find(1)!.ConnectMember);
    }

    [Fact]
    public void SendLevelUp_FansOutAsMemberInfoShort()
    {
        // SendLevelUp wraps SendMemberInfoShort; verifies the trigger
        // updates the cache same as a direct call.
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        var g = svc.Find(1)!;
        g.Members[1].Online = false;
        g.Members[1].Level = 50;
        var pc = MakePc(charId: 200, accountId: 1001, level: 60);
        pc.GuildId = 1;

        svc.SendLevelUp(pc);

        Assert.True(g.Members[1].Online);
        Assert.Equal(60, g.Members[1].Level);
    }

    [Fact]
    public void MasterCharId_BindsMasterName_OnJoin()
    {
        // Cache was hydrated without the master's name populated yet —
        // the bind on MemberJoined should fill MasterName.
        var (svc, _) = BuildGuildWith(master: 100, others: new[] { (200, 1001) });
        var g = svc.Find(1)!;
        g.MasterName = string.Empty;
        var master = MakePc(charId: 100, accountId: 1000, level: 99);
        master.GuildId = 1;
        // The master needs the master's name set so it lands on g.MasterName.
        var pcWithName = new PlayerEntity(100, 1000, "Marshal", System.Guid.NewGuid(), 1, 100, 100);
        pcWithName.GuildId = 1;
        pcWithName.Level = 99;
        Assert.True(svc.MemberJoined(pcWithName));
        Assert.Equal("Marshal", g.MasterName);
    }

    // -----------------------------------------------------------------

    private static (GuildService svc, GuildEntity g) BuildGuildWith(int master, (int charId, int accountId)[] others)
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = new GuildInfoData
        {
            GuildId = 1, Name = "T", MaxMember = 16,
            MasterCharacterId = master,
        };
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1000, CharacterId = master, Name = "M", ClassId = 4060, Level = 99, Online = false
        });
        foreach (var (cid, aid) in others)
        {
            proto.Members.Add(new GuildMemberInfo
            {
                AccountId = aid, CharacterId = cid, Name = $"P{cid}", ClassId = 1, Level = 0, Online = false
            });
        }
        var g = svc.OnRecvInfo(proto);
        return (svc, g);
    }

    private static PlayerEntity MakePc(int charId, int accountId, int level)
    {
        // Ctor is (characterId, accountId, name, sessionId, mapId, x, y).
        var pc = new PlayerEntity(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
        pc.Level = level;
        return pc;
    }
}
