namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_configuration</c> / <c>clif_equipcheckbox</c>
/// ([clif.cpp:11001-11004]). Single setting flag. Fixed 10 bytes:
/// <c>0x02d9 packet_id (2) + type (4) + value (4)</c>.
///
/// Type values (rAthena <c>e_config_type</c>):
///   0 = CONFIG_OPEN_EQUIPMENT_WINDOW (clif_equipcheckbox)
///   1 = CONFIG_CALL
///   2 = CONFIG_PET_AUTOFEED
///   3 = CONFIG_HOMUNCULUS_AUTOFEED
/// </summary>
public class ZC_CONFIG : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(uint);

    public uint Type { get; init; }
    public uint Value { get; init; }

    public ZC_CONFIG() : base(PacketHeader.ZC_CONFIG, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Type);
        writer.Write(Value);
    }
}
