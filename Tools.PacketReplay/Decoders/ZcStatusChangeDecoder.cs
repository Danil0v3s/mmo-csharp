using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_STATUS_CHANGE</c> (0x00BE). Fixed 5 bytes:
/// header (2) + statusId (2) + value (1). Used by
/// <c>clif_initialstatus</c> for the renewal SP_USTR..SP_UCRT
/// need-points fields.
/// </summary>
public sealed class ZcStatusChangeDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_STATUS_CHANGE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var statusId = r.ReadUInt16();
        var value = r.ReadByte();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("StatusId", SpId.Format(statusId)),
            new("Value", value),
        });
    }
}
