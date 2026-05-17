namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_close_dialog</c> ([clif.cpp:2514]). Tells the client
/// to close an NPC dialog window. Fixed 6 bytes:
/// <c>0x00b6 packet_id (2) + npcId (4)</c>.
/// </summary>
public class ZC_CLOSE_DIALOG : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint);

    public uint NpcId { get; init; }

    public ZC_CLOSE_DIALOG() : base(PacketHeader.ZC_CLOSE_DIALOG, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(NpcId);
    }
}
