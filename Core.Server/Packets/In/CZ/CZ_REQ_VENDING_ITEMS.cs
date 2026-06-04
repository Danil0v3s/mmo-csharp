namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Click a vending stall to browse it. rAthena <c>clif_parse_VendingListReq</c> (clif.cpp, 0x0130).
/// Fixed 6 bytes: <c>0130 &lt;vendor account id&gt;.L</c>.
/// </summary>
public class CZ_REQ_VENDING_ITEMS : IncomingPacket
{
    private const int SIZE = 6;

    public int VendorAccountId { get; private set; }

    public CZ_REQ_VENDING_ITEMS() : base(PacketHeader.CZ_REQ_VENDING_ITEMS, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        VendorAccountId = reader.ReadInt32();
    }

    public static CZ_REQ_VENDING_ITEMS Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_VENDING_ITEMS();
        packet.Read(reader);
        return packet;
    }
}
