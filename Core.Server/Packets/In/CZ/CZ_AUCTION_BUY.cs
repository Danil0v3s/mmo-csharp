namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Bid on an auction. rAthena <c>clif_parse_Auction_bid</c> (clif.cpp, 0x024f). Fixed 10 bytes:
/// <c>024f &lt;auctionId&gt;.L &lt;money&gt;.L</c>.
/// </summary>
public class CZ_AUCTION_BUY : IncomingPacket
{
    private const int SIZE = 10;

    public uint AuctionId { get; private set; }
    public int Money { get; private set; }

    public CZ_AUCTION_BUY() : base(PacketHeader.CZ_AUCTION_BUY, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        AuctionId = reader.ReadUInt32();
        Money = (int)reader.ReadUInt32();
    }

    public static CZ_AUCTION_BUY Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_BUY();
        p.Read(reader);
        return p;
    }
}
