using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Skills;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Party;

/// <summary>
/// Wave 87 — coverage for the map-side party cache + helper family
/// (<c>party_search</c>, <c>party_isleader</c>, <c>party_skill_check</c>,
/// <c>party_recv_info</c>, <c>party_recv_noinfo</c>).
/// </summary>
public class PartyServiceTests
{
    private static PartyService Build() =>
        new(NullLogger<PartyService>.Instance);

    private static PlayerEntity NewPlayer(int charId, int partyId = 0)
    {
        var p = new PlayerEntity(charId, accountId: 1000 + charId,
            name: $"Char{charId}", sessionId: Guid.NewGuid(),
            mapId: 1, x: 100, y: 100);
        p.PartyId = partyId;
        return p;
    }

    [Fact]
    public void Get_UnknownParty_ReturnsNull()
    {
        var svc = Build();
        Assert.Null(svc.Get(123));
        Assert.Null(svc.Get(0));
    }

    [Fact]
    public void ApplySnapshot_StoresPartyAndSetsLeaderFlag()
    {
        var svc = Build();
        var party = svc.ApplySnapshot(42, "alpha", exp: 1, item: 0,
            leaderCharacterId: 100, leaderAccountId: 1100,
            members: new[]
            {
                new MapPartyMember { CharacterId = 100, AccountId = 1100, Name = "leader", Online = true, ClassId = 15 },
                new MapPartyMember { CharacterId = 101, AccountId = 1101, Name = "alice", Online = true, ClassId = 23 },
            });
        Assert.NotNull(svc.Get(42));
        Assert.Same(party, svc.Get(42));
        Assert.True(party.Members[100].Leader);
        Assert.False(party.Members[101].Leader);
    }

    [Fact]
    public void IsLeader_TrueForLeader_FalseForMember_FalseForSolo()
    {
        var svc = Build();
        svc.ApplySnapshot(42, "p", 1, 0, 100, 1100, new[]
        {
            new MapPartyMember { CharacterId = 100, Online = true },
            new MapPartyMember { CharacterId = 101, Online = true },
        });

        var leader = NewPlayer(100, partyId: 42);
        var member = NewPlayer(101, partyId: 42);
        var solo = NewPlayer(102, partyId: 0);

        Assert.True(svc.IsLeader(leader));
        Assert.False(svc.IsLeader(member));
        Assert.False(svc.IsLeader(solo));
    }

    [Fact]
    public void GetMember_ReturnsRecordOrNull()
    {
        var svc = Build();
        svc.ApplySnapshot(42, "p", 0, 0, 100, 0, new[]
        {
            new MapPartyMember { CharacterId = 100, Name = "leader", Online = true },
        });
        Assert.NotNull(svc.GetMember(42, 100));
        Assert.Equal("leader", svc.GetMember(42, 100)!.Name);
        Assert.Null(svc.GetMember(42, 999));
        Assert.Null(svc.GetMember(999, 100));
    }

    [Fact]
    public void Forget_DropsCachedEntry()
    {
        var svc = Build();
        svc.ApplySnapshot(42, "p", 0, 0, 100, 0,
            new[] { new MapPartyMember { CharacterId = 100, Online = true } });
        Assert.NotNull(svc.Get(42));
        svc.Forget(42);
        Assert.Null(svc.Get(42));
    }

    [Fact]
    public void SkillCheck_TkCounter_GatedByMonk()
    {
        var svc = Build();
        // Party with one Acolyte (15=Monk)
        svc.ApplySnapshot(42, "p", 0, 0, 100, 0, new[]
        {
            new MapPartyMember { CharacterId = 100, ClassId = 15, Online = true },
            new MapPartyMember { CharacterId = 101, ClassId = 23, Online = true }, // SN
        });
        var caster = NewPlayer(100, partyId: 42);
        // Monk in party → TK_COUNTER returns 1
        Assert.Equal(1, svc.SkillCheck(caster, 42, SkillIds.TK_COUNTER, 1));
        // No SG → MO_COMBOFINISH gate fails
        Assert.Equal(0, svc.SkillCheck(caster, 42, SkillIds.MO_COMBOFINISH, 1));
        // SN present → AM_TWILIGHT2 = 1
        Assert.Equal(1, svc.SkillCheck(caster, 42, SkillIds.AM_TWILIGHT2, 1));
        // No TK → AM_TWILIGHT3 = 0
        Assert.Equal(0, svc.SkillCheck(caster, 42, SkillIds.AM_TWILIGHT3, 1));
    }

    [Fact]
    public void SkillCheck_NoParty_ReturnsZero()
    {
        var svc = Build();
        var caster = NewPlayer(100, partyId: 0);
        Assert.Equal(0, svc.SkillCheck(caster, 0, SkillIds.TK_COUNTER, 1));
    }

    [Fact]
    public void UpdateMemberMap_UpdatesCachedRow()
    {
        var svc = Build();
        svc.ApplySnapshot(42, "p", 0, 0, 100, 0, new[]
        {
            new MapPartyMember { CharacterId = 100, MapName = "prontera", Level = 50, Online = true },
        });
        svc.UpdateMemberMap(42, 100, "geffen", 75, false);
        var m = svc.GetMember(42, 100)!;
        Assert.Equal("geffen", m.MapName);
        Assert.Equal(75u, m.Level);
        Assert.False(m.Online);
    }

    [Fact]
    public void OnLevelUp_NoParty_NoOp()
    {
        var svc = Build();
        var pc = NewPlayer(100, partyId: 0);
        // No-op (no exception, no cache mutation).
        svc.OnLevelUp(pc);
        Assert.Null(svc.Get(0));
    }

    [Fact]
    public void OnLevelUp_PartyMember_RefreshesCachedLevel()
    {
        var svc = Build();
        svc.ApplySnapshot(42, "p", 0, 0, 100, 0, new[]
        {
            new MapPartyMember { CharacterId = 100, Level = 50, Online = true },
        });
        var pc = NewPlayer(100, partyId: 42);
        pc.Level = 51;
        svc.OnLevelUp(pc);
        Assert.Equal(51u, svc.GetMember(42, 100)!.Level);
    }
}
