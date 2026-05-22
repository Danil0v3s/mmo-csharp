using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// WOE-2 — verifies the per-PC guild EXP accumulator + minute-tick
/// flush. Mirrors rAthena <c>guild_payexp</c> (cpp:1681),
/// <c>guild_getexp</c> (cpp:1712), <c>guild_payexp_timer_sub</c>
/// (cpp:624) and the MAX_GUILD_EXP cap (config/const.hpp:71).
/// </summary>
public class GuildExpServiceTests
{
    // ---- PayExp ----

    [Fact]
    public void PayExp_NoGuild_Zero()
    {
        var (_, _, exp) = Build();
        var pc = MakePc(100, 1000); // no GuildId
        Assert.Equal(0, exp.PayExp(pc, 100));
    }

    [Fact]
    public void PayExp_GuildNotCached_Zero()
    {
        var (_, _, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 99; // not in cache
        Assert.Equal(0, exp.PayExp(pc, 100));
    }

    [Fact]
    public void PayExp_ZeroTax_Zero()
    {
        // Position with exp_mode=0 should pay no tax (rAthena cpp:1693).
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 0;
        Assert.Equal(0, exp.PayExp(pc, 1000));
    }

    [Fact]
    public void PayExp_PartialTax_PaysProportional()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 25; // 25% tax
        Assert.Equal(250, exp.PayExp(pc, 1000));
        Assert.Equal(250, exp.Peek(pc.CharacterId));
    }

