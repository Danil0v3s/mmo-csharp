using Map.Server.Mob;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.MobDatabase;

public class MobDbTests
{
    [Fact]
    public void LoadFromTwoFiles_OverrideReplacesPrimaryEntry()
    {
        var primary = WriteTempYaml("""
            Header:
              Type: MOB_DB
              Version: 4
            Body:
              - Id: 1002
                AegisName: PORING
                Name: Poring
                Hp: 50
              - Id: 1004
                AegisName: HORNET
                Name: Hornet
                Hp: 100
            """);

        var overrideFile = WriteTempYaml("""
            Header:
              Type: MOB_DB
              Version: 4
            Body:
              - Id: 1002
                AegisName: PORING
                Name: PoringBuffed
                Hp: 99999
              - Id: 9999
                AegisName: NEWMOB
                Name: NewMob
                Hp: 7
            """);

        try
        {
            var db = new MobDb(primary, overrideFile, NullLogger<MobDb>.Instance);

            Assert.Equal(3, db.Count);
            Assert.Equal(99999, db.Get(1002)!.Hp);
            Assert.Equal("PoringBuffed", db.Get(1002)!.Name);
            Assert.Equal(100, db.Get(1004)!.Hp);     // primary untouched
            Assert.Equal(7, db.Get(9999)!.Hp);       // new entry added
            Assert.NotNull(db.GetByAegisName("NEWMOB"));
        }
        finally
        {
            File.Delete(primary);
            File.Delete(overrideFile);
        }
    }

    [Fact]
    public void GetByAegisName_IsCaseInsensitive()
    {
        var primary = WriteTempYaml("""
            Header:
              Type: MOB_DB
              Version: 4
            Body:
              - Id: 1002
                AegisName: PORING
                Name: Poring
            """);
        try
        {
            var db = new MobDb(primary, overridePath: null, NullLogger<MobDb>.Instance);
            Assert.NotNull(db.GetByAegisName("poring"));
            Assert.NotNull(db.GetByAegisName("Poring"));
        }
        finally { File.Delete(primary); }
    }

    [Fact]
    public void MissingOverride_IsSilentlySkipped()
    {
        var primary = WriteTempYaml("""
            Header:
              Type: MOB_DB
              Version: 4
            Body:
              - Id: 1002
                AegisName: PORING
                Name: Poring
            """);
        try
        {
            var db = new MobDb(primary, "/no/such/file.yml", NullLogger<MobDb>.Instance);
            Assert.Equal(1, db.Count);
        }
        finally { File.Delete(primary); }
    }

    [Fact]
    public void MissingPrimary_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            new MobDb("/no/such/file.yml", overridePath: null, NullLogger<MobDb>.Instance));
    }

    [Fact]
    public void RealMobDb_LoadsAndContainsPoring()
    {
        const string path = "/Volumes/1TB/Projetos/rathena/db/re/mob_db.yml";
        if (!File.Exists(path))
        {
            // rAthena renewal mob_db.yml isn't shipped with the repo; skip
            // silently in environments that don't have a local checkout.
            return;
        }

        var db = new MobDb(path, overridePath: null, NullLogger<MobDb>.Instance);
        Assert.True(db.Count > 1000, $"Expected >1000 entries, got {db.Count}");

        var poring = db.Get(1002);
        Assert.NotNull(poring);
        Assert.Equal("PORING", poring!.AegisName);
        Assert.Equal(1, poring.Level);
        Assert.True(poring.Hp > 0);
        Assert.Equal(poring, db.GetByAegisName("PORING"));
    }

    private static string WriteTempYaml(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mobdb_test_{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, content);
        return path;
    }
}
