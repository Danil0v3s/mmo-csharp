using Core.Server.Packets;
using Tools.PacketReplay;
using Tools.PacketReplay.Tokens;
using Xunit.Abstractions;

namespace Map.Server.Tests.Replay;

/// <summary>
/// End-to-end packet-replay tests. The class fixture spins up Login/Char/Map
/// once for the class; each <see cref="ReplayFile"/> case opens a fresh
/// session and validates the captured transcript packet-by-packet.
///
/// Fixture files live under <c>Replay/Fixtures/*.log</c> and are copied to
/// the test output dir at build time. To add a new replay test, drop the
/// capture into that directory and rebuild.
/// </summary>
public sealed class PacketReplayTests : IClassFixture<ServerStackFixture>
{
    private readonly ServerStackFixture _fixture;
    private readonly ITestOutputHelper _output;

    public PacketReplayTests(ServerStackFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Theory]
    [MemberData(nameof(FixturePaths))]
    public async Task Replay(string fixtureFileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Replay", "Fixtures", fixtureFileName);
        var file = PacketLogFile.Load(path);
        _output.WriteLine($"Loaded {file.Events.Count} events from {fixtureFileName}");
        _output.WriteLine($"Ports in capture: {string.Join(", ", file.Ports)}");

        var packets = new PacketSystem();
        packets.Initialize();
        var comparer = new PacketComparer(packets.Registry);

        var portMap = new Dictionary<int, int>
        {
            [6900] = 6900, // login
            [6121] = 6121, // char
            [5121] = 5191, // map: rAthena default 5121 → our 5191
        };

        var rewriter = new TokenRewriter(packets.Registry);
        await using var session = new ReplaySession(
            "127.0.0.1", portMap, TimeSpan.FromSeconds(5), rewriter);
        var capture = await session.ReplayAsync(file);
        var report = comparer.Compare(capture);

        _output.WriteLine(report.Render());
        if (rewriter.Substitutions.Count > 0)
        {
            _output.WriteLine($"token substitutions ({rewriter.Substitutions.Count}):");
            foreach (var s in rewriter.Substitutions)
            {
                _output.WriteLine($"  {s.Name}: {BitConverter.ToString(s.From)} → {BitConverter.ToString(s.To)}");
            }
        }
        _output.WriteLine($"server logs: {_fixture.LogDirectory}");

        Assert.True(report.Passed, $"Replay diverged for {fixtureFileName} — see test output for details.");
    }

    public static IEnumerable<object[]> FixturePaths()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Replay", "Fixtures");
        if (!Directory.Exists(dir)) yield break;
        foreach (var file in Directory.GetFiles(dir, "*.log").OrderBy(f => f))
        {
            yield return new object[] { Path.GetFileName(file) };
        }
    }
}
