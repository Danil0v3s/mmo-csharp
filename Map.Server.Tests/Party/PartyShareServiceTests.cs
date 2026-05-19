using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Status;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Party;

public class PartyShareServiceTests
{
    [Fact]
    public void Solo_KillerNotInParty_ReturnsFalse()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, partyId: 0);
        Assert.False(ctx.Service.ShareKill(pc, 1000, 500));
    }

    [Fact]
    public void Party_OneMember_DoesntSplit_FallsBackToSolo()
    {
        // Single member of a party — service returns false so the caller
        // gives all EXP via the regular IExpService path.
        var ctx = Build();
        var pc = ctx.AddPlayer(1, partyId: 42);
        Assert.False(ctx.Service.ShareKill(pc, 1000, 500));
        // Confirm no EXP was awarded by the share path.
        Assert.Equal(0, pc.BaseExp);
    }

    [Fact]
    public void Party_TwoMembers_SplitsWithEvenShareBonus()
    {
        var ctx = Build();
        var a = ctx.AddPlayer(1, partyId: 42);
        var b = ctx.AddPlayer(2, partyId: 42);

        Assert.True(ctx.Service.ShareKill(a, 1000, 500));

        // Each gets half × (100 + 10)/100 = 550 base, 275 job.
        Assert.Equal(550, a.BaseExp);
        Assert.Equal(550, b.BaseExp);
        Assert.Equal(275, a.JobExp);
        Assert.Equal(275, b.JobExp);
    }

    [Fact]
    public void Party_DeadMembersExcluded_FromSplit()
    {
        var ctx = Build();
        var a = ctx.AddPlayer(1, partyId: 42);
        var b = ctx.AddPlayer(2, partyId: 42);
        var c = ctx.AddPlayer(3, partyId: 42);
        c.Hp = 0; // dead — shouldn't share

        ctx.Service.ShareKill(a, 1000, 0);

        // 2 eligible × (1000 / 2) × 110% = 550 each.
        Assert.Equal(550, a.BaseExp);
        Assert.Equal(550, b.BaseExp);
        Assert.Equal(0, c.BaseExp);
    }

    [Fact]
    public void Party_DifferentMap_ExcludedFromShare()
    {
        var ctx = Build();
        var a = ctx.AddPlayer(1, partyId: 42);
        var b = ctx.AddPlayer(2, partyId: 42, mapId: 999);

        ctx.Service.ShareKill(a, 1000, 0);
        // Only A is eligible — service short-circuits to false.
        Assert.Equal(0, b.BaseExp);
    }

    private static TestContext Build()
    {
        var map = new MapData("test_map", 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var calc = new StatusCalcService();
        var exp = new ExpService(calc, new NoSessions(), NullLogger<ExpService>.Instance);
        var svc = new PartyShareService(entities, exp, NullLogger<PartyShareService>.Instance);
        return new TestContext(svc, entities, (uint)"test_map".GetHashCode());
    }

    private sealed record TestContext(
        PartyShareService Service,
        EntityRegistry Entities,
        uint DefaultMapId)
    {
        public PlayerEntity AddPlayer(int charId, int partyId, uint? mapId = null)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(),
                mapId ?? DefaultMapId, 100, 100);
            pc.Hp = pc.MaxHp = 1000;
            pc.PartyId = partyId;
            // High level so the test EXP amounts don't trigger a level-up
            // (which would over-carry cap BaseExp to next-1 and break
            // the simple "received N" assertion below).
            pc.Level = 99;
            pc.JobLevel = ExpTable.MaxJobLevel; // also at cap to avoid job level-up cap
            Entities.Add(pc);
            return pc;
        }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }

    private sealed class NoSessions : ISessionManagerAccessor
    {
        public Map.Server.MapSessionData? GetByEntityId(EntityId entityId) => null;
    }
}
