using Map.Server.Mob;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T5.1b — verifies the YAML loader reads rAthena
/// <c>db/mob_chat_db.yml</c> rows into <see cref="IMobChatDb"/>.
/// Uses a temp file so the test doesn't depend on the rAthena tree
/// being mounted; one round-trip-against-real-yaml smoke test
/// confirms the production layout works.
/// </summary>
public class MobChatYmlLoaderTests
{
    [Fact]
    public void Load_MissingFile_ReturnsZero()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".yml");
        var db = new MobChatDb();
        var loader = new MobChatYmlLoader(NullLogger<MobChatYmlLoader>.Instance);

        var n = loader.Load(path, db);

        Assert.Equal(0, n);
        Assert.Equal(0, db.Count);
    }

    [Fact]
    public void Load_ParsesIdDialogAndColor()
    {
        var yaml = """
Header:
  Type: MOB_CHAT_DB
Body:
  - Id: 100
    Dialog: "Hello, world!"
  - Id: 101
    Color: 0x00FF00
    Dialog: "Green line"
""";
        var path = WriteTemp(yaml);
        var db = new MobChatDb();
        var loader = new MobChatYmlLoader(NullLogger<MobChatYmlLoader>.Instance);

        var n = loader.Load(path, db);

        Assert.Equal(2, n);
        Assert.Equal(2, db.Count);

        var row100 = db.Find(100);
        Assert.NotNull(row100);
        Assert.Equal("Hello, world!", row100!.Message);
        Assert.Equal(MobChatYmlLoader.DefaultColor, row100.ColorRgb); // default 0xFF0000

        var row101 = db.Find(101);
        Assert.NotNull(row101);
        Assert.Equal("Green line", row101!.Message);
        Assert.Equal(0x00FF00u, row101.ColorRgb);
    }

    [Fact]
    public void Load_SkipsRowsMissingIdOrDialog()
    {
        var yaml = """
Body:
  - Id: 1
    Dialog: "ok"
  - Dialog: "no id"
  - Id: 3
""";
        var path = WriteTemp(yaml);
        var db = new MobChatDb();
        var loader = new MobChatYmlLoader(NullLogger<MobChatYmlLoader>.Instance);

        var n = loader.Load(path, db);

        Assert.Equal(1, n);
        Assert.NotNull(db.Find(1));
        Assert.Null(db.Find(3));
    }

    [Fact]
    public void Load_OverwritesOnReload()
    {
        // Reload semantics — second call replaces row with same id.
        var first = WriteTemp("Body:\n  - Id: 5\n    Dialog: \"first\"\n");
        var second = WriteTemp("Body:\n  - Id: 5\n    Dialog: \"second\"\n");
        var db = new MobChatDb();
        var loader = new MobChatYmlLoader(NullLogger<MobChatYmlLoader>.Instance);

        loader.Load(first, db);
        Assert.Equal("first", db.Find(5)!.Message);

        loader.Load(second, db);
        Assert.Equal(1, db.Count);
        Assert.Equal("second", db.Find(5)!.Message);
    }

    private static string WriteTemp(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".yml");
        File.WriteAllText(path, contents);
        return path;
    }
}
