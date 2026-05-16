using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>HC_CHARLIST_NOTIFY</c> (0x09A0). rAthena
/// <c>clif_charlist_notify</c>: <c>0x09A0 &lt;total_pages&gt;.L = 6 bytes</c>.
/// Sent right after the slot summary; tells the client how many
/// character-list pages to request.
/// </summary>
public sealed class HcCharlistNotifyDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.HC_CHARLIST_NOTIFY;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("TotalPages", r.ReadInt32()),
        });
    }
}
