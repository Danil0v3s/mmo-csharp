namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>e_cashshop_buy_result</c> (<c>CASHSHOP_RESULT_*</c>, cashshop.hpp:42) — the
/// <c>result</c> code carried by the cash-shop-button buy packet
/// <see cref="ZC_PC_BUY_CASHITEM_RESULT"/> (0x0849). Distinct from the NPC-shop <c>ERROR_TYPE_*</c>
/// ack (0x0289).
/// </summary>
public enum CashShopBuyResult : ushort
{
    Success = 0x0,            // CASHSHOP_RESULT_SUCCESS
    System = 0x1,            // CASHSHOP_RESULT_ERROR_SYSTEM
    ShortageCash = 0x2,      // CASHSHOP_RESULT_ERROR_SHORTTAGE_CASH — not enough points
    UnknownItem = 0x3,       // CASHSHOP_RESULT_ERROR_UNKONWN_ITEM — bad item / not in tab
    InventoryWeight = 0x4,   // CASHSHOP_RESULT_ERROR_INVENTORY_WEIGHT — over weight
    InventoryItemCnt = 0x5,  // CASHSHOP_RESULT_ERROR_INVENTORY_ITEMCNT — no free slot
    PcState = 0x6,           // CASHSHOP_RESULT_ERROR_PC_STATE — busy (trading)
    OverProductTotalCnt = 0x7, // CASHSHOP_RESULT_ERROR_OVER_PRODUCT_TOTAL_CNT — over stack/amount
    SomeBuyFailure = 0x8,    // CASHSHOP_RESULT_ERROR_SOME_BUY_FAILURE
    Unknown = 0xb,           // CASHSHOP_RESULT_ERROR_UNKNOWN
}

/// <summary>
/// Cash-shop buy result + the player's resulting point balances. rAthena <c>clif_cashshop_result</c>
/// (clif.cpp, <c>PACKET_ZC_SE_PC_BUY_CASHITEM_RESULT</c> 0x0849). Fixed 16 bytes:
/// <c>0849 &lt;itemId&gt;.L &lt;result&gt;.W &lt;cashPoints&gt;.L &lt;kafraPoints&gt;.L</c>.
/// </summary>
public class ZC_PC_BUY_CASHITEM_RESULT : OutgoingPacket
{
    private const int SIZE = 2 + 4 + 2 + 4 + 4; // 16

    public uint ItemId { get; init; }
    public CashShopBuyResult Result { get; init; }
    public int CashPoints { get; init; }
    public int KafraPoints { get; init; }

    public ZC_PC_BUY_CASHITEM_RESULT() : base(PacketHeader.ZC_PC_BUY_CASHITEM_RESULT, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ItemId);
        writer.Write((ushort)Result);
        writer.Write(CashPoints);
        writer.Write(KafraPoints);
    }
}
