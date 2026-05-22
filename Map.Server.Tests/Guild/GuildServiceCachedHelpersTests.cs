using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-H1 — verifies the cache-backed helpers that ride on
/// <see cref="GuildService.Find"/>: <c>CheckMember</c>,
/// <c>CheckAlliance</c>, and the <c>All()</c> enumeration. These were
/// stubs before GD-H1 and now resolve from the in-memory replica.
/// </summary>
public class GuildServiceCachedHelpersTests
{
    [Fact]
    public void CheckMember_HitsCachedRoster()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var proto = new GuildInfoData
        {
            GuildId = 1, Name = "T", MaxMember = 16, MasterCharacterId = 100
        };
        proto.Members.Add(new GuildMemberInfo
        {
            AccountId = 1000, CharacterId = 100, Name = "M",
            ClassId = 1, Level = 1, Online = true
        });
        svc.OnRecvInfo(proto);

        var pc = MakePc(charId: 100, accountId: 1000);
        Assert.True(svc.CheckMember(1, pc));

        var stranger = MakePc(charId: 999, accountId: 9999);
        Assert.False(svc.CheckMember(1, stranger));
        Assert.False(svc.CheckMember(999, pc));   // unknown guild id
    }

    [Fact]
    public void CheckAlliance_FlagDisambiguates()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        var g = svc.OnRecvInfo(new GuildInfoData { GuildId = 1, Name = "A", MaxMember = 16 });
        g.Alliances.Add(new GuildAlliance { GuildId = 10, IsOpposition = false }); // ally
        g.Alliances.Add(new GuildAlliance { GuildId = 20, IsOpposition = true });  // enemy

        // flag=0 → allied lookup
        Assert.Equal(1, svc.CheckAlliance(1, 10, 0));
        Assert.Equal(0, svc.CheckAlliance(1, 20, 0));   // is enemy, not ally
        // flag=1 → opposition lookup
        Assert.Equal(1, svc.CheckAlliance(1, 20, 1));
        Assert.Equal(0, svc.CheckAlliance(1, 10, 1));   // is ally, not enemy
        // unknown guild
        Assert.Equal(0, svc.CheckAlliance(999, 10, 0));
    }

    [Fact]
    public void All_EnumeratesEveryCachedGuild()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        svc.OnRecvInfo(new GuildInfoData { GuildId = 1, Name = "A", MaxMember = 16 });
        svc.OnRecvInfo(new GuildInfoData { GuildId = 2, Name = "B", MaxMember = 16 });
        svc.OnRecvInfo(new GuildInfoData { GuildId = 3, Name = "C", MaxMember = 16 });

        var ids = new System.Collections.Generic.HashSet<int>();
        foreach (var g in svc.All()) ids.Add(g.GuildId);

        Assert.Equal(3, ids.Count);
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
        Assert.Contains(3, ids);
    }

    private static PlayerEntity MakePc(int charId, int accountId)
        // Ctor is (characterId, accountId, name, sessionId, mapId, x, y).
        => new(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
