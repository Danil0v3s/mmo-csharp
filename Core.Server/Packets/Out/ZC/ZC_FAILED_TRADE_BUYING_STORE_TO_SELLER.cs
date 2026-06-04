namespace Core.Server.Packets.Out.ZC;

/// <summary>Buying-store sell-in failure result (to the seller). rAthena clif.cpp comment at 0x0824.</summary>
public enum BuyStoreSellResult : ushort
{
    DealFailed = 5,        // generic "The deal has failed."
    OverCount = 6,         // amount higher than the buyer is willing to buy
    BuyerLacksZeny = 7,    // buyer's escrow can't cover it
}

/// <summary>
/// A sell-into-buying-store attempt failed (sent to the seller). rAthena
/// <c>clif_buyingstore_trade_failed_seller</c> (clif.cpp, 0x0824). Fixed 6 bytes:
/// <c>0824 &lt;result&gt;.W &lt;name id&gt;.W</c>.
/// </summary>
public class ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER : OutgoingPacket
{
    private const int SIZE = 2 + 2 + 2; // 6

    public BuyStoreSellResult Result { get; init; }
    public short NameId { get; init; }

    public ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER() : base(PacketHeader.ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((ushort)Result);
        writer.Write(NameId);
    }
}
