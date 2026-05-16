using Core.Database.Repositories.Api;
using Map.Server.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DbItem = Core.Database.Entities.ItemEntity;

namespace Map.Server.Tests.Items;

public class ItemCatalogTests
{
    [Fact]
    public void Load_PopulatesIdAndAegisNameIndexes()
    {
        var rows = new List<DbItem>
        {
            NewItem(501, "Red_Potion", "Red Potion"),
            NewItem(909, "Jellopy", "Jellopy"),
        };
        var catalog = NewCatalog(rows);

        Assert.Equal(2, catalog.Count);
        Assert.Equal("Red Potion", catalog.Get(501)!.NameEnglish);
        Assert.Equal(909u, catalog.GetByAegisName("Jellopy")!.Id);
    }

    [Fact]
    public void GetByAegisName_IsCaseInsensitive()
    {
        var catalog = NewCatalog(new[] { NewItem(909, "Jellopy", "Jellopy") });

        Assert.NotNull(catalog.GetByAegisName("JELLOPY"));
        Assert.NotNull(catalog.GetByAegisName("jellopy"));
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        var catalog = NewCatalog(Array.Empty<DbItem>());

        Assert.Null(catalog.Get(99999));
        Assert.Null(catalog.GetByAegisName("unknown"));
    }

    [Fact]
    public void Reload_SwapsToNewSnapshot()
    {
        var repo = new StubItemRepository(NewItem(501, "Red_Potion", "Red Potion"));
        var catalog = NewCatalog(repo);
        Assert.Equal(1, catalog.Count);

        repo.SetRows(
            NewItem(501, "Red_Potion", "Red Potion"),
            NewItem(909, "Jellopy", "Jellopy"));
        catalog.Reload();

        Assert.Equal(2, catalog.Count);
        Assert.NotNull(catalog.GetByAegisName("Jellopy"));
    }

    // ---- helpers ----

    private static DbItem NewItem(uint id, string aegis, string english) => new()
    {
        Id = id,
        NameAegis = aegis,
        NameEnglish = english,
    };

    private static ItemCatalog NewCatalog(IEnumerable<DbItem> rows) =>
        NewCatalog(new StubItemRepository(rows.ToArray()));

    private static ItemCatalog NewCatalog(IItemRepository repository)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => repository);
        var provider = services.BuildServiceProvider();
        return new ItemCatalog(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ItemCatalog>.Instance);
    }

    private sealed class StubItemRepository : IItemRepository
    {
        private List<DbItem> _rows;
        public StubItemRepository(params DbItem[] rows) => _rows = rows.ToList();
        public void SetRows(params DbItem[] rows) => _rows = rows.ToList();

        public Task<List<DbItem>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_rows.ToList());

        public Task<DbItem?> GetByIdAsync(uint itemId, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(r => r.Id == itemId));
        public Task<DbItem?> GetByAegisNameAsync(string aegisName, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(r =>
                string.Equals(r.NameAegis, aegisName, StringComparison.OrdinalIgnoreCase)));
        public Task<List<DbItem>> GetByTypeAsync(string type, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r => r.Type == type).ToList());
        public Task<List<DbItem>> GetByTypeAndSubtypeAsync(string type, string subtype, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r => r.Type == type && r.Subtype == subtype).ToList());
        public Task<List<DbItem>> SearchByNameAsync(string searchTerm, int limit = 50, CancellationToken ct = default)
            => Task.FromResult(_rows
                .Where(r => r.NameEnglish.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(limit).ToList());
        public Task<List<DbItem>> GetByPriceRangeAsync(uint minPrice, uint maxPrice, CancellationToken ct = default)
            => Task.FromResult(_rows
                .Where(r => r.PriceBuy >= minPrice && r.PriceBuy <= maxPrice).ToList());
        public Task<List<DbItem>> GetByEquipLocationAsync(string location, CancellationToken ct = default)
            => Task.FromResult(_rows.ToList());
    }
}
