namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Result of staging an item for auction. rAthena <c>clif_Auction_setitem</c> (clif.cpp, 0x0256).
/// Fixed 5 bytes: <c>0256 &lt;index&gt;.W &lt;result&gt;.B</c> — result 0 = success, 1 = failure.
/// <c>Index</c> is the client inventory index (server + 2) on success.
/// </summary>
public class ZC_ACK_AUCTION_ADD_ITEM : OutgoingPacket
{
    private const int SIZE = 5;

    public short Index { get; init; }
    public bool Fail { get; init; }

    public ZC_ACK_AUCTION_ADD_ITEM() : base(PacketHeader.ZC_ACK_AUCTION_ADD_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write((byte)(Fail ? 1 : 0));
    }
}
