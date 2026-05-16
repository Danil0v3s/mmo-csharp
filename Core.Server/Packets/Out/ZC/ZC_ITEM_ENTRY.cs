namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "There's an item already on the floor here." rAthena <c>clif_getareachar_item</c>.
/// Sent when a player walks into the AOI of an existing floor item.
/// PACKETVER 20211103 shape (item id widened to uint32):
///
/// <code>
///   0x009d packet_id (2) + AID (4) + itemId (4) + identify (1) +
///   x (2) + y (2) + amount (2) + subX (1) + subY (1) = 19 bytes
/// </code>
/// </summary>
public class ZC_ITEM_ENTRY : OutgoingPacket
{
    private const int SIZE = 19;

    public int EntityId { get; init; }
    public int ItemId { get; init; }
    public byte Identified { get; init; } = 1;
    public short X { get; init; }
    public short Y { get; init; }
    public short Amount { get; init; }
    public byte SubX { get; init; }
    public byte SubY { get; init; }

    public ZC_ITEM_ENTRY() : base(PacketHeader.ZC_ITEM_ENTRY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(ItemId);
        writer.Write(Identified);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Amount);
        writer.Write(SubX);
        writer.Write(SubY);
    }
}
