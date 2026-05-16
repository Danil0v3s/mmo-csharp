using Core.Database.Repositories.Api;
using Map.Server.Mob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DbMob = Core.Database.Entities.MobEntity;

namespace Map.Server.Tests.MobDatabase;

public class MobDbTests
{
    [Fact]
    public void Load_PopulatesIdAndAegisNameIndexes()
    {
        var rows = new List<DbMob>
        {
            NewMob(1002, "PORING", "Poring", hp: 55),
            NewMob(1004, "HORNET", "Hornet", hp: 100),
        };

        var db = NewMobDb(rows);

        Assert.Equal(2, db.Count);
        Assert.Equal("Poring", db.Get(1002)!.Name);
        Assert.Equal(55, db.Get(1002)!.Hp);
        Assert.NotNull(db.GetByAegisName("HORNET"));
    }

    [Fact]
    public void GetByAegisName_IsCaseInsensitive()
    {
        var db = NewMobDb(new List<DbMob> { NewMob(1002, "PORING", "Poring") });

        Assert.NotNull(db.GetByAegisName("poring"));
        Assert.NotNull(db.GetByAegisName("Poring"));
    }

    [Fact]
    public void Drops_FlattenedFromInlineColumns_IncludeAllPopulatedSlots()
    {
        var poring = NewMob(1002, "PORING", "Poring", hp: 55);
        poring.Drop1Item = "Jellopy"; poring.Drop1Rate = 7000;
        poring.Drop2Item = "Knife_"; poring.Drop2Rate = 100;
        poring.Drop8Item = "Poring_Card"; poring.Drop8Rate = 20; poring.Drop8Nosteal = 1;
        // Slot 3-7 / 9-10 left null — should not appear in the list.

        var db = NewMobDb(new List<DbMob> { poring });
        var drops = db.Get(1002)!.Drops;

        Assert.Equal(3, drops.Count);
        Assert.Equal("Jellopy", drops[0].Item);
        Assert.Equal(7000, drops[0].Rate);
        Assert.Equal("Poring_Card", drops[2].Item);
        Assert.True(drops[2].StealProtected);
    }

    [Fact]
    public void MvpDrops_FlattenedFromMvpdropColumns()
    {
        var mvp = NewMob(1038, "OSIRIS", "Osiris", hp: 668000);
        mvp.Mvpdrop1Item = "Bs_Making_S"; mvp.Mvpdrop1Rate = 5000;
        mvp.Mvpdrop2Item = "Seed_Of_Yggdrasil"; mvp.Mvpdrop2Rate = 3000;
        mvp.ModeMvp = 1;

        var db = NewMobDb(new List<DbMob> { mvp });
        var entry = db.Get(1038)!;

        Assert.Equal(2, entry.MvpDrops.Count);
        Assert.Equal("Bs_Making_S", entry.MvpDrops[0].Item);
        Assert.True(entry.Modes["Mvp"]);
    }

    [Fact]
    public void ModesAndRaceGroups_ParsedFromBitColumns()
    {
        var ghoul = NewMob(1036, "GHOUL", "Ghoul", hp: 2429);
        ghoul.ModeDetector = 1;
        ghoul.RacegroupClocktower = 1;

        var db = NewMobDb(new List<DbMob> { ghoul });
        var entry = db.Get(1036)!;

        Assert.True(entry.Modes["Detector"]);
        Assert.True(entry.RaceGroups["Clocktower"]);
        // Unset flags shouldn't appear in the dictionary at all.
        Assert.False(entry.Modes.ContainsKey("Mvp"));
    }

    [Fact]
    public void Reload_SwapsToNewSnapshot()
    {
        var repo = new StubMobRepository(NewMob(1002, "PORING", "Poring", hp: 55));
        var db = NewMobDb(repo);

        Assert.Equal(55, db.Get(1002)!.Hp);

        // Simulate a hot-patch: change underlying data + reload.
        repo.SetRows(NewMob(1002, "PORING", "Poring", hp: 99));
        db.Reload();
        Assert.Equal(99, db.Get(1002)!.Hp);
    }

    [Fact]
    public void EmptyRepository_ProducesEmptyDb()
    {
        var db = NewMobDb(new List<DbMob>());
        Assert.Equal(0, db.Count);
        Assert.Null(db.Get(1002));
    }

    // ---- helpers ----

    private static DbMob NewMob(uint id, string aegis, string english, uint hp = 1) => new()
    {
        Id = id,
        NameAegis = aegis,
        NameEnglish = english,
        NameJapanese = english,
        Level = 1,
        Hp = hp,
    };

    private static MobDb NewMobDb(IEnumerable<DbMob> rows) =>
        NewMobDb(new StubMobRepository(rows.ToArray()));

    private static MobDb NewMobDb(IMobRepository repository)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => repository);
        var provider = services.BuildServiceProvider();
        return new MobDb(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MobDb>.Instance);
    }

    private sealed class StubMobRepository : IMobRepository
    {
        private List<DbMob> _rows;
        public StubMobRepository(params DbMob[] rows) => _rows = rows.ToList();
        public void SetRows(params DbMob[] rows) => _rows = rows.ToList();

        public Task<List<DbMob>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_rows.ToList());

        public Task<DbMob?> GetByIdAsync(uint mobId, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(r => r.Id == mobId));
        public Task<DbMob?> GetByAegisNameAsync(string aegisName, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(r =>
                string.Equals(r.NameAegis, aegisName, StringComparison.OrdinalIgnoreCase)));

        public Task<List<DbMob>> GetByLevelRangeAsync(ushort minLevel, ushort maxLevel, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r => r.Level >= minLevel && r.Level <= maxLevel).ToList());
        public Task<List<DbMob>> GetByRaceAsync(string race, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r => r.Race == race).ToList());
        public Task<List<DbMob>> GetByElementAsync(string element, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r => r.Element == element).ToList());
        public Task<List<DbMob>> GetBySizeAsync(string size, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r => r.Size == size).ToList());
        public Task<List<DbMob>> GetAllMvpsAsync(CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r => (r.ModeMvp ?? 0) != 0).ToList());
        public Task<List<DbMob>> SearchByNameAsync(string searchTerm, int limit = 50, CancellationToken ct = default)
            => Task.FromResult(_rows
                .Where(r => r.NameEnglish.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(limit).ToList());
        public Task<List<DbMob>> GetByDropItemAsync(string itemName, CancellationToken ct = default)
            => Task.FromResult(_rows.Where(r =>
                r.Drop1Item == itemName || r.Drop2Item == itemName).ToList());
    }
}
