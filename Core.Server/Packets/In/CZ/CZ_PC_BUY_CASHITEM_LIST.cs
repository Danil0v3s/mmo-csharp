namespace Core.Server.Packets.In.CZ;

/// <summary>One line of a cash-shop buy: <c>Amount</c> of item <c>ItemId</c> from cash-shop <c>Tab</c>.</summary>
public readonly record struct CashBuyLine(int ItemId, int Amount, short Tab);

/// <summary>
/// Buy item(s) from the cash shop. rAthena <c>clif_parse_cashshop_buy</c>
/// (clif.cpp, <c>PACKET_CZ_SE_PC_BUY_CASHITEM_LIST</c> 0x0848). Variable:
/// <c>0848 &lt;len&gt;.W &lt;count&gt;.W &lt;kafraPoints&gt;.L { &lt;itemId&gt;.L &lt;amount&gt;.L &lt;tab&gt;.W }*</c>
/// — 10 bytes per line; the list begins at body offset 6 (count.W + kafraPoints.L).
/// </summary>
public class CZ_PC_BUY_CASHITEM_LIST : IncomingPacket
{
    private const int LineSize = 10; // itemId.L(4) amount.L(4) tab.W(2)

    public int KafraPoints { get; private set; }
    public IReadOnlyList<CashBuyLine> Lines { get; private set; } = Array.Empty<CashBuyLine>();

    public CZ_PC_BUY_CASHITEM_LIST() : base(PacketHeader.CZ_PC_BUY_CASHITEM_LIST, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength < 6) return;
        var count = reader.ReadUInt16();
        KafraPoints = (int)reader.ReadUInt32();

        var available = (bodyLength - 6) / LineSize;
        if (count > available) count = (ushort)available; // trust the byte count, not the field
        var lines = new CashBuyLine[count];
        for (var i = 0; i < count; i++)
        {
            var itemId = (int)reader.ReadUInt32();
            var amount = (int)reader.ReadUInt32();
            var tab = reader.ReadInt16();
            lines[i] = new CashBuyLine(itemId, amount, tab);
        }
        Lines = lines;
    }

    public static CZ_PC_BUY_CASHITEM_LIST Create(BinaryReader reader)
    {
        var packet = new CZ_PC_BUY_CASHITEM_LIST();
        packet.Read(reader);
        return packet;
    }
}
