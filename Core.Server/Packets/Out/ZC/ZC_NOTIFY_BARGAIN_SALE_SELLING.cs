namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// An active limited-time sale, surfaced to a logging-in player. rAthena <c>clif_sale_start</c>
/// (clif.cpp, <c>PACKET_ZC_NOTIFY_BARGAIN_SALE_SELLING</c> 0x09b2). Fixed 10 bytes:
/// <c>09b2 &lt;itemId&gt;.L &lt;remainingTime&gt;.L</c> (seconds until the sale ends).
/// </summary>
public class ZC_NOTIFY_BARGAIN_SALE_SELLING : OutgoingPacket
{
    private const int SIZE = 2 + 4 + 4; // 10

    public uint ItemId { get; init; }
    public int RemainingSeconds { get; init; }

    public ZC_NOTIFY_BARGAIN_SALE_SELLING() : base(PacketHeader.ZC_NOTIFY_BARGAIN_SALE_SELLING, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(RemainingSeconds);
    }
}
