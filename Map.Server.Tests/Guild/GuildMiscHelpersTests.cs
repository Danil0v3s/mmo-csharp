using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-L1 — exercises the misc helpers: skill table (CheckSkill /
/// SkillGetMax / CheckSkillRequire / SkillUpAck / BlockSkill /
/// GuildAuraRefresh), member iteration (GetAvailableMemberCharId /
/// SendXyTimerSub), broken-sub cleanup (BrokenSub), and the guild
/// flag-NPC registry (FlagAdd / Remove / Clear). Mirrors rAthena
/// guild.cpp:235, :246, :255, :576, :1786, :1825, :2114, :2650+.
/// </summary>
public class GuildMiscHelpersTests
{
    // ---- Skill table ----

    [Fact]
    public void SkillGetMax_KnownAndUnknown()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal((ushort)1, svc.SkillGetMax(10010)); // GD_BATTLEORDER
        Assert.Equal((ushort)5, svc.SkillGetMax(10001)); // GD_KAFRACONTRACT
        Assert.Equal((ushort)10, svc.SkillGetMax(10004)); // GD_EXTENSION
        Assert.Equal((ushort)0, svc.SkillGetMax(60000)); // unknown
    }

    [Fact]
    public void CheckSkill_DefaultZero_Then_ReadAfterAck()
    {
        var (svc, g) = SeedGuildWith1Member();
        Assert.Equal(0, svc.CheckSkill(g.GuildId, 10004)); // GD_EXTENSION
        g.SkillPoints = 5;
        Assert.Equal(1, svc.SkillUpAck(g.GuildId, 10004, 1000));
        Assert.Equal(1, svc.CheckSkill(g.GuildId, 10004));
        Assert.Equal(4, g.SkillPoints); // consumed one
    }

    [Fact]
    public void SkillUpAck_AtMax_RefusesPromotion()
    {
        var (svc, g) = SeedGuildWith1Member();
        g.SkillPoints = 5;
        // GD_BATTLEORDER caps at 1
        Assert.Equal(1, svc.SkillUpAck(g.GuildId, 10010, 1000));
        Assert.Equal(0, svc.SkillUpAck(g.GuildId, 10010, 1000)); // already at max
        Assert.Equal(1, g.GetSkillLevel(10010));
    }

    [Fact]
    public void CheckSkillRequire_PermissiveForCachedGuild()
    {
        var (svc, g) = SeedGuildWith1Member();
        Assert.True(svc.CheckSkillRequire(g.GuildId, 10010));
        Assert.False(svc.CheckSkillRequire(99, 10010));
    }

    // ---- BlockSkill / Cooldown ----

    [Fact]
    public void BlockSkill_SetsCooldownOnAllBlockables()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(charId: 100, accountId: 1000);
        svc.BlockSkill(pc, durationMs: 60_000);
        // GD_BATTLEORDER / REGEN / RESTORE / EMERGENCYCALL
        foreach (var sid in new ushort[] { 10010, 10011, 10012, 10013 })
        {
            var rem = svc.GetBlockedSkillRemaining(pc, sid);
            Assert.InRange(rem, 1, 60_000);
        }
    }

    [Fact]
    public void BlockSkill_NoOpForNullOrZero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.BlockSkill(null!, 1000); // no throw
        var pc = MakePc(100, 1000);
        svc.BlockSkill(pc, 0);       // no cooldown set
        Assert.Equal(0, svc.GetBlockedSkillRemaining(pc, 10010));
    }

    [Fact]
    public void GetBlockedSkillRemaining_UnsetReturnsZero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var pc = MakePc(100, 1000);
        Assert.Equal(0, svc.GetBlockedSkillRemaining(pc, 10010));
    }

    // ---- GuildAuraRefresh ----

    [Fact]
    public void GuildAuraRefresh_DoesNotThrow_NullSafe()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.GuildAuraRefresh(null!, 10006, 1);
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        svc.GuildAuraRefresh(pc, 10006, 1); // GD_LEADERSHIP smoke
    }

    // ---- GetAvailableMemberCharId ----

    [Fact]
    public void GetAvailableMemberCharId_ReturnsFirstOnline_Or_Zero()
    {
        var (svc, g) = SeedGuildWith3Members();
        g.Members[0].Online = false;
        g.Members[1].Online = true;
        g.Members[2].Online = false;
        Assert.Equal(200, svc.GetAvailableMemberCharId(g.GuildId));

        g.Members[1].Online = false;
        Assert.Equal(0, svc.GetAvailableMemberCharId(g.GuildId));
    }

    [Fact]
    public void GetAvailableMemberCharId_UnknownGuild_Zero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.GetAvailableMemberCharId(99));
    }

    // ---- SendXyTimerSub ----

    [Fact]
    public void SendXyTimerSub_ReturnsOnlineMemberCharIds()
    {
        var (svc, g) = SeedGuildWith3Members();
        g.Members[0].Online = true;
        g.Members[1].Online = false;
        g.Members[2].Online = true;
        var ids = svc.SendXyTimerSub(g.GuildId);
        Assert.Equal(2, ids.Count);
        Assert.Contains(100, ids);
        Assert.Contains(300, ids);
    }

    [Fact]
    public void SendXyTimerSub_UnknownGuild_Empty()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Empty(svc.SendXyTimerSub(99));
    }

    // ---- BrokenSub ----

    [Fact]
    public void BrokenSub_DropsCacheAndClearsReferencesElsewhere()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var g1 = svc.OnRecvInfo(MakeProto(1, "A"));
        var g2 = svc.OnRecvInfo(MakeProto(2, "B"));
        var g3 = svc.OnRecvInfo(MakeProto(3, "C"));
        g1.Alliances.Add(new GuildAlliance { GuildId = 2, IsOpposition = false });
        g3.Alliances.Add(new GuildAlliance { GuildId = 2, IsOpposition = true });

        var touched = svc.BrokenSub(2);

        Assert.Equal(2, touched);
        Assert.Null(svc.Find(2));
        Assert.False(g1.IsAllied(2));
        Assert.False(g3.IsOpposition(2));
    }

    [Fact]
    public void BrokenSub_UnknownId_Zero()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(0, svc.BrokenSub(0));
        Assert.Equal(0, svc.BrokenSub(99));
    }

    // ---- Flag NPC registry ----

    [Fact]
    public void FlagAdd_AndRemove_RoundTrips()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.FlagAdd(10);
        svc.FlagAdd(20);
        svc.FlagAdd(10); // dup
        Assert.Equal(2, svc.GetFlagNpcs().Count);
        Assert.Contains(10, svc.GetFlagNpcs());
        Assert.Contains(20, svc.GetFlagNpcs());

        svc.FlagRemove(10);
        Assert.DoesNotContain(10, svc.GetFlagNpcs());
        Assert.Contains(20, svc.GetFlagNpcs());

        svc.FlagsClear();
        Assert.Empty(svc.GetFlagNpcs());
    }

    [Fact]
    public void FlagAdd_ZeroIgnored()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.FlagAdd(0);
        Assert.Empty(svc.GetFlagNpcs());
    }

    // ---- RetrieveItemBound ----

    [Fact]
    public void RetrieveItemBound_DispatchesAndReturnsOne()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.Equal(1, svc.RetrieveItemBound(charId: 100, accountId: 1000, guildId: 1));
    }

    // -----------------------------------------------------------------

    private static (GuildService svc, GuildEntity g) SeedGuildWith1Member()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = MakeProto(1, "T");
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1000, CharacterId = 100, Name = "M", Level = 99, Online = true
        });
        return (svc, svc.OnRecvInfo(proto));
    }

    private static (GuildService svc, GuildEntity g) SeedGuildWith3Members()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = MakeProto(1, "T");
        proto.Members.Add(new GuildMemberInfo { AccountId = 1000, CharacterId = 100, Name = "M", Level = 99, Online = true });
        proto.Members.Add(new GuildMemberInfo { AccountId = 1001, CharacterId = 200, Name = "L", Level = 80, Online = true });
        proto.Members.Add(new GuildMemberInfo { AccountId = 1002, CharacterId = 300, Name = "R", Level = 40, Online = true });
        return (svc, svc.OnRecvInfo(proto));
    }

    private static GuildInfoData MakeProto(int id, string name) => new()
    {
        GuildId = id, Name = name, MaxMember = 16, MasterCharacterId = 100,
    };

    private static PlayerEntity MakePc(int charId, int accountId)
        => new(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
