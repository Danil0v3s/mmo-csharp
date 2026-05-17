using System.Text;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>HC_ACCEPT_MAKECHAR</c> (0x0B6F) — fixed 177 bytes:
///   2-byte header + 175-byte <see cref="CharacterInfo"/> block.
///
/// Decomposes the CharacterInfo body into named fields so divergence
/// reports name the offending field (e.g. "Hp expected=40 actual=0")
/// instead of an opaque byte offset. Only <c>GID</c> is marked tolerant
/// — that's the char_id auto_increment value, intrinsically different
/// between the capture DB and our local one. Everything else should be
/// deterministic from the create-character parameters, so a mismatch
/// there is a real parity bug worth surfacing.
/// </summary>
public sealed class HcAcceptMakecharDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.HC_ACCEPT_MAKECHAR;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType (HC_ACCEPT_MAKECHAR is fixed-size, no length prefix)

        var fields = ReadCharacterInfoFields(r);
        return new DecodedPacket(Header, fields);
    }

    /// <summary>
    /// Reads one 175-byte CharacterInfo block from the current position
    /// of <paramref name="r"/> and returns a flat field list. Shared with
    /// any other packet that embeds CharacterInfo (e.g. variable-length
    /// HC_CHARACTER_LIST entries).
    /// </summary>
    internal static List<DecodedField> ReadCharacterInfoFields(BinaryReader r)
    {
        var fields = new List<DecodedField>
        {
            new("GID", r.ReadUInt32(), Tolerant: true), // char_id — auto_increment
            new("Exp", r.ReadInt64()),
            new("Money", r.ReadInt32()),
            new("JobExp", r.ReadInt64()),
            new("JobLevel", r.ReadInt32()),
            new("BodyState", r.ReadInt32()),
            new("HealthState", r.ReadInt32()),
            new("EffectState", r.ReadInt32()),
            new("Virtue", r.ReadInt32()),
            new("Honor", r.ReadInt32()),
            new("JobPoint", r.ReadInt16()),
            new("Hp", r.ReadInt64()),
            new("MaxHp", r.ReadInt64()),
            new("Sp", r.ReadInt64()),
            new("MaxSp", r.ReadInt64()),
            new("Speed", r.ReadInt16()),
            new("Job", r.ReadInt16()),
            new("Head", r.ReadInt16()),
            new("Body", r.ReadInt16()),
            new("Weapon", r.ReadInt16()),
            new("Level", r.ReadInt16()),
            new("SpPoint", r.ReadInt16()),
            new("Accessory", r.ReadInt16()),
            new("Shield", r.ReadInt16()),
            new("Accessory2", r.ReadInt16()),
            new("Accessory3", r.ReadInt16()),
            new("HeadPalette", r.ReadInt16()),
            new("BodyPalette", r.ReadInt16()),
            new("Name", ReadFixedString(r, 24)),
            new("Str", r.ReadByte()),
            new("Agi", r.ReadByte()),
            new("Vit", r.ReadByte()),
            new("Int", r.ReadByte()),
            new("Dex", r.ReadByte()),
            new("Luk", r.ReadByte()),
            new("CharNum", r.ReadByte()),
            new("HairColor", r.ReadByte()),
            new("IsChangedCharName", r.ReadInt16()),
            new("MapName", ReadFixedString(r, 16)),
            new("DelRevDate", r.ReadInt32()),
            new("RobePalette", r.ReadInt32()),
            new("ChrSlotChangeCnt", r.ReadInt32()),
            new("ChrNameChangeCnt", r.ReadInt32()),
            new("Sex", r.ReadByte()),
        };
        return fields;
    }

    private static string ReadFixedString(BinaryReader r, int length)
    {
        var bytes = r.ReadBytes(length);
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        return Encoding.ASCII.GetString(bytes, 0, len);
    }
}
