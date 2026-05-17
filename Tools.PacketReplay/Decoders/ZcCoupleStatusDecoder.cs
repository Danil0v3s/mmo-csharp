using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_COUPLESTATUS</c> (0x0141). Fixed 14 bytes:
/// header (2) + statusType (4) + base (4) + plus (4). Surfaces both
/// the character's base allocation and the equipment/buff bonus.
///
/// Used for SP_STR..SP_LUK and renewal SP_POW..SP_CRT.
/// </summary>
public sealed class ZcCoupleStatusDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_COUPLESTATUS;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var statusType = r.ReadUInt32();
        var baseStatus = r.ReadInt32();
        var plusStatus = r.ReadInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("StatusType", SpId.Format(statusType)),
            new("Base", baseStatus),
            new("Plus", plusStatus),
        });
    }
}
