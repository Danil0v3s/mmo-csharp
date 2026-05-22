using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-M2 — Alliance / opposition flow. Mirrors rAthena
/// <c>guild_reqalliance</c> (cpp:1853),
/// <c>guild_reply_reqalliance</c> (cpp:1915),
/// <c>guild_delalliance</c> (cpp:1974), <c>guild_opposition</c>
/// (cpp:1989), <c>guild_allianceack</c> (cpp:2030).
/// </summary>
public class GuildAllianceTests
{
    // ----- ReqAlliance -----

    [Fact]
    public void ReqAlliance_HappyPath_True()
    {
        var (svc, _, _, alpha, beta) = TwoGuilds();
        Assert.True(svc.ReqAlliance(alpha, beta));
    }

    [Fact]
    public void ReqAlliance_AgitActive_False()
    {
        var (svc, _, _, alpha, beta) = TwoGuilds();
        svc.MaxAlliancePerSide = 3;
        ((GuildService)svc).IsAgitActive = true;
        Assert.False(svc.ReqAlliance(alpha, beta));
    }

    [Fact]
    public void ReqAlliance_SameGuild_False()
    {
        var (svc, g, _, alpha, _) = TwoGuilds();
        var sibling = MakePc(charId: 105, accountId: 1005);
        sibling.GuildId = alpha.GuildId;
        // Sibling isn't on roster — but the same-guild gate fires before
        // the roster check.
        Assert.False(svc.ReqAlliance(alpha, sibling));
    }

    [Fact]
    public void ReqAlliance_AlreadyAllied_False()
    {
        var (svc, g1, g2, alpha, beta) = TwoGuilds();
        g1.Alliances.Add(new GuildAlliance { GuildId = g2.GuildId, IsOpposition = false });
        Assert.False(svc.ReqAlliance(alpha, beta));
    }

    [Fact]
    public void ReqAlliance_AtMaxCap_False()
    {
        var (svc, g1, _, alpha, beta) = TwoGuilds();
        svc.MaxAlliancePerSide = 1;
        g1.Alliances.Add(new GuildAlliance { GuildId = 99, IsOpposition = false });
        Assert.False(svc.ReqAlliance(alpha, beta));
    }

    [Fact]
    public void ReqAlliance_TargetNoGuild_False()
    {
        var (svc, _, _, alpha, _) = TwoGuilds();
        var lone = MakePc(charId: 999, accountId: 9999);
        // GuildId 0
        Assert.False(svc.ReqAlliance(alpha, lone));
    }

    // ----- ReplyReqAlliance -----

    [Fact]
    public void ReplyReqAlliance_AcceptOrDeny_DispatchOk()
    {
        var (svc, _, _, _, beta) = TwoGuilds();
        Assert.Equal(1, svc.ReplyReqAlliance(beta, requesterAccountId: 1000, flag: 1));
        Assert.Equal(1, svc.ReplyReqAlliance(beta, requesterAccountId: 1000, flag: 0));
    }

