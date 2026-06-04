namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Open the auction window on the client. rAthena <c>clif_Auction_openwindow</c> (clif.cpp, 0x025f).
/// Fixed 6 bytes: <c>025f &lt;flag&gt;.L</c> (0 = open).
/// </summary>
public class ZC_AUCTION_OPENWINDOW : OutgoingPacket
{
    private const int SIZE = 6;

    public int Flag { get; init; }

    public ZC_AUCTION_OPENWINDOW() : base(PacketHeader.ZC_AUCTION_OPENWINDOW, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Flag);
}
