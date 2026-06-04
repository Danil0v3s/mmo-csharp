namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// The remaining sale stock for an active sale item. rAthena <c>clif_sale_amount</c>
/// (clif.cpp, <c>PACKET_ZC_ACK_COUNT_BARGAIN_SALE_ITEM</c> 0x09c4). Fixed 10 bytes:
/// <c>09c4 &lt;itemId&gt;.L &lt;amount&gt;.L</c>.
/// </summary>
public class ZC_ACK_COUNT_BARGAIN_SALE_ITEM : OutgoingPacket
{
    private const int SIZE = 2 + 4 + 4; // 10

    public uint ItemId { get; init; }
    public int Amount { get; init; }

    public ZC_ACK_COUNT_BARGAIN_SALE_ITEM() : base(PacketHeader.ZC_ACK_COUNT_BARGAIN_SALE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(Amount);
    }
}
