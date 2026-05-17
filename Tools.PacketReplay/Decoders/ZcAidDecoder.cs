using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_AID</c> (0x0283) — fixed 6 bytes:
///   header (2) + accountId (4).
/// Sent by the map server right after CZ_WANT_TO_CONNECTION succeeds.
/// AccountId is marked tolerant for the same reason as in AC_ACCEPT_LOGIN:
/// our local DB's auto_increment offset differs from the capture's, so
/// our AID will always differ by a constant — the value itself isn't a
/// parity signal.
/// </summary>
public sealed class ZcAidDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_AID;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType

        var fields = new List<DecodedField>
        {
            new("AccountId", r.ReadUInt32(), Tolerant: true),
        };

        return new DecodedPacket(Header, fields);
    }
}
