namespace Core.Server.Packets.In.CZ;

/// <summary>One line of a vending purchase: <c>Amount</c> of the offer at cart client <c>Index</c>.</summary>
public readonly record struct VendBuyLine(short Amount, short Index);

/// <summary>
/// Buy items from a vending shop. rAthena <c>clif_parse_PurchaseReq</c> (clif.cpp, 0x0134). Variable:
/// <c>0134 &lt;len&gt;.W &lt;vendor account id&gt;.L { &lt;amount&gt;.W &lt;index&gt;.W }*</c> (4 bytes per line,
/// amount before index — rAthena <c>CZ_PURCHASE_ITEM_FROMMC</c>).
/// </summary>
public class CZ_PC_PURCHASE_ITEMLIST_FROMMC : IncomingPacket
{
    private const int LineSize = 4;

    public int VendorAccountId { get; private set; }
    public IReadOnlyList<VendBuyLine> Lines { get; private set; } = Array.Empty<VendBuyLine>();

    public CZ_PC_PURCHASE_ITEMLIST_FROMMC() : base(PacketHeader.CZ_PC_PURCHASE_ITEMLIST_FROMMC, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength < 4) return;
        VendorAccountId = reader.ReadInt32();

        var count = (bodyLength - 4) / LineSize;
        var lines = new VendBuyLine[count];
        for (var i = 0; i < count; i++)
        {
            var amount = reader.ReadInt16();
            var index = reader.ReadInt16();
            lines[i] = new VendBuyLine(amount, index);
        }
        Lines = lines;
    }

    public static CZ_PC_PURCHASE_ITEMLIST_FROMMC Create(BinaryReader reader)
    {
        var packet = new CZ_PC_PURCHASE_ITEMLIST_FROMMC();
        packet.Read(reader);
        return packet;
    }
}
