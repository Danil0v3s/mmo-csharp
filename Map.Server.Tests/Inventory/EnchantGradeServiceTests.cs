using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Inventory;

public class EnchantGradeServiceTests
{
    [Fact]
    public void Reload_PopulatesChanceMap()
    {
        var svc = NewService(new[]
        {
            Chance("Armor", 1, "D", 0, 10000),
            Chance("Armor", 1, "C", 0, 8000),
            Chance("Weapon1", 4, "A", 10, 100),
        });

        Assert.True(svc.IsLoaded);
        Assert.Equal(10000, svc.GetUpgradeChance("Armor", 1, "D", 0));
        Assert.Equal(8000, svc.GetUpgradeChance("Armor", 1, "C", 0));
        Assert.Equal(100, svc.GetUpgradeChance("Weapon1", 4, "A", 10));
    }

    [Fact]
    public void GetUpgradeChance_ReturnsZeroForUnknownOrEmpty()
    {
        var svc = NewService(new[] { Chance("Armor", 1, "D", 0, 10000) });
        Assert.Equal(0, svc.GetUpgradeChance("Armor", 1, "D", 99)); // wrong refine
        Assert.Equal(0, svc.GetUpgradeChance("Weapon1", 1, "D", 0)); // wrong type
        Assert.Equal(0, svc.GetUpgradeChance("Armor", 99, "D", 0)); // wrong level
        Assert.Equal(0, svc.GetUpgradeChance("Armor", 1, "X", 0)); // unknown grade
        Assert.Equal(0, svc.GetUpgradeChance("", 1, "D", 0));
        Assert.Equal(0, svc.GetUpgradeChance("Armor", 1, "", 0));
    }

    [Fact]
    public void EmptyCatalog_IsLoadedFalse()
    {
        var svc = new EnchantGradeService(NullLogger<EnchantGradeService>.Instance);
        Assert.False(svc.IsLoaded);
        Assert.Equal(0, svc.GetUpgradeChance("Armor", 1, "D", 0));
    }

    private static EnchantGradeChanceDbEntity Chance(string equipType, int itemLvl, string grade, int refine, int chance) => new()
    {
        EquipType = equipType,
        ItemLevel = itemLvl,
        Grade = grade,
        Refine = refine,
        Chance = chance,
    };

    private static EnchantGradeService NewService(EnchantGradeChanceDbEntity[] chances)
    {
        var services = new ServiceCollection();
        services.AddScoped<IEnchantGradeDbRepository>(_ => new StubRepo(chances.ToList()));
        var provider = services.BuildServiceProvider();
        return new EnchantGradeService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EnchantGradeService>.Instance);
    }

    private sealed class StubRepo : IEnchantGradeDbRepository
    {
        private readonly List<EnchantGradeChanceDbEntity> _chances;
        public StubRepo(List<EnchantGradeChanceDbEntity> chances) => _chances = chances;
        public Task<IReadOnlyList<EnchantGradeDbEntity>> GetAllGroupsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EnchantGradeDbEntity>>(new List<EnchantGradeDbEntity>());
        public Task<IReadOnlyList<EnchantGradeLevelDbEntity>> GetAllLevelsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EnchantGradeLevelDbEntity>>(new List<EnchantGradeLevelDbEntity>());
        public Task<IReadOnlyList<EnchantGradeChanceDbEntity>> GetAllChancesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EnchantGradeChanceDbEntity>>(_chances);
    }
}
