using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_NOTIFY_UNREADMAIL</c> (0x09E7). Fixed 3 bytes:
/// header (2) + result (1). For a fresh character the result is 0
/// (no unread mail).
/// </summary>
public sealed class ZcNotifyUnreadMailDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_NOTIFY_UNREADMAIL;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var result = r.ReadByte();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("Result", result),
        });
    }
}
