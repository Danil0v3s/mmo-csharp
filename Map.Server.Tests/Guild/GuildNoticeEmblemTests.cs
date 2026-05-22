using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-M1 — Notice + Emblem update flow. Mirrors rAthena
/// <c>guild_change_notice</c> (cpp:1542), <c>guild_notice_changed</c>
/// (cpp:1553), <c>guild_check_emblem_change_condition</c> (cpp:1573),
/// <c>guild_change_emblem</c> (cpp:1587),
/// <c>guild_change_emblem_version</c> (cpp:1598),
/// <c>guild_emblem_changed</c> (cpp:1609).
/// </summary>
public class GuildNoticeEmblemTests
{
    // ---- Notice ----

    [Fact]
    public void ChangeNotice_GuildMatch_Succeeds()
    {
        var (svc, _, master) = Seed();
        Assert.True(svc.ChangeNotice(master, master.GuildId, "hi", "everyone"));
    }

    [Fact]
    public void ChangeNotice_GuildMismatch_Fails()
    {
        var (svc, _, master) = Seed();
        Assert.False(svc.ChangeNotice(master, master.GuildId + 1, "hi", "everyone"));
    }

    [Fact]
    public void ChangeNotice_NullPc_Fails()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.False(svc.ChangeNotice(null!, 1, "hi", "every"));
    }

    [Fact]
    public void NoticeChanged_MutatesCachedNotice()
    {
        var (svc, g, _) = Seed();
        Assert.Equal(1, svc.NoticeChanged(g.GuildId, "headline", "body"));
        Assert.Equal("headline", g.Notice1);
        Assert.Equal("body", g.Notice2);
    }

    [Fact]
    public void NoticeChanged_UnknownGuild_ReturnsZero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.NoticeChanged(99, "x", "y"));
    }

    [Fact]
    public void NoticeChanged_TruncatesToCaps()
    {
        var (svc, g, _) = Seed();
        // Build strings longer than MAX_GUILDMES1 (60) and
        // MAX_GUILDMES2 (120).
        var long1 = new string('a', 80);
        var long2 = new string('b', 200);
        svc.NoticeChanged(g.GuildId, long1, long2);
        Assert.Equal(60, g.Notice1.Length);
        Assert.Equal(120, g.Notice2.Length);
    }

    [Fact]
    public void NoticeChanged_NullInputs_ClearedToEmpty()
    {
        var (svc, g, _) = Seed();
        g.Notice1 = "x"; g.Notice2 = "y";
        svc.NoticeChanged(g.GuildId, null!, null!);
        Assert.Equal(string.Empty, g.Notice1);
        Assert.Equal(string.Empty, g.Notice2);
    }

    // ---- Emblem ----

    [Fact]
    public void CheckEmblemChangeCondition_InGuild_True()
    {
        var (svc, _, master) = Seed();
        Assert.True(svc.CheckEmblemChangeCondition(master));
    }

    [Fact]
    public void CheckEmblemChangeCondition_NoGuild_False()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(charId: 100, accountId: 1000);
        Assert.False(svc.CheckEmblemChangeCondition(pc));
    }

    [Fact]
    public void CheckEmblemChangeCondition_NullPc_False()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.False(svc.CheckEmblemChangeCondition(null!));
    }

    [Fact]
    public void ChangeEmblem_GateOk_ReturnsOne()
    {
        var (svc, _, master) = Seed();
        Assert.Equal(1, svc.ChangeEmblem(master, new byte[] { 0x42, 0x4D, 0x10 }));
    }

    [Fact]
    public void ChangeEmblem_NoGuild_ReturnsZero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(charId: 100, accountId: 1000);
        Assert.Equal(0, svc.ChangeEmblem(pc, new byte[] { 0x42, 0x4D, 0x10 }));
    }

    [Fact]
    public void ChangeEmblemVersion_BumpsLocalVersion()
    {
        var (svc, g, master) = Seed();
        g.EmblemVersion = 1;
        Assert.Equal(1, svc.ChangeEmblemVersion(master, 5));
        Assert.Equal(5, g.EmblemVersion);
    }

    [Fact]
    public void ChangeEmblemVersion_GateBlocks_NoMutation()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(charId: 100, accountId: 1000);
        Assert.Equal(0, svc.ChangeEmblemVersion(pc, 5));
    }

    [Fact]
    public void EmblemChanged_BumpsCachedVersion()
    {
        var (svc, g, _) = Seed();
        g.EmblemVersion = 4;
        Assert.Equal(1, svc.EmblemChanged(g.GuildId));
        Assert.Equal(5, g.EmblemVersion);
    }

    [Fact]
    public void EmblemChanged_UnknownGuild_Zero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.EmblemChanged(99));
    }

    // -----------------------------------------------------------------

    private static (GuildService svc, GuildEntity g, PlayerEntity master) Seed()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = new GuildInfoData
        {
            GuildId = 1, Name = "T", MaxMember = 16, MasterCharacterId = 100,
        };
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1000, CharacterId = 100, Name = "M",
            ClassId = 1, Level = 99, Online = true
        });
        proto.Positions.Add(new GuildPositionInfo { Index = 0, Name = "Master", Mode = (int)GuildPermission.All });
        var g = svc.OnRecvInfo(proto);
        var master = MakePc(charId: 100, accountId: 1000);
        master.GuildId = 1;
        return (svc, g, master);
    }

    private static PlayerEntity MakePc(int charId, int accountId)
        => new(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
