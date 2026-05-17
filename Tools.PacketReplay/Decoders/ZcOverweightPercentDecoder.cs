using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_OVERWEIGHT_PERCENT</c> (0x0ADE). Fixed 6 bytes:
/// header (2) + percent (4). Emitted after weight recalc.
/// </summary>
public sealed class ZcOverweightPercentDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_OVERWEIGHT_PERCENT;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var percent = r.ReadUInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("Percent", percent),
        });
    }
}
