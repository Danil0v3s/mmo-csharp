namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Cancel an auction listing. rAthena <c>clif_parse_Auction_cancel</c> (clif.cpp, 0x024e). Fixed 6
/// bytes: <c>024e &lt;auctionId&gt;.L</c>. Rejected by the service if a bidder already exists.
/// </summary>
public class CZ_AUCTION_CANCEL : IncomingPacket
{
    private const int SIZE = 6;

    public uint AuctionId { get; private set; }

    public CZ_AUCTION_CANCEL() : base(PacketHeader.CZ_AUCTION_CANCEL, SIZE) { }

    public override void Read(BinaryReader reader) => AuctionId = reader.ReadUInt32();

    public static CZ_AUCTION_CANCEL Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_CANCEL();
        p.Read(reader);
        return p;
    }
}
