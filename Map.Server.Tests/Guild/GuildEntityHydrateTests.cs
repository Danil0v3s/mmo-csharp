using Core.Server.IPC;
using Google.Protobuf;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-H1 — exercises the in-memory replica + OnRecvInfo hydrate path
/// that closes rAthena <c>guild_recv_info</c> / <c>guild_search</c>
/// (guild.cpp:822 / cpp:166).
/// </summary>
public class GuildEntityHydrateTests
{
    [Fact]
    public void Find_UnknownGuild_ReturnsNull()
    {
        var svc = Build();
        Assert.Null(svc.Find(99));
        Assert.Equal(0, svc.CachedCount);
    }

    [Fact]
    public void OnRecvInfo_CreatesEntityFromProto()
    {
        var svc = Build();
        var g = svc.OnRecvInfo(MakeProto(id: 1, master: 100, "Alpha"));

        Assert.NotNull(g);
        Assert.Equal(1, g.GuildId);
        Assert.Equal("Alpha", g.Name);
        Assert.Equal(100, g.MasterCharId);
        Assert.Equal(1, svc.CachedCount);

        // Round-trip lookup
        Assert.Same(g, svc.Find(1));
    }

    [Fact]
    public void OnRecvInfo_IsIdempotent_RefreshesInPlace()
    {
        var svc = Build();
        var first = svc.OnRecvInfo(MakeProto(id: 1, master: 100, "Alpha"));
        var second = svc.OnRecvInfo(MakeProto(id: 1, master: 100, "Alpha-renamed"));
        // Same reference — so existing callers keep observing the new state
        Assert.Same(first, second);
        Assert.Equal("Alpha-renamed", first.Name);
        Assert.Equal(1, svc.CachedCount);
    }

    [Fact]
    public void OnRecvInfo_PopulatesMembersAndComputesOnlineCount()
    {
        var svc = Build();
        var proto = MakeProto(id: 1, master: 100, "Alpha");
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1000, CharacterId = 100, Name = "Master",
            ClassId = 4060, Level = 99, Online = true
        });
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1001, CharacterId = 200, Name = "Lieutenant",
            ClassId = 4060, Level = 80, Online = true
        });
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1002, CharacterId = 300, Name = "Recruit",
            ClassId = 4001, Level = 40, Online = false
        });

        var g = svc.OnRecvInfo(proto);

        Assert.Equal(3, g.Members.Count);
        Assert.Equal(2, g.ConnectMember);
        // Average over the three levels (99 + 80 + 40) / 3 = 73
        Assert.Equal(73, g.AverageLevel);
        // Master sits at position 0 after hydrate
        Assert.Equal(0, g.Members[0].Position);
        Assert.Equal("Master", g.MasterName);
    }

    [Fact]
    public void OnRecvInfo_PopulatesPositionsAndSeedsMaxPosition()
    {
        var svc = Build();
        var proto = MakeProto(id: 1, master: 100, "Alpha");
        proto.Positions.Add(new GuildPositionInfo
        {
            Index = 0, Name = "Master", Mode = (int)GuildPermission.All, ExpMode = 100
        });
        proto.Positions.Add(new GuildPositionInfo
        {
            Index = 1, Name = "Lieutenant",
            Mode = (int)(GuildPermission.Invite | GuildPermission.Expel),
            ExpMode = 50
        });

        var g = svc.OnRecvInfo(proto);

        // Always back-filled to MAX_GUILDPOSITION (20) so a permission
        // query against an unset slot returns Mode == None instead of
        // OOB-ing.
        Assert.Equal(GuildLimits.MaxPosition, g.Positions.Count);
        Assert.Equal("Master", g.Positions[0].Name);
        Assert.Equal(GuildPermission.All, g.Positions[0].Mode);
        Assert.Equal(GuildPermission.Invite | GuildPermission.Expel, g.Positions[1].Mode);
        Assert.Equal(GuildPermission.None, g.Positions[19].Mode);
    }

    [Fact]
    public void OnRecvInfo_Position0_AlwaysAllPermissions()
    {
        // rAthena guarantees the master slot has GUILD_PERM_ALL even if
        // the YAML/SQL says otherwise. We protect against a malformed
        // proto by forcing slot 0 to All.
        var svc = Build();
        var proto = MakeProto(id: 1, master: 100, "Alpha");
        proto.Positions.Add(new GuildPositionInfo { Index = 0, Name = "Master", Mode = 0, ExpMode = 0 });
        var g = svc.OnRecvInfo(proto);
        Assert.Equal(GuildPermission.All, g.Positions[0].Mode);
    }

    [Fact]
    public void OnRecvInfo_TruncatesMembersAtMaxGuildCap()
    {
        var svc = Build();
        var proto = MakeProto(id: 1, master: 100, "Big");
        // Try to load 100 members — must clamp to MAX_GUILD = 76.
        for (int i = 0; i < 100; i++)
        {
            proto.Members.Add(new GuildMemberInfo
            {
                AccountId = 1000 + i, CharacterId = 200 + i,
                Name = $"P{i}", ClassId = 1, Level = 1, Online = false
            });
        }
        var g = svc.OnRecvInfo(proto);
        Assert.Equal(GuildLimits.MaxMember, g.Members.Count);
    }

    [Fact]
    public void OnRecvInfo_Rejects_NullProto_AndZeroId()
    {
        var svc = Build();
        Assert.Throws<System.ArgumentNullException>(() => svc.OnRecvInfo(null!));
        Assert.Throws<System.ArgumentException>(() => svc.OnRecvInfo(MakeProto(id: 0, master: 100, "Bad")));
    }

    [Fact]
    public void RecvNoInfo_DropsCachedEntry()
    {
        var svc = Build();
        svc.OnRecvInfo(MakeProto(id: 7, master: 100, "Alpha"));
        Assert.Equal(1, svc.CachedCount);
        svc.RecvNoInfo(7);
        Assert.Equal(0, svc.CachedCount);
        Assert.Null(svc.Find(7));
    }

    [Fact]
    public void Reload_ClearsAllCache()
    {
        var svc = Build();
        svc.OnRecvInfo(MakeProto(id: 1, master: 100, "A"));
        svc.OnRecvInfo(MakeProto(id: 2, master: 200, "B"));
        svc.OnRecvInfo(MakeProto(id: 3, master: 300, "C"));
        Assert.Equal(3, svc.CachedCount);

        svc.Reload();
        Assert.Equal(0, svc.CachedCount);
    }

    [Fact]
    public void Final_ClearsCache()
    {
        var svc = Build();
        svc.OnRecvInfo(MakeProto(id: 1, master: 100, "A"));
        svc.Final();
        Assert.Equal(0, svc.CachedCount);
    }

    private static GuildService Build() => new(NullLogger<GuildService>.Instance);

    private static GuildInfoData MakeProto(int id, int master, string name)
        => new()
        {
            GuildId = id,
            Name = name,
            Level = 1,
            MaxMember = 16,
            MasterCharacterId = master,
            EmblemVersion = 0,
            EmblemData = ByteString.Empty,
            Notice1 = string.Empty,
            Notice2 = string.Empty,
        };
}
