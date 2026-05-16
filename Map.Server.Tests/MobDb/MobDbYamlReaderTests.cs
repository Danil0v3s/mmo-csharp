using Map.Server.Mob;

namespace Map.Server.Tests.MobDatabase;

public class MobDbYamlReaderTests
{
    [Fact]
    public void Read_ParsesAllScalarFields()
    {
        var entries = MobDbYamlReader.Read(new StringReader(MinimalYaml));

        var entry = Assert.Single(entries);
        Assert.Equal(1002, entry.Id);
        Assert.Equal("PORING", entry.AegisName);
        Assert.Equal("Poring", entry.Name);
        Assert.Equal(1, entry.Level);
        Assert.Equal(55, entry.Hp);
        Assert.Equal(150, entry.BaseExp);
        Assert.Equal(1, entry.AttackRange);
        Assert.Equal("Medium", entry.Size);
        Assert.Equal("Plant", entry.Race);
        Assert.Equal("Water", entry.Element);
        Assert.Equal(400, entry.WalkSpeed);
    }

    [Fact]
    public void Read_ParsesDropsList()
    {
        var entries = MobDbYamlReader.Read(new StringReader(MinimalYaml));
        var entry = entries.Single();

        Assert.Equal(2, entry.Drops.Count);
        Assert.Equal("Jellopy", entry.Drops[0].Item);
        Assert.Equal(7000, entry.Drops[0].Rate);
        Assert.Equal("Poring_Card", entry.Drops[1].Item);
        Assert.True(entry.Drops[1].StealProtected);
    }

    [Fact]
    public void Read_ParsesModesAndRaceGroupFlags()
    {
        var entries = MobDbYamlReader.Read(new StringReader(WithFlagsYaml));
        var entry = entries.Single();

        Assert.True(entry.Modes["Detector"]);
        Assert.True(entry.RaceGroups["Clocktower"]);
    }

    [Fact]
    public void Read_DefaultsMissingOptionalFields()
    {
        var entries = MobDbYamlReader.Read(new StringReader(BareMinimumYaml));
        var entry = entries.Single();

        Assert.Equal(1, entry.Level);
        Assert.Equal(1, entry.Hp);
        Assert.Equal("Small", entry.Size);
        Assert.Equal("Formless", entry.Race);
        Assert.Equal("Neutral", entry.Element);
        Assert.Empty(entry.Drops);
    }

    [Fact]
    public void Read_MissingId_Throws()
    {
        const string yaml = """
            Header:
              Type: MOB_DB
              Version: 4
            Body:
              - AegisName: NO_ID
                Name: NoId
            """;
        Assert.Throws<InvalidDataException>(() => MobDbYamlReader.Read(new StringReader(yaml)));
    }

    [Fact]
    public void Read_EmptyDocument_ReturnsEmpty()
    {
        var entries = MobDbYamlReader.Read(new StringReader("Header:\n  Type: MOB_DB\n  Version: 4\n"));
        Assert.Empty(entries);
    }

    private const string MinimalYaml = """
        Header:
          Type: MOB_DB
          Version: 4
        Body:
          - Id: 1002
            AegisName: PORING
            Name: Poring
            Level: 1
            Hp: 55
            BaseExp: 150
            JobExp: 40
            Attack: 1
            Attack2: 1
            AttackRange: 1
            Size: Medium
            Race: Plant
            Element: Water
            WalkSpeed: 400
            Drops:
              - Item: Jellopy
                Index: 0
                Rate: 7000
              - Item: Poring_Card
                Index: 7
                Rate: 20
                StealProtected: true
        """;

    private const string WithFlagsYaml = """
        Header:
          Type: MOB_DB
          Version: 4
        Body:
          - Id: 1036
            AegisName: GHOUL
            Name: Ghoul
            Hp: 1968
            Race: Undead
            RaceGroups:
              Clocktower: true
            Modes:
              Detector: true
        """;

    private const string BareMinimumYaml = """
        Header:
          Type: MOB_DB
          Version: 4
        Body:
          - Id: 9999
            AegisName: BARE
            Name: BareMob
        """;
}
