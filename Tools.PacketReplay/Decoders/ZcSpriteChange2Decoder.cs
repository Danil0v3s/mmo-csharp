using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_SPRITE_CHANGE2</c> (0x01D7). Fixed 15 bytes for
/// PACKETVER ≥ 20181121 / PACKETVER_RE ≥ 20180704. AID is tolerant
/// (auto_increment offset between captured and live DB); LookType
/// uses rAthena's <c>_look</c> enum (map.hpp:585).
/// </summary>
public sealed class ZcSpriteChange2Decoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_SPRITE_CHANGE2;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var aid = r.ReadUInt32();
        var lookType = r.ReadByte();
        var value = r.ReadUInt32();
        var value2 = r.ReadUInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("AID", aid, Tolerant: true),
            new("Look", LookName(lookType) + $"({lookType})"),
            new("Value", value),
            new("Value2", value2),
        });
    }

    private static string LookName(byte t) => t switch
    {
        0 => "BASE",
        1 => "HAIR",
        2 => "WEAPON",
        3 => "HEAD_BOTTOM",
        4 => "HEAD_TOP",
        5 => "HEAD_MID",
        6 => "HAIR_COLOR",
        7 => "CLOTHES_COLOR",
        8 => "SHIELD",
        9 => "SHOES",
        10 => "BODY",
        11 => "RES",
        12 => "MRES",
        13 => "ROBE",
        14 => "BODY2",
        _ => "LOOK_?",
    };
}
