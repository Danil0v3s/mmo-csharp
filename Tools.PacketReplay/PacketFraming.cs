using Core.Server.Packets;

namespace Tools.PacketReplay;

/// <summary>
/// Slices a byte stream into individual wire packets using the rAthena
/// framing rules:
///   - First 2 bytes (LE) = packet header (id)
///   - If header is in the fixed-size registry → known body length follows
///   - Otherwise it's variable-length: next 2 bytes (LE) = total packet length
///
/// Two practical wrinkles from real captures:
///   - The char server's first response after CH_ENTER prefixes the stream
///     with a bare 4-byte account_id (no header). The caller can declare
///     a leading "prelude" length to skip past it.
///   - Some packets aren't in our <see cref="IPacketSizeRegistry"/> (we
///     haven't implemented them yet). The framer treats unknown headers
///     as a fatal error so the diff surfaces them at parse time rather than
///     silently swallowing the rest of the stream.
/// </summary>
public sealed class PacketFramer
{
    private readonly IPacketSizeRegistry _sizes;

    public PacketFramer(IPacketSizeRegistry sizes)
    {
        _sizes = sizes;
    }

    /// <summary>
    /// Frame <paramref name="bytes"/> into wire packets. Returns the list
    /// of slices plus any trailing prelude / unconsumed tail.
    /// </summary>
    public FramingResult Frame(ReadOnlySpan<byte> bytes, int preludeLength = 0)
    {
        var frames = new List<FramedPacket>();
        var unknown = new List<UnknownPacket>();
        var offset = 0;

        if (preludeLength > 0)
        {
            if (bytes.Length < preludeLength)
            {
                return new FramingResult(frames, unknown, bytes.ToArray(), TrailingBytes: 0);
            }
            offset = preludeLength;
        }

        while (offset + 2 <= bytes.Length)
        {
            var header = (PacketHeader)BitConverter.ToInt16(bytes[offset..(offset + 2)]);
            if (!_sizes.IsRegistered(header))
            {
                // Unknown header. Capture what we can and bail — anything
                // after this point is unparseable without the schema.
                unknown.Add(new UnknownPacket(header, offset, bytes[offset..].ToArray()));
                offset = bytes.Length;
                break;
            }

            int packetLength;
            if (_sizes.IsFixedLength(header))
            {
                packetLength = _sizes.GetFixedSize(header)!.Value;
            }
            else
            {
                if (offset + 4 > bytes.Length)
                {
                    // Truncated variable-length header; treat the remainder
                    // as a trailing fragment.
                    break;
                }
                packetLength = BitConverter.ToInt16(bytes[(offset + 2)..(offset + 4)]);
                if (packetLength < 4)
                {
                    throw new InvalidDataException(
                        $"Variable packet {header} (0x{(short)header:X4}) at offset {offset} "
                        + $"declared length {packetLength} (must be ≥ 4)");
                }
            }

            if (offset + packetLength > bytes.Length)
            {
                // Frame is truncated — capture as partial.
                break;
            }

            frames.Add(new FramedPacket(
                Header: header,
                Offset: offset,
                Length: packetLength,
                Body: bytes.Slice(offset, packetLength).ToArray()));
            offset += packetLength;
        }

        var trailing = bytes.Length - offset;
        var prelude = preludeLength > 0 ? bytes[..preludeLength].ToArray() : Array.Empty<byte>();
        return new FramingResult(frames, unknown, prelude, trailing);
    }
}

public sealed record FramingResult(
    IReadOnlyList<FramedPacket> Packets,
    IReadOnlyList<UnknownPacket> UnknownPackets,
    byte[] Prelude,
    int TrailingBytes);

public sealed record FramedPacket(PacketHeader Header, int Offset, int Length, byte[] Body);

public sealed record UnknownPacket(PacketHeader Header, int Offset, byte[] Tail);
