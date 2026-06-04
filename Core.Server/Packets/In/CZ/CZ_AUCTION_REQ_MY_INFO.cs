namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Request the player's own auctions. rAthena <c>clif_parse_Auction_buysell</c> (clif.cpp, 0x025c).
/// Fixed 4 bytes: <c>025c &lt;type&gt;.W</c> — 0 = my selling, 1 = my buying. Renders through the
/// same search-results packet (server-side filtered to the player).
/// </summary>
public class CZ_AUCTION_REQ_MY_INFO : IncomingPacket
{
    private const int SIZE = 4;

    public short Type { get; private set; }

    public CZ_AUCTION_REQ_MY_INFO() : base(PacketHeader.CZ_AUCTION_REQ_MY_INFO, SIZE) { }

    public override void Read(BinaryReader reader) => Type = reader.ReadInt16();

    public static CZ_AUCTION_REQ_MY_INFO Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_REQ_MY_INFO();
        p.Read(reader);
        return p;
    }
}