    [Fact]
    public void PayExp_FullTax_PaysAll()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        Assert.Equal(1000, exp.PayExp(pc, 1000));
    }

    [Fact]
    public void PayExp_OverTax_TaxesEverything()
    {
        // rAthena cpp:1697 — exp_mode ≥ 100 returns the full amount.
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 250; // intentionally over 100
        Assert.Equal(1000, exp.PayExp(pc, 1000));
    }

    [Fact]
    public void PayExp_Accumulates_AcrossCalls()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        exp.PayExp(pc, 500);
        exp.PayExp(pc, 250);
        Assert.Equal(750, exp.Peek(pc.CharacterId));
    }

    [Fact]
    public void PayExp_ClampsAtMaxGuildExp()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        exp.PayExp(pc, long.MaxValue);
        Assert.Equal(GuildExpService.MaxGuildExp, exp.Peek(pc.CharacterId));
    }

    [Fact]
    public void PayExp_NegativeOrZero_NoOp()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        Assert.Equal(0, exp.PayExp(pc, 0));
        Assert.Equal(0, exp.PayExp(pc, -5));
    }

    // ---- GetExp ----

    [Fact]
    public void GetExp_BypassesTax_PaysFull()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        // Position with no exp_mode — GetExp still queues the full amount
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 0;
        Assert.Equal(500, exp.GetExp(pc, 500));
        Assert.Equal(500, exp.Peek(pc.CharacterId));
    }

    [Fact]
    public void GetExp_NoGuild_Zero()
    {
        var (_, _, exp) = Build();
        var pc = MakePc(100, 1000);
        Assert.Equal(0, exp.GetExp(pc, 500));
    }

    // ---- FlushOne / FlushAll ----

    [Fact]
    public void FlushOne_LandsExpOnGuildMember_AndClearsCache()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        exp.PayExp(pc, 500);

        Assert.Equal(500, exp.FlushOne(pc.CharacterId));

        Assert.Equal(500, g.Members[0].Exp);
        Assert.Equal(0, exp.Peek(pc.CharacterId));
    }

    [Fact]
    public void FlushOne_Accumulates_OnExistingMemberExp()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        g.Members[0].Exp = 10000;
        exp.PayExp(pc, 500);

        Assert.Equal(500, exp.FlushOne(pc.CharacterId));
        Assert.Equal(10500, g.Members[0].Exp);
    }

    [Fact]
    public void FlushOne_NotOnRoster_ReturnsZero()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(999, 9999);
        pc.GuildId = 1;
        // Manually inject into cache by calling GetExp which doesn't
        // need the PC to be on the roster.
        exp.GetExp(pc, 500);

        Assert.Equal(0, exp.FlushOne(pc.CharacterId));
        // Cache cleared regardless (rAthena cpp:633 frees the entry)
        Assert.Equal(0, exp.Peek(pc.CharacterId));
    }

    [Fact]
    public void FlushOne_NotInCache_Zero()
    {
        var (_, _, exp) = Build();
        Assert.Equal(0, exp.FlushOne(charId: 100));
    }

    [Fact]
    public void FlushOne_OverflowSafe_ClampsToMax()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        g.Members[0].Exp = GuildExpService.MaxGuildExp - 100;
        exp.PayExp(pc, 500);
        exp.FlushOne(pc.CharacterId);
        Assert.Equal(GuildExpService.MaxGuildExp, g.Members[0].Exp);
    }

    [Fact]
    public void FlushAll_DrainsEveryEntry()
    {
        var (guilds, g, exp) = Build();
        for (int i = 0; i < 3; i++)
        {
            var pc = MakePc(charId: 100 + i, accountId: 1000 + i);
            pc.GuildId = 1;
            // All three are guild members in this test seed
            g.Members.Add(new GuildMember { AccountId = pc.AccountId, CharId = pc.CharacterId, Name = $"M{i}", Level = 50, Position = 5 });
            g.Positions[5].ExpMode = 100;
            exp.GetExp(pc, 100 + i * 50);
        }

        Assert.Equal(3, exp.FlushAll());
        // Cache empty
        Assert.Empty(exp.Snapshot());
    }

    // ---- Roster drift safety ----

    [Fact]
    public void GuildSwitch_DiscardsStaleAccumulator()
    {
        var (guilds, g, exp) = Build();
        var pc = MakePc(100, 1000);
        pc.GuildId = 1;
        g.Members[0].Position = 5;
        g.Positions[5].ExpMode = 100;
        exp.PayExp(pc, 500);
        Assert.Equal(500, exp.Peek(pc.CharacterId));

        // PC moved to a different guild — second pay accumulates
        // fresh under the new guildId.
        var g2Proto = new GuildInfoData { GuildId = 2, Name = "B", MaxMember = 16, MasterCharacterId = 100 };
        g2Proto.Members.Add(new GuildMemberInfo { AccountId = 1000, CharacterId = 100, Name = "M", Level = 99, Online = true });
        g2Proto.Positions.Add(new GuildPositionInfo { Index = 0, Name = "Master", Mode = (int)GuildPermission.All, ExpMode = 100 });
        var g2 = guilds.OnRecvInfo(g2Proto);
        g2.Members[0].Position = 0;

        pc.GuildId = 2;
        exp.PayExp(pc, 300);
        // Old tally dropped; new GuildId on the cache.
        var snap = exp.Snapshot();
        Assert.Equal(2, snap[pc.CharacterId].GuildId);
        Assert.Equal(300, snap[pc.CharacterId].Exp);
    }

    // -----------------------------------------------------------------

    private static (GuildService guilds, GuildEntity g, GuildExpService exp) Build()
    {
        var guilds = new GuildService(NullLogger<GuildService>.Instance);
        var proto = new GuildInfoData
        {
            GuildId = 1, Name = "T", MaxMember = 16, MasterCharacterId = 100,
        };
        proto.Members.Add(new GuildMemberInfo { AccountId = 1000, CharacterId = 100, Name = "M", Level = 99, Online = true });
        // Position 5 is where the tests park the PC; pre-seed with sane defaults.
        proto.Positions.Add(new GuildPositionInfo { Index = 0, Name = "Master", Mode = (int)GuildPermission.All, ExpMode = 100 });
        for (int i = 1; i <= 6; i++)
            proto.Positions.Add(new GuildPositionInfo { Index = i, Name = $"R{i}", Mode = (int)GuildPermission.None, ExpMode = 0 });
        var g = guilds.OnRecvInfo(proto);
        var exp = new GuildExpService(NullLogger<GuildExpService>.Instance, guilds);
        return (guilds, g, exp);
    }

    private static PlayerEntity MakePc(int charId, int accountId)
        => new(charId, accountId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
