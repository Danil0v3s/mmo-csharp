using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>HC_SECOND_PASSWD_LOGIN</c> (0x08B9). Fixed-length 12 bytes:
///
/// <code>
///   int16 packetType  2  ← header (skipped)
///   uint32 seed       4  ← TOLERANT (random per emit)
///   uint32 accountId  4  ← TOLERANT (auto_increment differs per DB)
///   uint16 state      2  ← rAthena pincode_state (must match)
/// </code>
///
/// Only the state field is strict — the rest is session/DB-dependent noise.
/// </summary>
public sealed class HcSecondPasswdLoginDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.HC_SECOND_PASSWD_LOGIN;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType

        var fields = new List<DecodedField>
        {
            new("Seed", r.ReadUInt32(), Tolerant: true),
            new("AccountId", r.ReadUInt32(), Tolerant: true),
            new("State", r.ReadUInt16()),
        };

        return new DecodedPacket(Header, fields);
    }
}
