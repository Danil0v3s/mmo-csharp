using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_MAPPROPERTY_R2</c> (0x099B). Fixed 8 bytes:
/// header (2) + mapType (2) + flag (4). MapType values from rAthena's
/// <c>e_map_property</c>: MAPPROPERTY_NOTHING=0, MAPPROPERTY_FREEPVPZONE=1,
/// MAPPROPERTY_AGITZONE=2, etc.
/// </summary>
public sealed class ZcMapPropertyR2Decoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_MAPPROPERTY_R2;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var mapType = r.ReadInt16();
        var flag = r.ReadUInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("MapType", mapType),
            new("Flag", flag),
        });
    }
}
