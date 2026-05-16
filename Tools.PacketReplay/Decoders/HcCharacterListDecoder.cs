using System.Text;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for the packet our codebase calls
/// <c>HC_CHARACTER_LIST</c> (0x082D). On the wire — and in rAthena, where
/// the same id is named <c>HC_BLOCK_CHARACTER</c> — it carries the
/// per-account slot summary, not character rows:
///
/// <code>
///   int16 packetType        2
///   int16 packetLength      2
///   uint8 normal_slots      1
///   uint8 premium_slots     1
///   uint8 billing_slots     1
///   uint8 producible_slots  1
///   uint8 valid_slots       1
///   char  extension[20]    20  ← TOLERANT (rAthena leaves it zero)
/// </code>
///
/// The actual character-row list lives in <c>HC_ACCEPT_ENTER</c> (0x006B).
/// </summary>
public sealed class HcCharacterListDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.HC_CHARACTER_LIST;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType
        r.ReadInt16(); // packetLength (always 29 for this fixed-shape variant)

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("NormalSlots",     r.ReadByte()),
            new("PremiumSlots",    r.ReadByte()),
            new("BillingSlots",    r.ReadByte()),
            new("ProducibleSlots", r.ReadByte()),
            new("ValidSlots",      r.ReadByte()),
            new("Extension",       ReadFixedString(r, 20), Tolerant: true),
        });
    }

    private static string ReadFixedString(BinaryReader r, int length)
    {
        var bytes = r.ReadBytes(length);
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        return Encoding.ASCII.GetString(bytes, 0, len);
    }
}
