using Map.Server.Guild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// GD-L3 — castle data model + WoE-prep bookkeeping. Mirrors rAthena
/// <c>guild_castle_map_init</c> (cpp:2370), <c>guild_castledatasave</c>
/// (cpp:2390), <c>guild_castledataloadack</c> (cpp:2483),
/// <c>guild_castle_reconnect</c> (cpp:2465), <c>guild_checkcastles</c>
/// (cpp:2620), <c>castle_guild_broken_sub</c> (cpp:2132).
/// </summary>
public class GuildCastleTests
{
    // ---- RegisterCastle + lookup ----

    [Fact]
    public void RegisterCastle_RoundTrips_AndAllCastles_Enumerates()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1, Name = "Aldebaran 1", MapId = 100 });
        svc.RegisterCastle(new CastleEntity { CastleId = 2, Name = "Aldebaran 2", MapId = 101 });

        var c = svc.FindCastle(1);
        Assert.NotNull(c);
        Assert.Equal("Aldebaran 1", c!.Name);

        var ids = new System.Collections.Generic.HashSet<int>();
        foreach (var cs in svc.AllCastles()) ids.Add(cs.CastleId);
        Assert.Equal(2, ids.Count);
    }

    [Fact]
    public void RegisterCastle_IgnoresNullOrZeroId()
    {
        var svc = Build();
        svc.RegisterCastle(null!);
        svc.RegisterCastle(new CastleEntity { CastleId = 0 });
        Assert.Empty(svc.AllCastles());
    }

    // ---- CastleMapInit ----

    [Fact]
    public void CastleMapInit_ReturnsRegisteredCount()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1 });
        svc.RegisterCastle(new CastleEntity { CastleId = 2 });
        svc.RegisterCastle(new CastleEntity { CastleId = 3 });
        Assert.Equal(3, svc.CastleMapInit());
    }

    [Fact]
    public void CastleMapInit_EmptyDb_ReturnsZero()
    {
        var svc = Build();
        Assert.Equal(0, svc.CastleMapInit());
    }

    // ---- CheckCastles ----

    [Fact]
    public void CheckCastles_CountsByOwningGuild()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1, GuildId = 10 });
        svc.RegisterCastle(new CastleEntity { CastleId = 2, GuildId = 10 });
        svc.RegisterCastle(new CastleEntity { CastleId = 3, GuildId = 20 });
        svc.RegisterCastle(new CastleEntity { CastleId = 4, GuildId = 0 });

        Assert.Equal(2, svc.CheckCastles(10));
        Assert.Equal(1, svc.CheckCastles(20));
        Assert.Equal(0, svc.CheckCastles(30)); // unowned by anyone
        Assert.Equal(0, svc.CheckCastles(0));
    }

    // ---- CastleDataSave / CastleDataLoadAck ----

    [Fact]
    public void CastleDataSave_MutatesCachedField_AndEnqueuesPending()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1, GuildId = 0 });

        Assert.Equal(1, svc.CastleDataSave(1, CastleDataIndex.GuildId, value: 42));
        Assert.Equal(42, svc.FindCastle(1)!.GuildId);
        Assert.Single(svc.GetPendingCastleSaves());
        Assert.Equal(42, svc.GetPendingCastleSaves()[(1, CastleDataIndex.GuildId)]);
    }

    [Fact]
    public void CastleDataSave_OutOfRangeIndex_Zero()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1 });
        Assert.Equal(0, svc.CastleDataSave(1, index: 999, value: 1));
    }

    [Fact]
    public void CastleDataSave_UnknownCastle_Zero()
    {
        var svc = Build();
        Assert.Equal(0, svc.CastleDataSave(99, CastleDataIndex.GuildId, 42));
    }

    [Fact]
    public void CastleDataSave_GuardianRange_LandsOnDictionary()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1 });
        // Guardian slot 0 (CD_ENABLED_GUARDIAN00) → visible
        Assert.Equal(1, svc.CastleDataSave(1, CastleDataIndex.EnabledGuardian00, 1));
        // Guardian slot 7 (last valid)
        Assert.Equal(1, svc.CastleDataSave(1, CastleDataIndex.EnabledGuardian00 + 7, 1));
        // Index == Max is OOR
        Assert.Equal(0, svc.CastleDataSave(1, CastleDataIndex.Max, 1));

        var c = svc.FindCastle(1)!;
        Assert.Equal(1, c.GuardianVisible[0]);
        Assert.Equal(1, c.GuardianVisible[7]);
    }

    [Fact]
    public void CastleDataLoadAck_AllocatesAndPaints()
    {
        var svc = Build();
        // No prior register — load-ack should auto-allocate.
        Assert.Equal(1, svc.CastleDataLoadAck(7, CastleDataIndex.CurrentEconomy, 1000));
        var c = svc.FindCastle(7);
        Assert.NotNull(c);
        Assert.Equal(1000, c!.Economy);
    }

    [Fact]
    public void CastleDataLoad_DelegatesToMapInit()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1 });
        svc.RegisterCastle(new CastleEntity { CastleId = 2 });
        Assert.Equal(2, svc.CastleDataLoad());
    }

    // ---- CastleReconnect ----

    [Fact]
    public void CastleReconnect_EnqueuesAndFlushes()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1 });
        svc.CastleDataSave(1, CastleDataIndex.CurrentEconomy, 500);
        Assert.Single(svc.GetPendingCastleSaves());

        // -1 flushes
        svc.CastleReconnect(-1, 0, 0);
        Assert.Empty(svc.GetPendingCastleSaves());
    }

    [Fact]
    public void CastleReconnect_LastWriteWins()
    {
        var svc = Build();
        svc.CastleReconnect(1, CastleDataIndex.CurrentEconomy, 100);
        svc.CastleReconnect(1, CastleDataIndex.CurrentEconomy, 200);
        Assert.Equal(200, svc.GetPendingCastleSaves()[(1, CastleDataIndex.CurrentEconomy)]);
    }

    // ---- CastleGuildBrokenSub ----

    [Fact]
    public void CastleGuildBrokenSub_ZerosOwnership()
    {
        var svc = Build();
        svc.RegisterCastle(new CastleEntity { CastleId = 1, GuildId = 10 });
        svc.RegisterCastle(new CastleEntity { CastleId = 2, GuildId = 10 });
        svc.RegisterCastle(new CastleEntity { CastleId = 3, GuildId = 20 });

        Assert.Equal(2, svc.CastleGuildBrokenSub(10));
        Assert.Equal(0, svc.FindCastle(1)!.GuildId);
        Assert.Equal(0, svc.FindCastle(2)!.GuildId);
        Assert.Equal(20, svc.FindCastle(3)!.GuildId); // untouched
    }

    [Fact]
    public void CastleGuildBrokenSub_BadId_Zero()
    {
        var svc = Build();
        Assert.Equal(0, svc.CastleGuildBrokenSub(0));
        Assert.Equal(0, svc.CastleGuildBrokenSub(-1));
    }

    private static GuildService Build() => new(NullLogger<GuildService>.Instance);
}
