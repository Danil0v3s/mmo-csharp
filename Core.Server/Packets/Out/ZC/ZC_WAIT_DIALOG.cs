namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Show the 'Next' button" in the open dialog. rAthena
/// <c>clif_scriptnext</c>. Fixed 6 bytes: 0x00b5 packet_id (2) + npcId (4).
/// Server waits for <see cref="In.CZ.CZ_REQ_NEXT_SCRIPT"/> before sending
/// the next batch of dialog packets.
/// </summary>
public class ZC_WAIT_DIALOG : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint);

    public uint NpcId { get; init; }

    public ZC_WAIT_DIALOG() : base(PacketHeader.ZC_WAIT_DIALOG, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(NpcId);
    }
}
