using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-H3 — verifies the permission gate (rAthena
/// <c>guild_has_permission</c> guild.cpp:2640) and the gameplay-side
/// guards on Invite (cpp:925), Expulsion (cpp:1189), ChangePosition
/// (cpp:1511), Break (cpp:2289), Leave (cpp:1156).
/// </summary>
public class GuildPermissionGateTests
{
    // ----- HasPermission core -----

    [Fact]
    public void HasPermission_NoGuild_AlwaysFalse()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(charId: 100, accountId: 1000);
        Assert.False(svc.HasPermission(pc, GuildPermission.Invite));
    }

    [Fact]
    public void HasPermission_GuildNotCached_False()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(charId: 100, accountId: 1000);
        pc.GuildId = 99;
        Assert.False(svc.HasPermission(pc, GuildPermission.Invite));
    }

    [Fact]
    public void HasPermission_Master_HasAllPermissions()
    {
        var (svc, g, master, _) = BuildSeed();
        // Master always at position 0 with GuildPermission.All
        Assert.True(svc.HasPermission(master, GuildPermission.Invite));
        Assert.True(svc.HasPermission(master, GuildPermission.Expel));
        Assert.True(svc.HasPermission(master, GuildPermission.Storage));
    }

    [Fact]
    public void HasPermission_LowRank_FollowsPositionMode()
    {
        var (svc, g, _, recruit) = BuildSeed();
        // Recruit lives at position 2 (set in BuildSeed) with Mode = None
        g.Members[2].Position = 2;
        g.Positions[2].Mode = GuildPermission.None;
        Assert.False(svc.HasPermission(recruit, GuildPermission.Invite));

        // Give Invite, not Expel
        g.Positions[2].Mode = GuildPermission.Invite;
        Assert.True(svc.HasPermission(recruit, GuildPermission.Invite));
        Assert.False(svc.HasPermission(recruit, GuildPermission.Expel));
    }

    [Fact]
    public void HasPermission_RosterDrift_False()
    {
        var (svc, _, _, _) = BuildSeed();
        // PC claims the guild but isn't on the roster.
        var rogue = MakePc(charId: 9999, accountId: 9999);
        rogue.GuildId = 1;
        Assert.False(svc.HasPermission(rogue, GuildPermission.Invite));
    }

    // ----- Invite -----

    [Fact]
    public void Invite_Master_Succeeds()
    {
        var (svc, _, master, _) = BuildSeed();
        var invitee = MakePc(charId: 500, accountId: 2000);
        Assert.True(svc.Invite(master, invitee));
    }

    [Fact]
    public void Invite_WithoutPermission_Fails()
    {
        var (svc, g, _, recruit) = BuildSeed();
        g.Members[2].Position = 2;
        g.Positions[2].Mode = GuildPermission.None;
        var invitee = MakePc(charId: 500, accountId: 2000);
        Assert.False(svc.Invite(recruit, invitee));
    }

    [Fact]
    public void Invite_WithInvitePerm_Succeeds()
    {
        var (svc, g, _, recruit) = BuildSeed();
        g.Members[2].Position = 2;
        g.Positions[2].Mode = GuildPermission.Invite;
        var invitee = MakePc(charId: 500, accountId: 2000);
        Assert.True(svc.Invite(recruit, invitee));
    }

    [Fact]
    public void Invite_InviteeAlreadyInGuild_Fails()
    {
        var (svc, _, master, _) = BuildSeed();
        var invitee = MakePc(charId: 500, accountId: 2000);
        invitee.GuildId = 50; // already in a different guild
        Assert.False(svc.Invite(master, invitee));
    }

    [Fact]
    public void Invite_NullArgs_Fails()
    {
        var (svc, _, master, _) = BuildSeed();
        Assert.False(svc.Invite(master, null!));
        Assert.False(svc.Invite(null!, MakePc(500, 2000)));
    }

    // ----- Expulsion -----

    [Fact]
    public void Expulsion_Master_ExpelsRecruit()
    {
        var (svc, _, master, recruit) = BuildSeed();
        Assert.True(svc.Expulsion(master, master.GuildId,
            recruit.AccountId, recruit.CharacterId, "bye"));
    }

    [Fact]
    public void Expulsion_CannotExpelMaster()
    {
        var (svc, _, master, _) = BuildSeed();
        Assert.False(svc.Expulsion(master, master.GuildId,
            master.AccountId, master.CharacterId, "bye"));
    }

    [Fact]
    public void Expulsion_WithoutPermission_Fails()
    {
        var (svc, g, _, recruit) = BuildSeed();
        g.Members[2].Position = 2;
        g.Positions[2].Mode = GuildPermission.Invite; // Invite, not Expel
        // Recruit attempting to expel the lieutenant
        var lieutenant = svc.Find(1)!.Members[1];
        Assert.False(svc.Expulsion(recruit, recruit.GuildId,
            lieutenant.AccountId, lieutenant.CharId, "ha"));
    }

    [Fact]
    public void Expulsion_WithExpelPerm_Succeeds()
    {
        var (svc, g, _, recruit) = BuildSeed();
        g.Members[2].Position = 2;
        g.Positions[2].Mode = GuildPermission.Expel;
        var lieutenant = svc.Find(1)!.Members[1];
        Assert.True(svc.Expulsion(recruit, recruit.GuildId,
            lieutenant.AccountId, lieutenant.CharId, "ha"));
    }

    [Fact]
    public void Expulsion_UnknownMember_Fails()
    {
        var (svc, _, master, _) = BuildSeed();
        Assert.False(svc.Expulsion(master, master.GuildId,
            accountId: 9999, charId: 9999, "no such"));
    }

    // ----- ChangePosition -----

    [Fact]
    public void ChangePosition_Master_FlipsBits()
    {
        var (svc, g, master, _) = BuildSeed();
        Assert.True(svc.ChangePosition(master, idx: 3,
            mode: (int)GuildPermission.Invite, exp_mode: 50, name: "Officer"));
        Assert.Equal(GuildPermission.Invite, g.Positions[3].Mode);
        Assert.Equal(50, g.Positions[3].ExpMode);
        Assert.Equal("Officer", g.Positions[3].Name);
    }

    [Fact]
    public void ChangePosition_NonMaster_Fails()
    {
        var (svc, _, _, recruit) = BuildSeed();
        Assert.False(svc.ChangePosition(recruit, idx: 3,
            mode: (int)GuildPermission.Invite, exp_mode: 0, name: "X"));
    }

    [Fact]
    public void ChangePosition_OutOfRangeIdx_Fails()
    {
        var (svc, _, master, _) = BuildSeed();
        Assert.False(svc.ChangePosition(master, idx: 999, mode: 0, exp_mode: 0, name: "X"));
        Assert.False(svc.ChangePosition(master, idx: -1, mode: 0, exp_mode: 0, name: "X"));
    }

    [Fact]
    public void ChangePosition_Position0_AlwaysAllPermissions()
    {
        // The master slot can't be downgraded — protects against UI bugs.
        var (svc, g, master, _) = BuildSeed();
        Assert.True(svc.ChangePosition(master, idx: 0, mode: 0, exp_mode: 100, name: "Master"));
        Assert.Equal(GuildPermission.All, g.Positions[0].Mode);
    }

    // ----- Break -----

    [Fact]
    public void Break_SoloMaster_NameMatches_Succeeds()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = new GuildInfoData { GuildId = 1, Name = "Solo", MaxMember = 16, MasterCharacterId = 100 };
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1000, CharacterId = 100, Name = "M", ClassId = 1, Level = 99, Online = false
        });
        svc.OnRecvInfo(proto);
        var master = MakePc(charId: 100, accountId: 1000);
        master.GuildId = 1;
        Assert.True(svc.Break(master, "Solo"));
    }

    [Fact]
    public void Break_NonMaster_Fails()
    {
        var (svc, _, _, recruit) = BuildSeed();
        Assert.False(svc.Break(recruit, "T"));
    }

    [Fact]
    public void Break_NameMismatch_Fails()
    {
        var (svc, _, master, _) = BuildSeed();
        Assert.False(svc.Break(master, "WrongName"));
    }

    [Fact]
    public void Break_OtherMembersStillOn_Fails()
    {
        // Master alone can break; with anyone else on the roster, no.
        var (svc, _, master, _) = BuildSeed();
        Assert.False(svc.Break(master, "T"));
    }

    // ----- Leave -----

    [Fact]
    public void Leave_OwnSelf_Succeeds()
    {
        var (svc, _, _, recruit) = BuildSeed();
        Assert.True(svc.Leave(recruit, recruit.GuildId,
            recruit.AccountId, recruit.CharacterId, "see ya"));
    }

    [Fact]
    public void Leave_GuildIdMismatch_Fails()
    {
        var (svc, _, _, recruit) = BuildSeed();
        Assert.False(svc.Leave(recruit, recruit.GuildId + 1,
            recruit.AccountId, recruit.CharacterId, "X"));
    }

    [Fact]
    public void Leave_IdentityMismatch_Fails()
    {
        var (svc, _, _, recruit) = BuildSeed();
        // AID and CID for someone else
        Assert.False(svc.Leave(recruit, recruit.GuildId,
            accountId: 9999, charId: 9999, "X"));
    }

    // -----------------------------------------------------------------

    private static (GuildService svc, GuildEntity g, PlayerEntity master, PlayerEntity recruit) BuildSeed()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = new GuildInfoData
        {
            GuildId = 1, Name = "T", MaxMember = 16, MasterCharacterId = 100,
        };
        proto.Members.Add(new GuildMemberInfo { AccountId = 1000, CharacterId = 100, Name = "M", ClassId = 1, Level = 99, Online = true });
        proto.Members.Add(new GuildMemberInfo { AccountId = 1001, CharacterId = 200, Name = "L", ClassId = 1, Level = 80, Online = true });
        proto.Members.Add(new GuildMemberInfo { AccountId = 1002, CharacterId = 300, Name = "R", ClassId = 1, Level = 40, Online = true });
        proto.Positions.Add(new GuildPositionInfo { Index = 0, Name = "Master", Mode = (int)GuildPermission.All, ExpMode = 100 });
        proto.Positions.Add(new GuildPositionInfo { Index = 1, Name = "Lt", Mode = (int)(GuildPermission.Invite | GuildPermission.Expel), ExpMode = 50 });
        proto.Positions.Add(new GuildPositionInfo { Index = 2, Name = "Recruit", Mode = (int)GuildPermission.None, ExpMode = 0 });
        var g = svc.OnRecvInfo(proto);
        // Force position indices (hydrate defaults non-master to last slot).
        g.Members[0].Position = 0;
        g.Members[1].Position = 1;
        g.Members[2].Position = 2;

        var master = MakePc(charId: 100, accountId: 1000);
        master.GuildId = 1;
        var recruit = MakePc(charId: 300, accountId: 1002);
        recruit.GuildId = 1;
        return (svc, g, master, recruit);
    }

    private static PlayerEntity MakePc(int charId, int accountId)
        => new(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
