using Core.Server.IPC;
using Map.Server.Guild;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-H1 — covers <see cref="GuildEntity.GetIndex"/> /
/// <see cref="GuildEntity.GetPosition"/> /
/// <see cref="GuildEntity.IsAllied"/> /
/// <see cref="GuildEntity.IsOpposition"/> /
/// <see cref="GuildEntity.GetAllianceCount"/>. Mirrors rAthena
/// <c>guild_getindex</c> (cpp:584), <c>guild_getposition</c>
/// (cpp:592), <c>guild_isallied</c> (cpp:2630),
/// <c>guild_get_alliance_count</c> (cpp:1813).
/// </summary>
public class GuildEntityLookupTests
{
    [Fact]
    public void GetIndex_FindsMember()
    {
        var g = MakeGuildWith3Members();
        Assert.Equal(0, g.GetIndex(1000, 100));
        Assert.Equal(1, g.GetIndex(1001, 200));
        Assert.Equal(2, g.GetIndex(1002, 300));
    }

    [Fact]
    public void GetIndex_MissingMember_ReturnsMinus1()
    {
        var g = MakeGuildWith3Members();
        Assert.Equal(-1, g.GetIndex(9999, 9999));
        Assert.Equal(-1, g.GetIndex(1000, 999)); // matching AID but wrong CID
    }

    [Fact]
    public void GetPosition_FollowsMember()
    {
        var g = MakeGuildWith3Members();
        // Hand-set positions
        g.Members[0].Position = 0; // master
        g.Members[1].Position = 3;
        g.Members[2].Position = 5;

        Assert.Equal(0, g.GetPosition(1000, 100));
        Assert.Equal(3, g.GetPosition(1001, 200));
        Assert.Equal(5, g.GetPosition(1002, 300));
        Assert.Equal(-1, g.GetPosition(9999, 9999));
    }

    [Fact]
    public void Alliance_AlliedAndOpposition_Disambiguate()
    {
        var g = MakeGuildWith3Members();
        g.Alliances.Add(new GuildAlliance { GuildId = 10, Name = "Ally", IsOpposition = false });
        g.Alliances.Add(new GuildAlliance { GuildId = 20, Name = "Enemy", IsOpposition = true });

        Assert.True(g.IsAllied(10));
        Assert.False(g.IsAllied(20));   // opposed, not allied
        Assert.False(g.IsAllied(30));   // unknown

        Assert.True(g.IsOpposition(20));
        Assert.False(g.IsOpposition(10));
    }

    [Fact]
    public void GetAllianceCount_SeparatesByOpposition()
    {
        var g = MakeGuildWith3Members();
        g.Alliances.Add(new GuildAlliance { GuildId = 10, IsOpposition = false });
        g.Alliances.Add(new GuildAlliance { GuildId = 11, IsOpposition = false });
        g.Alliances.Add(new GuildAlliance { GuildId = 12, IsOpposition = false });
        g.Alliances.Add(new GuildAlliance { GuildId = 20, IsOpposition = true });
        g.Alliances.Add(new GuildAlliance { GuildId = 21, IsOpposition = true });

        Assert.Equal(3, g.GetAllianceCount(opposition: false));
        Assert.Equal(2, g.GetAllianceCount(opposition: true));
    }

    [Fact]
    public void GetSkillLevel_DefaultsToZero_HitsCacheWhenPresent()
    {
        var g = MakeGuildWith3Members();
        Assert.Equal(0, g.GetSkillLevel(skillId: 10010)); // GD_BATTLEORDER
        g.Skills[10010] = 3;
        Assert.Equal(3, g.GetSkillLevel(skillId: 10010));
        Assert.Equal(0, g.GetSkillLevel(skillId: 10011)); // still unset
    }

    private static GuildEntity MakeGuildWith3Members()
    {
        var g = new GuildEntity
        {
            GuildId = 1,
            Name = "Test",
            GuildLv = 1,
            MaxMember = 16,
            MasterCharId = 100,
            MasterName = "M",
        };
        g.Members.Add(new GuildMember { AccountId = 1000, CharId = 100, Name = "M", Level = 99 });
        g.Members.Add(new GuildMember { AccountId = 1001, CharId = 200, Name = "L", Level = 80 });
        g.Members.Add(new GuildMember { AccountId = 1002, CharId = 300, Name = "R", Level = 40 });
        return g;
    }
}
