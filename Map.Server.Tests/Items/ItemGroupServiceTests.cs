using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Items;

public class ItemGroupServiceTests
{
    [Fact]
    public void Reload_PopulatesGroupAndSubGroupIndexes()
    {
        var svc = NewService(
            Group("Bloody_Branch"),
            Entry("Bloody_Branch", subGroup: 1, idx: 1, aegis: "Goblin", rate: 50),
            Entry("Bloody_Branch", subGroup: 1, idx: 2, aegis: "Orc_Lady", rate: 50),
            Entry("Bloody_Branch", subGroup: 2, idx: 1, aegis: "Eclipse", rate: 100));

        Assert.Equal(1, svc.GroupCount);
        Assert.True(svc.HasGroup("Bloody_Branch"));
        Assert.False(svc.HasGroup("Unknown"));
        Assert.Equal(new[] { 1, 2 }, svc.SubGroupsOf("Bloody_Branch").ToArray());
        Assert.Equal(new[] { 1, 2 }, svc.SubGroupsOf("BLOODY_BRANCH").ToArray()); // case-insensitive
    }

    [Fact]
    public void RollFromGroup_ReturnsNullForUnknownGroupOrSubGroup()
    {
        var svc = NewService(
            Group("Old_Card_Album"),
            Entry("Old_Card_Album", subGroup: 1, idx: 1, aegis: "Poring_Card", rate: 100));

        Assert.Null(svc.RollFromGroup("Missing", 1));
        Assert.Null(svc.RollFromGroup("Old_Card_Album", 99));
        Assert.Null(svc.RollFromGroup("", 1));
    }

    [Fact]
    public void RollFromGroup_RespectsWeightDistribution()
    {
        // 90/10 split — over many rolls the heavy entry should dominate.
        var svc = NewService(
            Group("Test_Bag"),
            Entry("Test_Bag", subGroup: 1, idx: 1, aegis: "Common", rate: 900),
            Entry("Test_Bag", subGroup: 1, idx: 2, aegis: "Rare", rate: 100));

        int common = 0, rare = 0;
        for (int i = 0; i < 10000; i++)
        {
            var pick = svc.RollFromGroup("Test_Bag", 1);
            if (pick!.ItemAegis == "Common") common++;
            else rare++;
        }
        // Loose bounds — 90/10 → expect ~9000/1000. Allow ±5%.
        Assert.InRange(common, 8500, 9500);
        Assert.InRange(rare, 500, 1500);
    }

    [Fact]
    public void RollAllSubGroups_PicksOneEntryPerSubGroup()
    {
        var svc = NewService(
            Group("Multi"),
            Entry("Multi", subGroup: 1, idx: 1, aegis: "A", rate: 100),
            Entry("Multi", subGroup: 2, idx: 1, aegis: "B", rate: 100),
            Entry("Multi", subGroup: 3, idx: 1, aegis: "C", rate: 100));

        var result = svc.RollAllSubGroups("Multi");
        Assert.Equal(3, result.Count);
        Assert.Contains(result, e => e.ItemAegis == "A");
        Assert.Contains(result, e => e.ItemAegis == "B");
        Assert.Contains(result, e => e.ItemAegis == "C");
    }

    [Fact]
    public void RollAllSubGroups_ReturnsEmptyForUnknownGroup()
    {
        var svc = NewService();
        Assert.Empty(svc.RollAllSubGroups("Nope"));
        Assert.Empty(svc.RollAllSubGroups(""));
    }

    [Fact]
    public void ZeroOrNegativeRate_IsTreatedAsWeightOne()
    {
        // Defensive — misseeded rows shouldn't break the bucket totals.
        var svc = NewService(
            Group("Defensive"),
            Entry("Defensive", subGroup: 1, idx: 1, aegis: "Zero", rate: 0),
            Entry("Defensive", subGroup: 1, idx: 2, aegis: "Negative", rate: -5));

        // Both entries should still be reachable.
        var seen = new HashSet<string>();
        for (int i = 0; i < 1000; i++) seen.Add(svc.RollFromGroup("Defensive", 1)!.ItemAegis);
        Assert.Contains("Zero", seen);
        Assert.Contains("Negative", seen);
    }

    // ---- helpers ----

    private static ItemGroupCatalogDbEntity Group(string name) => new() { GroupName = name };

    private static ItemGroupCatalogEntryDbEntity Entry(string group, int subGroup, int idx, string aegis, int rate) => new()
    {
        GroupName = group,
        SubGroup = subGroup,
        Index = idx,
        ItemAegis = aegis,
        Rate = rate,
        Amount = 1,
    };

    private static ItemGroupService NewService(params object[] rows)
    {
        var groups = rows.OfType<ItemGroupCatalogDbEntity>().ToList();
        var entries = rows.OfType<ItemGroupCatalogEntryDbEntity>().ToList();
        var services = new ServiceCollection();
        services.AddScoped<IItemGroupCatalogDbRepository>(_ => new StubRepo(groups, entries));
        var provider = services.BuildServiceProvider();
        return new ItemGroupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ItemGroupService>.Instance);
    }

    private sealed class StubRepo : IItemGroupCatalogDbRepository
    {
        private readonly List<ItemGroupCatalogDbEntity> _groups;
        private readonly List<ItemGroupCatalogEntryDbEntity> _entries;
        public StubRepo(List<ItemGroupCatalogDbEntity> g, List<ItemGroupCatalogEntryDbEntity> e)
        { _groups = g; _entries = e; }

        public Task<IReadOnlyList<ItemGroupCatalogDbEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemGroupCatalogDbEntity>>(_groups);

        public Task<IReadOnlyList<ItemGroupCatalogEntryDbEntity>> GetEntriesAsync(string groupName, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemGroupCatalogEntryDbEntity>>(
                _entries.Where(e => string.Equals(e.GroupName, groupName, StringComparison.OrdinalIgnoreCase)).ToList());
    }
}
