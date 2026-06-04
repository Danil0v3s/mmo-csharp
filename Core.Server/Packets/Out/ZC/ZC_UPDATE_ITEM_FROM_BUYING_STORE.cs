namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Update a buying store's offer + the buyer's remaining escrow limit (sent to the buyer on a sale).
/// rAthena <c>clif_buyingstore_update_item</c> (clif.cpp, 0x081b legacy form). Fixed 10 bytes:
/// <c>081b &lt;name id&gt;.W &lt;amount&gt;.W &lt;limit zeny&gt;.L</c>.
/// </summary>
public class ZC_UPDATE_ITEM_FROM_BUYING_STORE : OutgoingPacket
{
    private const int SIZE = 2 + 2 + 2 + 4; // 10

    public short NameId { get; init; }
    public short Amount { get; init; }   // remaining wanted amount of this offer
    public int ZenyLimit { get; init; }  // remaining escrow

    public ZC_UPDATE_ITEM_FROM_BUYING_STORE() : base(PacketHeader.ZC_UPDATE_ITEM_FROM_BUYING_STORE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(NameId);
        writer.Write(Amount);
        writer.Write(ZenyLimit);
    }
}
