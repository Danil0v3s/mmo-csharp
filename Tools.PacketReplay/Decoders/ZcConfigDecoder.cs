using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_CONFIG</c> (0x02D9). Fixed 10 bytes:
/// header (2) + type (4) + value (4). Type values from
/// rAthena's <c>e_config_type</c>: 0=OPEN_EQUIPMENT_WINDOW,
/// 1=CALL, 2=PET_AUTOFEED, 3=HOMUNCULUS_AUTOFEED, 4=BANK_AUTO,
/// 5=CASHSHOP_AUTO.
/// </summary>
public sealed class ZcConfigDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_CONFIG;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var type = r.ReadUInt32();
        var value = r.ReadUInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("Type", ConfigName(type) + $"({type})"),
            new("Value", value),
        });
    }

    private static string ConfigName(uint t) => t switch
    {
        0 => "OPEN_EQUIPMENT_WINDOW",
        1 => "CALL",
        2 => "PET_AUTOFEED",
        3 => "HOMUNCULUS_AUTOFEED",
        _ => "CONFIG_?",
    };
}
