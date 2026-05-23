using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Inventory;

public class RefineServiceTests
{
    [Fact]
    public void Reload_PopulatesLevelsAndChances()
    {
        var svc = NewService(
            groups: new[] { "Armor", "Weapon1" },
            levels: new[]
            {
                Level("Armor", 1, 1, bonus: 1),
                Level("Armor", 1, 2, bonus: 2),
                Level("Weapon1", 1, 1, bonus: 2),
            },
            chances: new[]
            {
                Chance("Armor", 1, 1, "Normal", rate: 10000, price: 200, mat: "Elunium"),
                Chance("Armor", 1, 2, "Normal", rate: 9000, price: 200, mat: "Elunium"),
                Chance("Weapon1", 1, 1, "HD", rate: 10000, price: 200, mat: "HD_Elunium"),
            });

        Assert.True(svc.IsLoaded);
        Assert.Equal(1, svc.GetRefineBonus("Armor", 1, 1));
        Assert.Equal(2, svc.GetRefineBonus("Armor", 1, 2));
        Assert.Equal(2, svc.GetRefineBonus("Weapon1", 1, 1));
    }

    [Fact]
    public void GetRefineBonus_ReturnsZeroForUnknown()
    {
        var svc = NewService(
            groups: new[] { "Armor" },
            levels: new[] { Level("Armor", 1, 1, bonus: 1) },
            chances: Array.Empty<RefineChanceDbEntity>());

        Assert.Equal(0, svc.GetRefineBonus("Armor", 1, 99));
        Assert.Equal(0, svc.GetRefineBonus("Missing", 1, 1));
        Assert.Equal(0, svc.GetRefineBonus("", 1, 1));
    }

    [Fact]
    public void GetRefineChance_ReturnsRowOrNull()
    {
        var svc = NewService(
            groups: new[] { "Armor" },
            levels: Array.Empty<RefineLevelDbEntity>(),
            chances: new[]
            {
                Chance("Armor", 1, 5, "Normal", rate: 6000, price: 300, mat: "Elunium"),
                Chance("Armor", 1, 5, "Enriched", rate: 9000, price: 600, mat: "Enriched_Elunium"),
            });

        var normal = svc.GetRefineChance("Armor", 1, 5, "Normal");
        Assert.NotNull(normal);
        Assert.Equal(6000, normal!.Rate);
        Assert.Equal(300, normal.Price);
        Assert.Equal("Elunium", normal.MaterialAegis);

        var enriched = svc.GetRefineChance("Armor", 1, 5, "Enriched");
        Assert.NotNull(enriched);
        Assert.Equal("Enriched_Elunium", enriched!.MaterialAegis);

        // Missing chance type
        Assert.Null(svc.GetRefineChance("Armor", 1, 5, "HD"));
        // Wrong refine level
        Assert.Null(svc.GetRefineChance("Armor", 1, 99, "Normal"));
        // Wrong group
        Assert.Null(svc.GetRefineChance("Missing", 1, 5, "Normal"));
        // Empty inputs
        Assert.Null(svc.GetRefineChance("", 1, 5, "Normal"));
        Assert.Null(svc.GetRefineChance("Armor", 1, 5, ""));
    }

    [Fact]
    public void EmptyCatalog_IsLoadedFalse()
    {
        var svc = new RefineService(NullLogger<RefineService>.Instance);
        Assert.False(svc.IsLoaded);
        Assert.Equal(0, svc.GetRefineBonus("Armor", 1, 1));
        Assert.Null(svc.GetRefineChance("Armor", 1, 1, "Normal"));
    }

    // ---- helpers ----

    private static RefineLevelDbEntity Level(string group, int itemLvl, int refineLvl, int bonus) => new()
    {
        GroupName = group,
        ItemLevel = itemLvl,
        RefineLevel = refineLvl,
        Bonus = bonus,
    };

    private static RefineChanceDbEntity Chance(string group, int itemLvl, int refineLvl, string type, int rate, int price, string mat) => new()
    {
        GroupName = group,
        ItemLevel = itemLvl,
        RefineLevel = refineLvl,
        ChanceType = type,
        Rate = rate,
        Price = price,
        MaterialAegis = mat,
    };

    private static RefineService NewService(
        string[] groups,
        RefineLevelDbEntity[] levels,
        RefineChanceDbEntity[] chances)
    {
        var services = new ServiceCollection();
        services.AddScoped<IRefineDbRepository>(_ => new StubRepo(
            groups.Select(g => new RefineGroupDbEntity { GroupName = g }).ToList(),
            levels.ToList(),
            chances.ToList()));
        var provider = services.BuildServiceProvider();
        return new RefineService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RefineService>.Instance);
    }

    private sealed class StubRepo : IRefineDbRepository
    {
        private readonly List<RefineGroupDbEntity> _groups;
        private readonly List<RefineLevelDbEntity> _levels;
        private readonly List<RefineChanceDbEntity> _chances;
        public StubRepo(List<RefineGroupDbEntity> g, List<RefineLevelDbEntity> l, List<RefineChanceDbEntity> c)
        { _groups = g; _levels = l; _chances = c; }

        public Task<IReadOnlyList<RefineGroupDbEntity>> GetAllGroupsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RefineGroupDbEntity>>(_groups);
        public Task<IReadOnlyList<RefineLevelDbEntity>> GetAllLevelsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RefineLevelDbEntity>>(_levels);
        public Task<IReadOnlyList<RefineChanceDbEntity>> GetAllChancesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RefineChanceDbEntity>>(_chances);
        public Task<IReadOnlyList<RefineLevelDbEntity>> GetLevelsForGroupAsync(string groupName, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RefineLevelDbEntity>>(_levels.Where(l => l.GroupName == groupName).ToList());
        public Task<IReadOnlyList<RefineChanceDbEntity>> GetChancesForGroupAsync(string groupName, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RefineChanceDbEntity>>(_chances.Where(c => c.GroupName == groupName).ToList());
    }
}