    [Fact]
    public void ReplyReqAlliance_BadArgs_Zero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.ReplyReqAlliance(null!, 1, 1));
        Assert.Equal(0, svc.ReplyReqAlliance(MakePc(1, 1), 0, 1));
    }

    // ----- DelAlliance -----

    [Fact]
    public void DelAlliance_RelationExists_One()
    {
        var (svc, g1, g2, alpha, _) = TwoGuilds();
        g1.Alliances.Add(new GuildAlliance { GuildId = g2.GuildId, IsOpposition = false });
        Assert.Equal(1, svc.DelAlliance(alpha, g2.GuildId, flag: 0));
    }

    [Fact]
    public void DelAlliance_RelationMissing_Zero()
    {
        var (svc, _, g2, alpha, _) = TwoGuilds();
        Assert.Equal(0, svc.DelAlliance(alpha, g2.GuildId, flag: 0));
    }

    [Fact]
    public void DelAlliance_AgitActive_Zero()
    {
        var (svc, g1, g2, alpha, _) = TwoGuilds();
        g1.Alliances.Add(new GuildAlliance { GuildId = g2.GuildId, IsOpposition = false });
        svc.IsAgitActive = true;
        Assert.Equal(0, svc.DelAlliance(alpha, g2.GuildId, flag: 0));
    }

    [Fact]
    public void DelAlliance_FlagDisambiguatesOppositionVsAlliance()
    {
        var (svc, g1, g2, alpha, _) = TwoGuilds();
        // Only an enemy relation exists; del with flag=0 (ally) is a no-op
        g1.Alliances.Add(new GuildAlliance { GuildId = g2.GuildId, IsOpposition = true });
        Assert.Equal(0, svc.DelAlliance(alpha, g2.GuildId, flag: 0));
        Assert.Equal(1, svc.DelAlliance(alpha, g2.GuildId, flag: 1));
    }

    // ----- Opposition -----

    [Fact]
    public void Opposition_HappyPath_One()
    {
        var (svc, _, _, alpha, beta) = TwoGuilds();
        Assert.Equal(1, svc.Opposition(alpha, beta));
    }

    [Fact]
    public void Opposition_SameGuild_Zero()
    {
        var (svc, _, _, alpha, _) = TwoGuilds();
        var sibling = MakePc(charId: 999, accountId: 9999);
        sibling.GuildId = alpha.GuildId;
        Assert.Equal(0, svc.Opposition(alpha, sibling));
    }

    [Fact]
    public void Opposition_AlreadyEnemy_Zero()
    {
        var (svc, g1, g2, alpha, beta) = TwoGuilds();
        g1.Alliances.Add(new GuildAlliance { GuildId = g2.GuildId, IsOpposition = true });
        Assert.Equal(0, svc.Opposition(alpha, beta));
    }

    [Fact]
    public void Opposition_AtMaxCap_Zero()
    {
        var (svc, g1, _, alpha, beta) = TwoGuilds();
        svc.MaxAlliancePerSide = 1;
        g1.Alliances.Add(new GuildAlliance { GuildId = 99, IsOpposition = true });
        Assert.Equal(0, svc.Opposition(alpha, beta));
    }

    // ----- OnAllianceAck (inbound) -----

    [Fact]
    public void OnAllianceAck_CreateAlliance_AddsToBothSides()
    {
        var (svc, g1, g2, _, _) = TwoGuilds();
        Assert.Equal(1, svc.OnAllianceAck(g1.GuildId, g2.GuildId, "A", "B", flag: 0));
        Assert.True(g1.IsAllied(g2.GuildId));
        Assert.True(g2.IsAllied(g1.GuildId));
    }

    [Fact]
    public void OnAllianceAck_CreateOpposition_AddsRequesterSideOnly()
    {
        // flag 0x01 = opposition; rAthena applies only to the
        // requester side (`2 - (flag & 1)` iterations).
        var (svc, g1, g2, _, _) = TwoGuilds();
        Assert.Equal(1, svc.OnAllianceAck(g1.GuildId, g2.GuildId, "A", "B", flag: 0x01));
        Assert.True(g1.IsOpposition(g2.GuildId));
        Assert.False(g2.IsOpposition(g1.GuildId));
    }

    [Fact]
    public void OnAllianceAck_FailureBits_NoMutation()
    {
        var (svc, g1, g2, _, _) = TwoGuilds();
        Assert.Equal(0, svc.OnAllianceAck(g1.GuildId, g2.GuildId, "A", "B", flag: 0x10));
        Assert.False(g1.IsAllied(g2.GuildId));
        Assert.False(g2.IsAllied(g1.GuildId));
    }

    [Fact]
    public void OnAllianceAck_Remove_DropsRelation()
    {
        var (svc, g1, g2, _, _) = TwoGuilds();
        g1.Alliances.Add(new GuildAlliance { GuildId = g2.GuildId, IsOpposition = false });
        g2.Alliances.Add(new GuildAlliance { GuildId = g1.GuildId, IsOpposition = false });

        // 0x08 = remove; 0x00 lower nibble = alliance
        Assert.Equal(1, svc.OnAllianceAck(g1.GuildId, g2.GuildId, "A", "B", flag: 0x08));
        Assert.False(g1.IsAllied(g2.GuildId));
        Assert.False(g2.IsAllied(g1.GuildId));
    }

    [Fact]
    public void OnAllianceAck_AtMaxCap_DoesNotAdd()
    {
        var (svc, g1, g2, _, _) = TwoGuilds();
        // Fill g1 to MaxAlliance
        for (int i = 0; i < GuildLimits.MaxAlliance; i++)
            g1.Alliances.Add(new GuildAlliance { GuildId = 1000 + i, IsOpposition = false });
        // Try to add g2 as ally — should land on g2 only.
        var result = svc.OnAllianceAck(g1.GuildId, g2.GuildId, "A", "B", flag: 0);
        Assert.Equal(1, result); // g2 side mutation succeeds
        Assert.False(g1.IsAllied(g2.GuildId));
        Assert.True(g2.IsAllied(g1.GuildId));
    }

    [Fact]
    public void OnAllianceAck_NoDuplicate()
    {
        var (svc, g1, g2, _, _) = TwoGuilds();
        g1.Alliances.Add(new GuildAlliance { GuildId = g2.GuildId, IsOpposition = false });
        // No mutation on the dup; opposite side still adds.
        var before = g1.Alliances.Count;
        svc.OnAllianceAck(g1.GuildId, g2.GuildId, "A", "B", flag: 0);
        Assert.Equal(before, g1.Alliances.Count);
    }

    // -----------------------------------------------------------------

    private static (GuildService svc, GuildEntity g1, GuildEntity g2, PlayerEntity alpha, PlayerEntity beta) TwoGuilds()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var p1 = new GuildInfoData { GuildId = 1, Name = "A", MaxMember = 16, MasterCharacterId = 100 };
        p1.Members.Add(new GuildMemberInfo { AccountId = 1000, CharacterId = 100, Name = "AM", Level = 99, Online = true });
        var p2 = new GuildInfoData { GuildId = 2, Name = "B", MaxMember = 16, MasterCharacterId = 200 };
        p2.Members.Add(new GuildMemberInfo { AccountId = 2000, CharacterId = 200, Name = "BM", Level = 99, Online = true });
        var g1 = svc.OnRecvInfo(p1);
        var g2 = svc.OnRecvInfo(p2);
        var alpha = MakePc(100, 1000); alpha.GuildId = 1;
        var beta = MakePc(200, 2000); beta.GuildId = 2;
        return (svc, g1, g2, alpha, beta);
    }

    private static PlayerEntity MakePc(int charId, int accountId)
        => new(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
