using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_PARTY_CONFIG</c> (0x02C9). Fixed 3 bytes:
/// header (2) + denyPartyInvites (1).
/// </summary>
public sealed class ZcPartyConfigDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_PARTY_CONFIG;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var deny = r.ReadByte();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("DenyPartyInvites", deny),
        });
    }
}
