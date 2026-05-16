using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decodes a single framed packet's body bytes into a typed POCO whose
/// public properties name + carry the wire fields in order. The framework
/// drives this via <see cref="DecoderRegistry"/>; when no decoder is
/// registered for a header the comparer falls back to byte-level diff.
///
/// Decoders consume the FULL packet bytes (including the 2-byte header
/// and, for variable-length packets, the 2-byte length field) so that
/// per-decoder slicing logic stays explicit. Implementations skip the
/// prefix as their first action.
/// </summary>
public interface IPacketDecoder
{
    PacketHeader Header { get; }
    DecodedPacket Decode(byte[] packetBytes);
}

/// <summary>
/// Structured form of a decoded packet. <see cref="Fields"/> is an ordered
/// list of (name, value) pairs in the same order the wire format defines.
/// The comparer iterates this list to surface a per-field diff that names
/// the diverging field rather than just a byte offset.
/// </summary>
public sealed record DecodedPacket(PacketHeader Header, IReadOnlyList<DecodedField> Fields);

public sealed record DecodedField(string Name, object? Value, bool Tolerant = false);
