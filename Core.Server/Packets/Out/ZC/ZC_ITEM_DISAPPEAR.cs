namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Floor item gone." rAthena <c>clif_clearflooritem</c>. Used on pickup,
/// despawn, and view-range exit. Shape: 0x00a1 packet_id (2) + AID (4) = 6 bytes.
/// </summary>
public class ZC_ITEM_DISAPPEAR : OutgoingPacket
{
    private const int SIZE = 6;

    public int EntityId { get; init; }

    public ZC_ITEM_DISAPPEAR() : base(PacketHeader.ZC_ITEM_DISAPPEAR, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
    }
}
