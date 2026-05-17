using System.Text;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>HC_SEND_MAP_DATA</c> (0x0AC5) — the map-handoff packet
/// the char server sends after CH_SELECT_CHAR. Fixed 156 bytes:
///
/// <code>
///   int16  packetType   2  ← header (skipped)
///   uint32 charId       4  ← TOLERANT (auto_increment differs per DB)
///   char   mapName[16] 16
///   uint32 ip           4  ← TOLERANT (host network address differs)
///   uint16 port         2  ← TOLERANT (capture host vs replay host)
///   char   domain[128] 128
/// </code>
///
/// MapName is strict — the suffix and base-name parity catch real bugs
/// (e.g. forgetting the ".gat" suffix or sending the wrong map).
/// </summary>
public sealed class HcSendMapDataDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.HC_SEND_MAP_DATA;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType

        var fields = new List<DecodedField>
        {
            new("CharId", r.ReadUInt32(), Tolerant: true),
            new("MapName", ReadFixedString(r, 16)),
            new("Ip", r.ReadUInt32(), Tolerant: true),
            new("Port", r.ReadUInt16(), Tolerant: true),
            new("Domain", ReadFixedString(r, 128)),
        };

        return new DecodedPacket(Header, fields);
    }

    private static string ReadFixedString(BinaryReader r, int length)
    {
        var bytes = r.ReadBytes(length);
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        return Encoding.ASCII.GetString(bytes, 0, len);
    }
}
