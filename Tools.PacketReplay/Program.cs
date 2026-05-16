using Core.Server.Packets;
using Tools.PacketReplay;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: Tools.PacketReplay <fixture.log> [--host HOST] [--map CAPTURE:LOCAL]* [--timeout-ms N]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  --host HOST          host to connect to (default 127.0.0.1)");
    Console.Error.WriteLine("  --map CAPTURE:LOCAL  capture-port → local-port mapping; default: 6900→6900 6121→6121 5121→5191");
    Console.Error.WriteLine("  --timeout-ms N       per-read socket timeout (default 5000)");
    return 2;
}

var fixturePath = args[0];
var host = "127.0.0.1";
var timeoutMs = 5000;
var portMap = new Dictionary<int, int> { [6900] = 6900, [6121] = 6121, [5121] = 5191 };

for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--host" when i + 1 < args.Length:
            host = args[++i];
            break;
        case "--map" when i + 1 < args.Length:
            var parts = args[++i].Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out var src)
                || !int.TryParse(parts[1], out var dst))
            {
                Console.Error.WriteLine($"--map expects CAPTURE:LOCAL (got '{args[i]}')");
                return 2;
            }
            portMap[src] = dst;
            break;
        case "--timeout-ms" when i + 1 < args.Length:
            timeoutMs = int.Parse(args[++i]);
            break;
        default:
            Console.Error.WriteLine($"unknown arg: {args[i]}");
            return 2;
    }
}

var file = PacketLogFile.Load(fixturePath);
Console.WriteLine($"loaded {file.Events.Count} events from {fixturePath}");
Console.WriteLine($"capture spans ports: {string.Join(", ", file.Ports)}");
Console.WriteLine($"port mapping: {string.Join(", ", portMap.Select(kv => $"{kv.Key}→{kv.Value}"))}");

var packets = new PacketSystem();
packets.Initialize();
var comparer = new PacketComparer(packets.Registry);

await using var session = new ReplaySession(host, portMap, TimeSpan.FromMilliseconds(timeoutMs));
var capture = await session.ReplayAsync(file);

var report = comparer.Compare(capture);
Console.WriteLine(report.Render());
Console.WriteLine(report.Passed ? "✓ PASS" : "✗ FAIL");
return report.Passed ? 0 : 1;
