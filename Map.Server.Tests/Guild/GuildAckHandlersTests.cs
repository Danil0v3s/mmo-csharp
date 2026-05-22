using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-L2 — verifies the ack handlers that fire when char-side
/// guild RPCs land back on the map: <c>guild_created</c> (cpp:722),
/// <c>guild_request_info</c> (cpp:745), <c>guild_position_changed</c>
/// (cpp:1524), <c>guild_memberposition_changed</c> (cpp:1497),
/// <c>guild_broken</c> (cpp:2149), <c>guild_gm_change</c> /
/// <c>guild_gm_changed</c> (cpp:2193 / :2229).
/// </summary>
public class GuildAckHandlersTests
{
    // ---- OnGuildCreated ----

    [Fact]
    public void OnGuildCreated_Success_SetsGuildIdOnMaster()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var master = MakePc(100, 1000);
        Assert.Equal(1, svc.OnGuildCreated(master, guildId: 7));
        Assert.Equal(7, master.GuildId);
    }

    [Fact]
    public void OnGuildCreated_Failure_PreservesZeroGuildId()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var master = MakePc(100, 1000);
        Assert.Equal(0, svc.OnGuildCreated(master, guildId: 0));
        Assert.Equal(0, master.GuildId);
    }

    [Fact]
    public void OnGuildCreated_NullPc_Zero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.OnGuildCreated(null!, 7));
    }

    // ---- RequestInfo / NpcRequestInfo ----

    [Fact]
    public void RequestInfo_RejectsBadGuildId()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.RequestInfo(0));
        Assert.Equal(1, svc.RequestInfo(7));
    }

    [Fact]
    public void NpcRequestInfo_CacheHit_ReturnsOne_NoDispatch()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.OnRecvInfo(new GuildInfoData { GuildId = 1, Name = "T", MaxMember = 16 });
        Assert.Equal(1, svc.NpcRequestInfo(1, "OnGuildLookup"));
    }

    [Fact]
    public void NpcRequestInfo_CacheMiss_DispatchesRequest()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(1, svc.NpcRequestInfo(99, "OnGuildLookup"));
    }

    // ---- OnPositionChanged ----

    [Fact]
    public void OnPositionChanged_FlipsCachedSlot()
    {
        var (svc, g, _) = Seed3Members();
        Assert.Equal(1, svc.OnPositionChanged(g.GuildId, idx: 3,
            mode: GuildPermission.Invite | GuildPermission.Expel, expMode: 50, name: "Captain"));
        Assert.Equal("Captain", g.Positions[3].Name);
        Assert.Equal(GuildPermission.Invite | GuildPermission.Expel, g.Positions[3].Mode);
        Assert.Equal(50, g.Positions[3].ExpMode);
    }

    [Fact]
    public void OnPositionChanged_Position0_AlwaysAll()
    {
        var (svc, g, _) = Seed3Members();
        Assert.Equal(1, svc.OnPositionChanged(g.GuildId, idx: 0,
            mode: GuildPermission.None, expMode: 100, name: "Master"));
        Assert.Equal(GuildPermission.All, g.Positions[0].Mode);
    }

    [Fact]
    public void OnPositionChanged_OutOfRange_Zero()
    {
        var (svc, g, _) = Seed3Members();
        Assert.Equal(0, svc.OnPositionChanged(g.GuildId, idx: 999,
            mode: GuildPermission.Invite, expMode: 0, name: "X"));
    }

    [Fact]
    public void OnPositionChanged_UnknownGuild_Zero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.OnPositionChanged(99, 0, GuildPermission.All, 0, "M"));
    }

    // ---- OnMemberPositionChanged ----

    [Fact]
    public void OnMemberPositionChanged_FlipsMemberPosition()
    {
        var (svc, g, _) = Seed3Members();
        Assert.Equal(1, svc.OnMemberPositionChanged(g.GuildId, idx: 1, newPosition: 3));
        Assert.Equal(3, g.Members[1].Position);
    }

    [Fact]
    public void OnMemberPositionChanged_BadArgs_Zero()
    {
        var (svc, g, _) = Seed3Members();
        Assert.Equal(0, svc.OnMemberPositionChanged(99, 1, 3));   // unknown guild
        Assert.Equal(0, svc.OnMemberPositionChanged(g.GuildId, -1, 3));     // bad idx
        Assert.Equal(0, svc.OnMemberPositionChanged(g.GuildId, 0, 999));    // bad position
    }

    // ---- OnBroken ----

    [Fact]
    public void OnBroken_FlagZero_DropsCache()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.OnRecvInfo(new GuildInfoData { GuildId = 1, Name = "A", MaxMember = 16 });
        Assert.Equal(1, svc.OnBroken(1, flag: 0));
        Assert.Null(svc.Find(1));
    }

    [Fact]
    public void OnBroken_NonZeroFlag_NoOp()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.OnRecvInfo(new GuildInfoData { GuildId = 1, Name = "A", MaxMember = 16 });
        Assert.Equal(0, svc.OnBroken(1, flag: 1));
        Assert.NotNull(svc.Find(1));
    }

    // ---- GmChange ----

    [Fact]
    public void GmChange_HappyPath_True()
    {
        var (svc, g, _) = Seed3Members();
        // Lieutenant (CharId=200) to become master
        Assert.True(svc.GmChange(g.GuildId, charId: 200));
    }

    [Fact]
    public void GmChange_AlreadyMaster_False()
    {
        var (svc, g, _) = Seed3Members();
        Assert.False(svc.GmChange(g.GuildId, charId: 100)); // already master
    }

    [Fact]
    public void GmChange_NonMember_False()
    {
        var (svc, g, _) = Seed3Members();
        Assert.False(svc.GmChange(g.GuildId, charId: 9999));
    }

    [Fact]
    public void GmChange_UnknownGuild_False()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.False(svc.GmChange(99, charId: 100));
    }

    // ---- OnGmChanged ----

    [Fact]
    public void OnGmChanged_SwapsSlots_AndUpdatesMaster()
    {
        var (svc, g, _) = Seed3Members();
        // Lieutenant (aid=1001, cid=200) takes over
        Assert.Equal(1, svc.OnGmChanged(g.GuildId, accountId: 1001, charId: 200, timestamp: 12345));

        // Master swap: Members[0] is now the lieutenant
        Assert.Equal(200, g.Members[0].CharId);
        Assert.Equal(0, g.Members[0].Position);
        // Old master is now at the lieutenant's previous slot (idx 1)
        Assert.Equal(100, g.Members[1].CharId);
        Assert.Equal(g.MasterCharId, 200);
        Assert.Equal(g.Members[0].Name, g.MasterName);
    }

    [Fact]
    public void OnGmChanged_TargetIsAlreadyMaster_Zero()
    {
        var (svc, g, _) = Seed3Members();
        Assert.Equal(0, svc.OnGmChanged(g.GuildId, accountId: 1000, charId: 100, timestamp: 0));
    }

    [Fact]
    public void OnGmChanged_NotOnRoster_Zero()
    {
        var (svc, g, _) = Seed3Members();
        Assert.Equal(0, svc.OnGmChanged(g.GuildId, accountId: 9999, charId: 9999, timestamp: 0));
    }

    [Fact]
    public void OnGmChanged_UnknownGuild_Zero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.OnGmChanged(99, 1, 1, 0));
    }

    // -----------------------------------------------------------------

    private static (GuildService svc, GuildEntity g, PlayerEntity master) Seed3Members()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = new GuildInfoData
        {
            GuildId = 1, Name = "T", MaxMember = 16, MasterCharacterId = 100,
        };
        proto.Members.Add(new GuildMemberInfo { AccountId = 1000, CharacterId = 100, Name = "M", Level = 99, Online = true });
        proto.Members.Add(new GuildMemberInfo { AccountId = 1001, CharacterId = 200, Name = "L", Level = 80, Online = true });
        proto.Members.Add(new GuildMemberInfo { AccountId = 1002, CharacterId = 300, Name = "R", Level = 40, Online = true });
        var g = svc.OnRecvInfo(proto);
        // Hydrate defaults non-master to last position; fix for our tests.
        g.Members[0].Position = 0;
        g.Members[1].Position = 1;
        g.Members[2].Position = 2;
        var master = MakePc(100, 1000);
        master.GuildId = 1;
        return (svc, g, master);
    }

    private static PlayerEntity MakePc(int charId, int accountId)
        => new(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
