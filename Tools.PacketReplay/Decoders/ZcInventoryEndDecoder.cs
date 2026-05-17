using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_INVENTORY_END</c> (0x0B0B). Fixed 4 bytes:
/// header (2) + invType (1) + flag (1).
/// </summary>
public sealed class ZcInventoryEndDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_INVENTORY_END;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var invType = r.ReadByte();
        var flag = r.ReadByte();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("InvType", invType),
            new("Flag", flag),
        });
    }
}
