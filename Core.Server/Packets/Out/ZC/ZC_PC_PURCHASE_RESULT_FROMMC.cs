namespace Core.Server.Packets.Out.ZC;

/// <summary>Vending purchase result code. rAthena <c>e_pc_purchase_result_frommc</c>.</summary>
public enum VendPurchaseResult : byte
{
    Success = 0,
    NoZeny = 1,
    Overweight = 2,
    OutOfStock = 4,
    TradeInProgress = 5,
    StoreIncorrect = 6,
    NoSalesInfo = 7,
}

/// <summary>
/// Vending purchase result to the buyer. rAthena <c>clif_buyvending</c> (clif.cpp, 0x0135). Fixed 7
/// bytes: <c>0135 &lt;index&gt;.W &lt;amount&gt;.W &lt;result&gt;.B</c>.
/// </summary>
public class ZC_PC_PURCHASE_RESULT_FROMMC : OutgoingPacket
{
    private const int SIZE = 2 + 2 + 2 + 1; // 7

    public short Index { get; init; }     // cart client index
    public short Amount { get; init; }
    public VendPurchaseResult Result { get; init; }

    public ZC_PC_PURCHASE_RESULT_FROMMC() : base(PacketHeader.ZC_PC_PURCHASE_RESULT_FROMMC, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Amount);
        writer.Write((byte)Result);
    }
}
