namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_equipcheckbox</c> (PACKETVER ≥ 20070918,
/// [clif.cpp:10282]). Fixed 3 bytes:
/// <c>0x02da packet_id (2) + open_equip_window (1)</c>.
///
/// Note: separate from <see cref="ZC_CONFIG"/> (0x02D9) — same idea,
/// different shape. rAthena sends one or the other depending on the
/// config knob being changed.
/// </summary>
public class ZC_CONFIG_NOTIFY : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(byte);

    public byte OpenEquipWindow { get; init; }

    public ZC_CONFIG_NOTIFY() : base(PacketHeader.ZC_CONFIG_NOTIFY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(OpenEquipWindow);
    }
}
