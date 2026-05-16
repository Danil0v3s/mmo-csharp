using System.Text;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>AC_REFUSE_LOGIN</c> (0x083E) — the login-failure packet.
/// Fixed-size 26 bytes:
///
/// <code>
///   int16 packetType        2  ← header (skipped)
///   uint32 error            4
///   char   unblockTime[20] 20  ← TOLERANT (only set on temp-ban refusals)
/// </code>
/// </summary>
public sealed class AcRefuseLoginDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.AC_REFUSE_LOGIN;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // header

        var fields = new List<DecodedField>
        {
            new("Error", r.ReadUInt32()),
            new("UnblockTime", ReadFixedString(r, 20), Tolerant: true),
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
