using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_ATTACK_RANGE</c> (0x013A). Fixed 4 bytes:
/// header (2) + range (2).
/// </summary>
public sealed class ZcAttackRangeDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_ATTACK_RANGE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var range = r.ReadInt16();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("Range", range),
        });
    }
}
