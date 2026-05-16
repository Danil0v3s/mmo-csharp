namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server tick echo for client latency calc. rAthena <c>clif_notify_time</c>.
/// Shape: 0x007f packet_id (2) + serverTick (4) = 6 bytes.
/// </summary>
public class ZC_NOTIFY_TIME : OutgoingPacket
{
    private const int SIZE = 6;

    public uint ServerTick { get; init; }

    public ZC_NOTIFY_TIME() : base(PacketHeader.ZC_NOTIFY_TIME, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ServerTick);
    }
}
