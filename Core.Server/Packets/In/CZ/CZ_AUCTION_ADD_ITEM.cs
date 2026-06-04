namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Stage an inventory item for auction. rAthena <c>clif_parse_Auction_setitem</c> (clif.cpp, 0x024c).
/// Fixed 8 bytes: <c>024c &lt;index&gt;.W &lt;amount&gt;.L</c>. <c>Index</c> is the client inventory index
/// (server index + 2); amount is always 1.
/// </summary>
public class CZ_AUCTION_ADD_ITEM : IncomingPacket
{
    private const int SIZE = 8;

    public short Index { get; private set; }
    public int Amount { get; private set; }

    public CZ_AUCTION_ADD_ITEM() : base(PacketHeader.CZ_AUCTION_ADD_ITEM, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Index = reader.ReadInt16();
        Amount = reader.ReadInt32();
    }

    public static CZ_AUCTION_ADD_ITEM Create(BinaryReader reader)
    {
        var p = new CZ_AUCTION_ADD_ITEM();
        p.Read(reader);
        return p;
    }
}
