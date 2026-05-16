using System.Globalization;

namespace Tools.PacketReplay;

/// <summary>
/// Parsed representation of a packet capture file. Format:
/// <code>
///   # comment line
///   &lt;port&gt;|S|&lt;hex&gt;   bytes the client sent to that port
///   &lt;port&gt;|R|&lt;hex&gt;   bytes the server on that port sent back
/// </code>
/// A change in port between events implicitly closes the current TCP
/// connection and opens a new one. This matches what an rAthena trace
/// captures end-to-end (login → char → map): the same file spans three
/// short TCP sessions back-to-back.
///
/// Each event still carries one contiguous chunk of bytes; the comparer
/// frames it into wire packets.
/// </summary>
public sealed class PacketLogFile
{
    public string SourcePath { get; }
    public IReadOnlyList<ReplayEvent> Events { get; }

    /// <summary>Ports the file touches, in first-seen order.</summary>
    public IReadOnlyList<int> Ports { get; }

    private PacketLogFile(string sourcePath, IReadOnlyList<ReplayEvent> events, IReadOnlyList<int> ports)
    {
        SourcePath = sourcePath;
        Events = events;
        Ports = ports;
    }

    public static PacketLogFile Load(string path)
    {
        var events = new List<ReplayEvent>();
        var portOrder = new List<int>();
        var seenPorts = new HashSet<int>();
        var lineNo = 0;
        foreach (var raw in File.ReadAllLines(path))
        {
            lineNo++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            // Format: <port>|<dir>|<hex>
            var firstPipe = line.IndexOf('|');
            var secondPipe = firstPipe < 0 ? -1 : line.IndexOf('|', firstPipe + 1);
            if (firstPipe < 0 || secondPipe < 0)
            {
                throw new InvalidDataException(
                    $"{path}:{lineNo}: expected '<port>|S|hex' or '<port>|R|hex', got '{line}'");
            }
            if (!int.TryParse(line.AsSpan(0, firstPipe), out var port))
            {
                throw new InvalidDataException($"{path}:{lineNo}: invalid port '{line[..firstPipe]}'");
            }
            var dirToken = line.Substring(firstPipe + 1, secondPipe - firstPipe - 1);
            var direction = dirToken switch
            {
                "S" => ReplayDirection.Send,
                "R" => ReplayDirection.Recv,
                _ => throw new InvalidDataException(
                    $"{path}:{lineNo}: direction must be S or R (got '{dirToken}')")
            };
            var bytes = HexToBytes(line[(secondPipe + 1)..], lineNo);
            events.Add(new ReplayEvent(port, direction, bytes, lineNo));
            if (seenPorts.Add(port)) portOrder.Add(port);
        }
        return new PacketLogFile(path, events, portOrder);
    }

    private static byte[] HexToBytes(string hex, int lineNo)
    {
        hex = hex.Replace(" ", "");
        if (hex.Length % 2 != 0)
        {
            throw new InvalidDataException($"Line {lineNo}: odd-length hex string");
        }
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var b))
            {
                throw new InvalidDataException($"Line {lineNo}: invalid hex at byte {i}");
            }
            bytes[i] = b;
        }
        return bytes;
    }
}

public enum ReplayDirection
{
    /// <summary>Bytes the client sends to the server.</summary>
    Send,
    /// <summary>Bytes the server is expected to send back.</summary>
    Recv,
}

public sealed record ReplayEvent(int Port, ReplayDirection Direction, byte[] Bytes, int SourceLine);
