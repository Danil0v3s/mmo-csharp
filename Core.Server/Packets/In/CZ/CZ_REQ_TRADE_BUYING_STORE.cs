namespace Core.Server.Packets.In.CZ;

/// <summary>One line of a sell-to-buying-store request: sell <c>Amount</c> of item <c>NameId</c> from
/// the seller's inventory at client <c>Index</c>.</summary>
public readonly record struct BuyStoreSellLine(short Index, short NameId, short Amount);

/// <summary>
/// Sell items into a buying store. rAthena <c>clif_parse_ReqTradeBuyingStore</c> (clif.cpp, 0x0819).
/// Variable: <c>0819 &lt;len&gt;.W &lt;buyer account id&gt;.L &lt;store id&gt;.L { &lt;index&gt;.W &lt;name id&gt;.W &lt;amount&gt;.W }*</c>
/// (6 bytes per line; the list begins at body offset 8).
/// </summary>
public class CZ_REQ_TRADE_BUYING_STORE : IncomingPacket
{
    private const int LineSize = 6;

    public int BuyerAccountId { get; private set; }
    public uint StoreId { get; private set; }
    public IReadOnlyList<BuyStoreSellLine> Lines { get; private set; } = Array.Empty<BuyStoreSellLine>();

    public CZ_REQ_TRADE_BUYING_STORE() : base(PacketHeader.CZ_REQ_TRADE_BUYING_STORE, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength < 8) return;
        BuyerAccountId = reader.ReadInt32();
        StoreId = reader.ReadUInt32();

        var count = (bodyLength - 8) / LineSize;
        var lines = new BuyStoreSellLine[count];
        for (var i = 0; i < count; i++)
        {
            var index = reader.ReadInt16();
            var nameId = reader.ReadInt16();
            var amount = reader.ReadInt16();
            lines[i] = new BuyStoreSellLine(index, nameId, amount);
        }
        Lines = lines;
    }

    public static CZ_REQ_TRADE_BUYING_STORE Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_TRADE_BUYING_STORE();
        packet.Read(reader);
        return packet;
    }
}
