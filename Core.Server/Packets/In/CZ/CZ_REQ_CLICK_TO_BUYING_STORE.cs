namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Click a buying store to view its offers. rAthena <c>clif_parse_ReqClickBuyingStore</c> (clif.cpp,
/// 0x0817). Fixed 6 bytes: <c>0817 &lt;buyer account id&gt;.L</c>.
/// </summary>
public class CZ_REQ_CLICK_TO_BUYING_STORE : IncomingPacket
{
    private const int SIZE = 6;

    public int BuyerAccountId { get; private set; }

    public CZ_REQ_CLICK_TO_BUYING_STORE() : base(PacketHeader.CZ_REQ_CLICK_TO_BUYING_STORE, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        BuyerAccountId = reader.ReadInt32();
    }

    public static CZ_REQ_CLICK_TO_BUYING_STORE Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_CLICK_TO_BUYING_STORE();
        packet.Read(reader);
        return packet;
    }
}
