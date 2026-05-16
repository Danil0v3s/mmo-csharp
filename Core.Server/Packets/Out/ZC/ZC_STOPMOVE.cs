namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Entity stopped walking at (x, y)." rAthena <c>clif_fixpos</c>.
/// Shape: 0x0088 packet_id (2) + entityId (4) + x (2) + y (2) = 10 bytes.
/// </summary>
public class ZC_STOPMOVE : OutgoingPacket
{
    private const int SIZE = 10;

    public int EntityId { get; init; }
    public short X { get; init; }
    public short Y { get; init; }

    public ZC_STOPMOVE() : base(PacketHeader.ZC_STOPMOVE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(X);
        writer.Write(Y);
    }
}
