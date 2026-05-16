using System.Text;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>HC_ACCEPT_ENTER</c> (0x006B) — the char-server's
/// "accepted, here's your account slot summary + character data" reply.
/// Variable-length: 4-byte prefix (header + length) + 3 slot counts +
/// 20-byte extension + the per-character data block.
///
/// <code>
///   int16 packetType        2
///   int16 packetLength      2
///   uint8 total             1
///   uint8 premiumStart      1
///   uint8 premiumEnd        1
///   char  extension[20]    20  ← TOLERANT (rAthena often leaves it zero)
///   byte  characterData[]   N  ← TOLERANT for now (per-char body decode TBD)
/// </code>
/// </summary>
public sealed class HcAcceptEnterDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.HC_ACCEPT_ENTER;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType
        var packetLength = r.ReadInt16();

        var fields = new List<DecodedField>
        {
            new("Total",        r.ReadByte()),
            new("PremiumStart", r.ReadByte()),
            new("PremiumEnd",   r.ReadByte()),
            new("Extension",    ReadFixedString(r, 20), Tolerant: true),
        };

        // Remaining bytes are character entries. Body-decoding each character
        // is the next iteration; for now treat the whole tail as a single
        // tolerant blob (per-character struct varies by PACKETVER and
        // changing one byte across many chars would drown the diff).
        var remaining = packetLength - ms.Position;
        if (remaining > 0)
        {
            fields.Add(new("CharacterData.Length", (int)remaining, Tolerant: true));
        }

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
