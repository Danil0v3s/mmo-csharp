using System.Text;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_NPCACK_MAPMOVE</c> (0x0091) — rAthena
/// <c>clif_changemap</c>. Fixed 22 bytes:
///
/// <code>
///   int16  packetType  2  ← header (skipped)
///   char   mapName[16] 16
///   int16  x           2  ← TOLERANT (RNG snap on OOB / non-walkable)
///   int16  y           2  ← TOLERANT
/// </code>
///
/// MapName is strict — confirms the suffix and base name match. X/Y are
/// tolerant for the same reason as <c>ZC_ACCEPT_ENTER_ZONE</c>: rAthena
/// <c>pc_setpos</c> randomizes when the saved coords are out of bounds.
/// </summary>
public sealed class ZcNpcackMapmoveDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_NPCACK_MAPMOVE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType

        var fields = new List<DecodedField>
        {
            new("MapName", ReadFixedString(r, 16)),
            new("X", r.ReadInt16(), Tolerant: true),
            new("Y", r.ReadInt16(), Tolerant: true),
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
